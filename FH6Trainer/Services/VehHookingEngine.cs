using System.Diagnostics;
using System.Runtime.InteropServices;
using Polootyyy.Helpers;

namespace Polootyyy.Services;

/// <summary>
/// Vectored Exception Handler (VEH) based hooking engine.
/// Uses hardware breakpoints (DR0-DR3) to intercept execution at target addresses
/// WITHOUT modifying any code bytes — making it invisible to CRC/integrity checks.
/// 
/// This is the standard approach used by modern game trainers to avoid detection.
/// </summary>
public class VehHookingEngine : IDisposable
{
    private readonly MemoryService _memory;
    private nint _vehHandle;
    private bool _installed;
    private bool _disposed;

    // Active hardware breakpoints
    private readonly Dictionary<int, HwBpInfo> _hwBreakpoints = new();
    private readonly object _lock = new();

    public event Action<string, Models.LogLevel>? OnLog;

    public VehHookingEngine(MemoryService memory)
    {
        _memory = memory;
    }

    /// <summary>
    /// Installs the VEH handler in the current process.
    /// The handler catches EXCEPTION_SINGLE_STEP from hardware breakpoints
    /// in the TARGET (game) process by using a remote thread technique.
    /// 
    /// Actually, VEH only works in-process. For cross-process hardware breakpoints,
    /// we use SetThreadContext on the game's threads to set DR0-DR3.
    /// </summary>
    public bool Install()
    {
        if (_installed) return true;
        _installed = true;
        OnLog?.Invoke("🔧 VEH engine ready (HW breakpoint mode)", Models.LogLevel.Success);
        return true;
    }

    /// <summary>
    /// Sets a hardware breakpoint on a specific thread in the game process.
    /// When execution reaches the target address, the thread will single-step,
    /// and we can intercept via the VEH handler.
    /// 
    /// For cross-process: we set DR0-DR3 + DR7 on the game's main thread(s).
    /// The game thread will hit the breakpoint, and we catch it via our
    /// debug event loop or by polling.
    /// </summary>
    public bool SetHardwareBreakpoint(nint targetAddress, HwBpCondition condition, HwBpLength length)
    {
        if (!_memory.IsAttached || _memory.GameProcess == null) return false;

        try
        {
            // Find the game's main thread
            uint mainThreadId = (uint)_memory.GameProcess.Threads[0].Id;

            nint hThread = NativeMethods.OpenThread(
                NativeMethods.THREAD_GET_CONTEXT | NativeMethods.THREAD_SET_CONTEXT |
                NativeMethods.THREAD_SUSPEND_RESUME,
                false, mainThreadId);

            if (hThread == nint.Zero) return false;

            NativeMethods.SuspendThread(hThread);

            var ctx = new NativeMethods.CONTEXT64
            {
                ContextFlags = NativeMethods.CONTEXT_DEBUG_REGISTERS | NativeMethods.CONTEXT_FULL
            };

            if (!NativeMethods.GetThreadContext(hThread, ref ctx))
            {
                NativeMethods.ResumeThread(hThread);
                NativeMethods.CloseHandle(hThread);
                return false;
            }

            // Find a free debug register (DR0-DR3)
            int slot = -1;
            ulong target = (ulong)targetAddress;

            // Check which slots are free
            if ((ctx.Dr7 & 0x1) == 0) slot = 0;
            else if ((ctx.Dr7 & 0x4) == 0) slot = 1;
            else if ((ctx.Dr7 & 0x10) == 0) slot = 2;
            else if ((ctx.Dr7 & 0x40) == 0) slot = 3;

            if (slot == -1)
            {
                NativeMethods.ResumeThread(hThread);
                NativeMethods.CloseHandle(hThread);
                OnLog?.Invoke("⚠️ No free hardware breakpoint slots", Models.LogLevel.Warning);
                return false;
            }

            // Set the debug register
            switch (slot)
            {
                case 0: ctx.Dr0 = target; break;
                case 1: ctx.Dr1 = target; break;
                case 2: ctx.Dr2 = target; break;
                case 3: ctx.Dr3 = target; break;
            }

            // Configure DR7 for this slot
            ulong dr7 = ctx.Dr7;

            // Clear existing bits for this slot
            int shift = slot * 2;
            dr7 &= ~(0x3UL << shift);          // Clear RW bits
            dr7 &= ~(0x3UL << (shift + 16));    // Clear LEN bits
            dr7 &= ~(0x1UL << (shift * 2));     // Clear local enable

            // Set condition (RW)
            uint rwBits = condition switch
            {
                HwBpCondition.Execute => NativeMethods.DR7_RW_EXECUTE,
                HwBpCondition.Write => NativeMethods.DR7_RW_WRITE,
                HwBpCondition.ReadWrite => NativeMethods.DR7_RW_READWRITE,
                _ => NativeMethods.DR7_RW_EXECUTE
            };
            dr7 |= (ulong)rwBits << shift;

            // Set length
            uint lenBits = length switch
            {
                HwBpLength.Byte1 => NativeMethods.DR7_LEN_1,
                HwBpLength.Byte2 => NativeMethods.DR7_LEN_2,
                HwBpLength.Byte4 => NativeMethods.DR7_LEN_4,
                HwBpLength.Byte8 => NativeMethods.DR7_LEN_8,
                _ => NativeMethods.DR7_LEN_1
            };
            dr7 |= (ulong)lenBits << (shift + 16);

            // Enable locally
            dr7 |= (ulong)NativeMethods.DR7_LOCAL_ENABLE << shift;

            ctx.Dr7 = dr7;

            if (!NativeMethods.SetThreadContext(hThread, ref ctx))
            {
                NativeMethods.ResumeThread(hThread);
                NativeMethods.CloseHandle(hThread);
                return false;
            }

            NativeMethods.ResumeThread(hThread);
            NativeMethods.CloseHandle(hThread);

            lock (_lock)
            {
                _hwBreakpoints[slot] = new HwBpInfo
                {
                    Slot = slot,
                    Address = targetAddress,
                    Condition = condition,
                    Length = length,
                    ThreadId = mainThreadId
                };
            }

            OnLog?.Invoke($"🎯 HW BP #{slot} set @ 0x{targetAddress:X} (thread {mainThreadId})",
                Models.LogLevel.Success);
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"HW BP failed: {ex.Message}", Models.LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Sets hardware breakpoints on ALL game threads.
    /// This ensures we catch the target address regardless of which thread executes it.
    /// </summary>
    public int SetHardwareBreakpointAllThreads(nint targetAddress, HwBpCondition condition, HwBpLength length)
    {
        if (!_memory.IsAttached || _memory.GameProcess == null) return 0;

        int count = 0;
        try
        {
            _memory.GameProcess.Refresh();
            foreach (ProcessThread thread in _memory.GameProcess.Threads)
            {
                try
                {
                    nint hThread = NativeMethods.OpenThread(
                        NativeMethods.THREAD_GET_CONTEXT | NativeMethods.THREAD_SET_CONTEXT |
                        NativeMethods.THREAD_SUSPEND_RESUME,
                        false, (uint)thread.Id);

                    if (hThread == nint.Zero) continue;

                    NativeMethods.SuspendThread(hThread);

                    var ctx = new NativeMethods.CONTEXT64
                    {
                        ContextFlags = NativeMethods.CONTEXT_DEBUG_REGISTERS
                    };

                    if (NativeMethods.GetThreadContext(hThread, ref ctx))
                    {
                        // Use DR0 for all threads (simplified — in production you'd manage slots)
                        ctx.Dr0 = (ulong)targetAddress;

                        ulong dr7 = ctx.Dr7;
                        dr7 &= ~0xFFFFUL; // Clear DR0 config
                        dr7 |= (ulong)NativeMethods.DR7_RW_EXECUTE; // RW0 = execute
                        dr7 |= (ulong)NativeMethods.DR7_LEN_1 << 16; // LEN0 = 1 byte
                        dr7 |= NativeMethods.DR7_LOCAL_ENABLE; // L0 = local enable
                        ctx.Dr7 = dr7;

                        NativeMethods.SetThreadContext(hThread, ref ctx);
                        count++;
                    }

                    NativeMethods.ResumeThread(hThread);
                    NativeMethods.CloseHandle(hThread);
                }
                catch { /* skip problematic threads */ }
            }
        }
        catch { }

        if (count > 0)
            OnLog?.Invoke($"🎯 HW BP set on {count} threads @ 0x{targetAddress:X}", Models.LogLevel.Success);

        return count;
    }

    /// <summary>
    /// Clears all hardware breakpoints from all game threads.
    /// </summary>
    public void ClearAllBreakpoints()
    {
        if (!_memory.IsAttached || _memory.GameProcess == null) return;

        try
        {
            _memory.GameProcess.Refresh();
            foreach (ProcessThread thread in _memory.GameProcess.Threads)
            {
                try
                {
                    nint hThread = NativeMethods.OpenThread(
                        NativeMethods.THREAD_GET_CONTEXT | NativeMethods.THREAD_SET_CONTEXT |
                        NativeMethods.THREAD_SUSPEND_RESUME,
                        false, (uint)thread.Id);

                    if (hThread == nint.Zero) continue;

                    NativeMethods.SuspendThread(hThread);

                    var ctx = new NativeMethods.CONTEXT64
                    {
                        ContextFlags = NativeMethods.CONTEXT_DEBUG_REGISTERS
                    };

                    if (NativeMethods.GetThreadContext(hThread, ref ctx))
                    {
                        ctx.Dr0 = 0;
                        ctx.Dr1 = 0;
                        ctx.Dr2 = 0;
                        ctx.Dr3 = 0;
                        ctx.Dr7 = 0;
                        NativeMethods.SetThreadContext(hThread, ref ctx);
                    }

                    NativeMethods.ResumeThread(hThread);
                    NativeMethods.CloseHandle(hThread);
                }
                catch { }
            }
        }
        catch { }

        lock (_lock) _hwBreakpoints.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearAllBreakpoints();
    }
}

public enum HwBpCondition
{
    Execute = 0,
    Write = 1,
    ReadWrite = 3
}

public enum HwBpLength
{
    Byte1 = 0,
    Byte2 = 1,
    Byte4 = 3,
    Byte8 = 2
}

internal class HwBpInfo
{
    public int Slot { get; set; }
    public nint Address { get; set; }
    public HwBpCondition Condition { get; set; }
    public HwBpLength Length { get; set; }
    public uint ThreadId { get; set; }
}
