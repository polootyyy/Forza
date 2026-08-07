using System.Diagnostics;
using System.Runtime.InteropServices;
using Polootyyy.Helpers;
using Polootyyy.Models;

namespace Polootyyy.Services;

public class MemoryService : IDisposable
{
    private Process? _gameProcess;
    private nint _processHandle;
    private bool _isAttached;
    private readonly Dictionary<string, nint> _moduleCache = new();

    public bool IsAttached => _isAttached;
    public Process? GameProcess => _gameProcess;
    public nint ProcessHandle => _processHandle;

    public event Action<string, LogLevel>? OnLog;

    public bool EnableDebugPrivilege()
    {
        try
        {
            nint token;
            if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), 0x0020 | 0x0008, out token))
                return false;

            NativeMethods.LUID luid;
            if (!NativeMethods.LookupPrivilegeValue(null, NativeMethods.SE_DEBUG_NAME, out luid))
                return false;

            var tp = new NativeMethods.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = NativeMethods.SE_PRIVILEGE_ENABLED
            };

            bool result = NativeMethods.AdjustTokenPrivileges(token, false, ref tp, 0, nint.Zero, nint.Zero);
            NativeMethods.CloseHandle(token);
            return result;
        }
        catch
        {
            return false;
        }
    }

    public bool AttachToGame(string processName = "ForzaHorizon6")
    {
        try
        {
            EnableDebugPrivilege();

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                processes = Process.GetProcessesByName(processName + ".exe");

            if (processes.Length == 0)
            {
                OnLog?.Invoke($"Game process '{processName}' not found. Is the game running?", LogLevel.Warning);
                return false;
            }

            _gameProcess = processes[0];
            _processHandle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_VM_WRITE |
                NativeMethods.PROCESS_VM_OPERATION | NativeMethods.PROCESS_QUERY_INFORMATION |
                NativeMethods.PROCESS_CREATE_THREAD,
                false, _gameProcess.Id);

            if (_processHandle == nint.Zero)
            {
                OnLog?.Invoke("Failed to open process. Run as Administrator!", LogLevel.Error);
                return false;
            }

            _isAttached = true;
            _moduleCache.Clear();
            OnLog?.Invoke($"✅ Attached to {processName}.exe (PID: {_gameProcess.Id})", LogLevel.Success);
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"Attach failed: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    public nint GetModuleBase(string moduleName)
    {
        if (_gameProcess == null) return nint.Zero;
        if (_moduleCache.TryGetValue(moduleName, out var cached))
            return cached;

        try
        {
            _gameProcess.Refresh();
            foreach (ProcessModule mod in _gameProcess.Modules)
            {
                if (mod.ModuleName?.Equals(moduleName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _moduleCache[moduleName] = mod.BaseAddress;
                    return mod.BaseAddress;
                }
            }
        }
        catch { }

        return nint.Zero;
    }

    public uint GetModuleSize(string moduleName)
    {
        if (_gameProcess == null) return 0;
        try
        {
            _gameProcess.Refresh();
            foreach (ProcessModule mod in _gameProcess.Modules)
            {
                if (mod.ModuleName?.Equals(moduleName, StringComparison.OrdinalIgnoreCase) == true)
                    return (uint)mod.ModuleMemorySize;
            }
        }
        catch { }
        return 0;
    }

    public T Read<T>(nint address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        // Use NT-level API for stealth — bypasses user-mode hooks on kernel32
        NtRead(address, buffer, size);
        return BytesToStruct<T>(buffer);
    }

    public byte[] ReadBytes(nint address, int size)
    {
        byte[] buffer = new byte[size];
        NtRead(address, buffer, size);
        return buffer;
    }

    /// <summary>
    /// Reads memory using NtReadVirtualMemory (ntdll) instead of kernel32!ReadProcessMemory.
    /// This bypasses user-mode API hooks that anti-cheat systems may place on kernel32.
    /// Falls back to kernel32 if NT call fails.
    /// </summary>
    private void NtRead(nint address, byte[] buffer, int size)
    {
        int status = NativeMethods.NtReadVirtualMemory(_processHandle, address, buffer, (uint)size, out _);
        if (status < 0) // NTSTATUS error
        {
            // Fallback to kernel32
            NativeMethods.ReadProcessMemory(_processHandle, address, buffer, size, out _);
        }
    }

    public void Write<T>(nint address, T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = StructToBytes(value);
        NtWrite(address, buffer, size);
    }

    public void WriteBytes(nint address, byte[] bytes)
    {
        NtWrite(address, bytes, bytes.Length);
    }

    /// <summary>
    /// Writes memory using NtWriteVirtualMemory (ntdll) instead of kernel32!WriteProcessMemory.
    /// </summary>
    private void NtWrite(nint address, byte[] buffer, int size)
    {
        int status = NativeMethods.NtWriteVirtualMemory(_processHandle, address, buffer, (uint)size, out _);
        if (status < 0)
        {
            NativeMethods.WriteProcessMemory(_processHandle, address, buffer, size, out _);
        }
    }

    public void WriteNop(nint address, int count)
    {
        byte[] nops = new byte[count];
        Array.Fill(nops, (byte)0x90);
        WriteBytes(address, nops);
    }

    public void WriteProtectedBytes(nint address, byte[] bytes)
    {
        // Use NT-level protect for stealth
        nint baseAddr = address;
        uint regionSize = (uint)bytes.Length;
        uint oldProtect;
        int status = NativeMethods.NtProtectVirtualMemory(_processHandle, ref baseAddr, ref regionSize,
            NativeMethods.PAGE_EXECUTE_READWRITE, out oldProtect);
        if (status < 0)
        {
            NativeMethods.VirtualProtectEx(_processHandle, address, (uint)bytes.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE, out oldProtect);
        }

        WriteBytes(address, bytes);

        // Restore original protection
        baseAddr = address;
        regionSize = (uint)bytes.Length;
        status = NativeMethods.NtProtectVirtualMemory(_processHandle, ref baseAddr, ref regionSize,
            oldProtect, out _);
        if (status < 0)
        {
            NativeMethods.VirtualProtectEx(_processHandle, address, (uint)bytes.Length,
                oldProtect, out _);
        }

        // Flush instruction cache to ensure CPU sees our changes
        NativeMethods.FlushInstructionCache(_processHandle, address, (uint)bytes.Length);
    }

    public nint FollowPointer(nint baseAddr, params int[] offsets)
    {
        nint addr = baseAddr;
        foreach (int off in offsets)
        {
            addr = Read<nint>(addr);
            if (addr == nint.Zero) return nint.Zero;
            addr += off;
        }
        return addr;
    }

    public nint Allocate(uint size)
    {
        nint baseAddr = nint.Zero;
        uint regionSize = size;
        int status = NativeMethods.NtAllocateVirtualMemory(_processHandle, ref baseAddr, nint.Zero,
            ref regionSize, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
            NativeMethods.PAGE_EXECUTE_READWRITE);
        if (status < 0)
        {
            return NativeMethods.VirtualAllocEx(_processHandle, nint.Zero, size,
                NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE,
                NativeMethods.PAGE_EXECUTE_READWRITE);
        }
        return baseAddr;
    }

    public void Free(nint address)
    {
        nint baseAddr = address;
        uint regionSize = 0;
        int status = NativeMethods.NtFreeVirtualMemory(_processHandle, ref baseAddr, ref regionSize,
            NativeMethods.MEM_RELEASE);
        if (status < 0)
        {
            NativeMethods.VirtualFreeEx(_processHandle, address, 0, NativeMethods.MEM_RELEASE);
        }
    }

    public bool ChangeProtection(nint address, uint size, uint newProtect, out uint oldProtect)
    {
        nint baseAddr = address;
        uint regionSize = size;
        int status = NativeMethods.NtProtectVirtualMemory(_processHandle, ref baseAddr, ref regionSize,
            newProtect, out oldProtect);
        if (status < 0)
        {
            return NativeMethods.VirtualProtectEx(_processHandle, address, size, newProtect, out oldProtect);
        }
        return status >= 0;
    }

    /// <summary>
    /// Suspends all threads in the target process except the current one.
    /// Use this before making memory modifications to avoid race conditions
    /// with the game's integrity checker threads.
    /// </summary>
    public List<(uint ThreadId, nint Handle)> SuspendAllThreads()
    {
        var suspended = new List<(uint, nint)>();
        if (_gameProcess == null) return suspended;

        try
        {
            nint snapshot = NativeMethods.CreateToolhelp32Snapshot(
                NativeMethods.TH32CS_SNAPTHREAD, 0);
            if (snapshot == nint.Zero || snapshot == new nint(-1)) return suspended;

            var te = new NativeMethods.THREADENTRY32 { dwSize = (uint)Marshal.SizeOf<NativeMethods.THREADENTRY32>() };

            if (NativeMethods.Thread32First(snapshot, ref te))
            {
                uint currentThreadId = (uint)Environment.CurrentManagedThreadId;
                do
                {
                    if (te.th32OwnerProcessID == (uint)_gameProcess.Id &&
                        te.th32ThreadID != currentThreadId)
                    {
                        nint hThread = NativeMethods.OpenThread(
                            NativeMethods.THREAD_SUSPEND_RESUME, false, te.th32ThreadID);
                        if (hThread != nint.Zero)
                        {
                            NativeMethods.SuspendThread(hThread);
                            suspended.Add((te.th32ThreadID, hThread));
                        }
                    }
                }
                while (NativeMethods.Thread32Next(snapshot, ref te));
            }
            NativeMethods.CloseHandle(snapshot);
        }
        catch { }

        return suspended;
    }

    /// <summary>
    /// Resumes previously suspended threads.
    /// </summary>
    public void ResumeThreads(List<(uint ThreadId, nint Handle)> threads)
    {
        foreach (var (_, handle) in threads)
        {
            NativeMethods.ResumeThread(handle);
            NativeMethods.CloseHandle(handle);
        }
    }

    /// <summary>
    /// Suspends the entire process (all threads at once via NT API).
    /// Use for atomic memory modifications.
    /// </summary>
    public bool SuspendProcess()
    {
        return NativeMethods.NtSuspendProcess(_processHandle) >= 0;
    }

    /// <summary>
    /// Resumes the entire process.
    /// </summary>
    public bool ResumeProcess()
    {
        return NativeMethods.NtResumeProcess(_processHandle) >= 0;
    }

    public nint CreateRemoteThread(nint startAddress, nint parameter = default)
    {
        NativeMethods.CreateRemoteThread(_processHandle, nint.Zero, 0, startAddress,
            parameter, 0, out _);
        return nint.Zero;
    }

    public void Detach()
    {
        if (_processHandle != nint.Zero)
        {
            NativeMethods.CloseHandle(_processHandle);
            _processHandle = nint.Zero;
        }
        _gameProcess = null;
        _isAttached = false;
        _moduleCache.Clear();
        OnLog?.Invoke("Detached from game.", LogLevel.Info);
    }

    private static T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try { return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject())!; }
        finally { handle.Free(); }
    }

    private static byte[] StructToBytes<T>(T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] arr = new byte[size];
        nint ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally { Marshal.FreeHGlobal(ptr); }
        return arr;
    }

    public void Dispose() => Detach();
}
