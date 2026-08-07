using System.Runtime.InteropServices;

namespace Polootyyy.Helpers;

public static class NativeMethods
{
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_VM_WRITE = 0x0020;
    public const uint PROCESS_VM_OPERATION = 0x0008;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_CREATE_THREAD = 0x0002;
    public const uint PROCESS_SET_INFORMATION = 0x0200;
    public const uint PROCESS_ALL_ACCESS = 0x001F0FFF;

    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_RELEASE = 0x8000;
    public const uint MEM_FREE = 0x10000;
    public const uint PAGE_EXECUTE_READWRITE = 0x40;
    public const uint PAGE_READWRITE = 0x04;
    public const uint PAGE_READONLY = 0x02;
    public const uint PAGE_EXECUTE_READ = 0x20;
    public const uint PAGE_GUARD = 0x100;
    public const uint PAGE_NOACCESS = 0x01;

    public const uint TH32CS_SNAPPROCESS = 0x00000002;
    public const uint TH32CS_SNAPMODULE = 0x00000008;
    public const uint TH32CS_SNAPTHREAD = 0x00000004;

    public const uint INFINITE = 0xFFFFFFFF;
    public const uint WAIT_OBJECT_0 = 0x00000000;
    public const uint WAIT_TIMEOUT = 0x00000102;
    public const uint WAIT_FAILED = 0xFFFFFFFF;

    public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    public const string SE_DEBUG_NAME = "SeDebugPrivilege";

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint VirtualAllocEx(nint hProcess, nint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(nint hProcess, nint lpAddress, uint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtectEx(nint hProcess, nint lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, uint dwStackSize, nint lpStartAddress, nint lpParameter, uint dwCreationFlags, out uint lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetExitCodeThread(nint hThread, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetModuleInformation(nint hProcess, nint hModule, out MODULEINFO lpmodinfo, uint cb);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetModuleFileNameEx(nint hProcess, nint hModule, System.Text.StringBuilder lpFilename, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetModuleBaseName(nint hProcess, nint hModule, System.Text.StringBuilder lpBaseName, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool EnumProcessModulesEx(nint hProcess, nint[] lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int GetCurrentProcessId();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool IsWow64Process(nint hProcess, out bool wow64Process);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FlushInstructionCache(nint hProcess, nint lpBaseAddress, uint dwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetProcAddress(nint hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(nint hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Process32First(nint hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Process32Next(nint hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Module32First(nint hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Module32Next(nint hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32First(nint hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32Next(nint hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(nint ProcessHandle, uint DesiredAccess, out nint TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AdjustTokenPrivileges(nint TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, nint PreviousState, nint ReturnLength);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(nint hWnd, int id);

    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEINFO
    {
        public nint lpBaseOfDll;
        public uint SizeOfImage;
        public nint EntryPoint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public nint lpMinimumApplicationAddress;
        public nint lpMaximumApplicationAddress;
        public nint dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public nint th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEENTRY32
    {
        public uint dwSize;
        public uint th32ModuleID;
        public uint th32ProcessID;
        public uint GlblcntUsage;
        public uint ProccntUsage;
        public nint modBaseAddr;
        public uint modBaseSize;
        public nint hModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct THREADENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ThreadID;
        public uint th32OwnerProcessID;
        public int tpBasePri;
        public int tpDeltaPri;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public ushort PartitionId;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    // ── NT-level syscalls for stealthier memory access ─────────────
    // These bypass user-mode hooks that the game may have placed on
    // kernel32!ReadProcessMemory / kernel32!WriteProcessMemory.

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtReadVirtualMemory(
        nint ProcessHandle,
        nint BaseAddress,
        byte[] Buffer,
        uint NumberOfBytesToRead,
        out uint NumberOfBytesRead);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtWriteVirtualMemory(
        nint ProcessHandle,
        nint BaseAddress,
        byte[] Buffer,
        uint NumberOfBytesToWrite,
        out uint NumberOfBytesWritten);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtProtectVirtualMemory(
        nint ProcessHandle,
        ref nint BaseAddress,
        ref uint RegionSize,
        uint NewProtect,
        out uint OldProtect);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtAllocateVirtualMemory(
        nint ProcessHandle,
        ref nint BaseAddress,
        nint ZeroBits,
        ref uint RegionSize,
        uint AllocationType,
        uint Protect);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtFreeVirtualMemory(
        nint ProcessHandle,
        ref nint BaseAddress,
        ref uint RegionSize,
        uint FreeType);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQueryVirtualMemory(
        nint ProcessHandle,
        nint BaseAddress,
        uint MemoryInformationClass,
        nint MemoryInformation,
        uint MemoryInformationLength,
        out uint ReturnLength);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQueryInformationProcess(
        nint ProcessHandle,
        uint ProcessInformationClass,
        nint ProcessInformation,
        uint ProcessInformationLength,
        out uint ReturnLength);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtCreateThreadEx(
        out nint ThreadHandle,
        uint DesiredAccess,
        nint ObjectAttributes,
        nint ProcessHandle,
        nint StartAddress,
        nint Parameter,
        uint CreateFlags,
        nint ZeroBits,
        nint StackSize,
        nint MaximumStackSize,
        nint AttributeList);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtClose(nint Handle);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtSuspendProcess(nint ProcessHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtResumeProcess(nint ProcessHandle);

    // ── VEH (Vectored Exception Handler) ──────────────────────────

    public const uint CALLBACK_FUNCTION = 0x00000000;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate int PVECTORED_EXCEPTION_HANDLER(ref EXCEPTION_POINTERS ExceptionInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint AddVectoredExceptionHandler(
        uint First,
        PVECTORED_EXCEPTION_HANDLER Handler);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint RemoveVectoredExceptionHandler(nint Handle);

    [StructLayout(LayoutKind.Sequential)]
    public struct EXCEPTION_POINTERS
    {
        public nint ExceptionRecord;
        public nint ContextRecord;
    }

    // ── Hardware breakpoint constants ──────────────────────────────

    public const uint EXCEPTION_SINGLE_STEP = 0x80000004;
    public const uint EXCEPTION_BREAKPOINT = 0x80000003;
    public const uint EXCEPTION_ACCESS_VIOLATION = 0xC0000005;
    public const uint EXCEPTION_GUARD_PAGE = 0x80000001;

    public const uint CONTEXT_DEBUG_REGISTERS = 0x00010000;
    public const uint CONTEXT_FULL = 0x00010007;

    // Dr7 bit flags
    public const uint DR7_LOCAL_ENABLE = 0x00000001;
    public const uint DR7_GLOBAL_ENABLE = 0x00000002;
    public const uint DR7_LEN_1 = 0x00000000;
    public const uint DR7_LEN_2 = 0x00040000;
    public const uint DR7_LEN_4 = 0x000C0000;
    public const uint DR7_LEN_8 = 0x00080000;
    public const uint DR7_RW_EXECUTE = 0x00000000;
    public const uint DR7_RW_WRITE = 0x00100000;
    public const uint DR7_RW_READWRITE = 0x00300000;

    // ── Thread context for hardware breakpoints ───────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetThreadContext(nint hThread, ref CONTEXT64 lpContext);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetThreadContext(nint hThread, ref CONTEXT64 lpContext);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint SuspendThread(nint hThread);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint ResumeThread(nint hThread);

    public const uint THREAD_GET_CONTEXT = 0x0008;
    public const uint THREAD_SET_CONTEXT = 0x0010;
    public const uint THREAD_SUSPEND_RESUME = 0x0002;
    public const uint THREAD_QUERY_INFORMATION = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    public struct CONTEXT64
    {
        public ulong P1Home;
        public ulong P2Home;
        public ulong P3Home;
        public ulong P4Home;
        public ulong P5Home;
        public ulong P6Home;
        public uint ContextFlags;
        public uint MxCsr;
        public ushort SegCs;
        public ushort SegDs;
        public ushort SegEs;
        public ushort SegFs;
        public ushort SegGs;
        public ushort SegSs;
        public uint EFlags;
        public ulong Dr0;
        public ulong Dr1;
        public ulong Dr2;
        public ulong Dr3;
        public ulong Dr6;
        public ulong Dr7;
        public ulong Rax;
        public ulong Rcx;
        public ulong Rdx;
        public ulong Rbx;
        public ulong Rsp;
        public ulong Rbp;
        public ulong Rsi;
        public ulong Rdi;
        public ulong R8;
        public ulong R9;
        public ulong R10;
        public ulong R11;
        public ulong R12;
        public ulong R13;
        public ulong R14;
        public ulong R15;
        public ulong Rip;
        // ... remaining fields omitted for brevity
    }

    // ── NtQuerySystemInformation for detecting anti-cheat threads ──

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQuerySystemInformation(
        uint SystemInformationClass,
        nint SystemInformation,
        uint SystemInformationLength,
        out uint ReturnLength);

    public const uint SystemProcessInformation = 5;
    public const uint SystemHandleInformation = 16;
}

