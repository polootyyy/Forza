using Polootyyy.Models;

namespace Polootyyy.Services;

/// <summary>
/// Comprehensive anti-tamper bypass for Forza Horizon 6.
/// Patches multiple CRC check locations, integrity verification functions,
/// and uses a high-frequency re-application timer to defeat runtime re-patching.
/// </summary>
public class CrcBypassEngine : IDisposable
{
    private readonly MemoryService _memory;
    private readonly SignatureService _signature;
    private readonly Dictionary<string, (nint Address, byte[] Original)> _patches = new();
    private bool _crcBypassActive;
    private System.Timers.Timer? _crcTimer;
    private System.Timers.Timer? _fastTimer;
    private bool _disposed;

    public bool IsBypassActive => _crcBypassActive;
    public event Action<string, LogLevel>? OnLog;

    // ── CRC / Integrity check signatures ──────────────────────────
    // These target the actual integrity verification functions in FH6.
    // Each signature is more specific to avoid false matches.

    // Primary CRC check dispatcher — patch to return immediately
    private const string CrcCheckPrimary =
        "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 48 8B D9";

    // Secondary integrity verification — NOP the call
    private const string IntegrityVerifyCall =
        "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9";

    // Memory region hash check — return 0 (match)
    private const string HashCheckFunction =
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 05 ?? ?? ?? ?? 48 8B F2 48 8B F9";

    // Anti-debug / anti-tamper detection — NOP it out
    private const string AntiTamperDetect =
        "48 83 EC 28 E8 ?? ?? ?? ?? 84 C0 75 ?? 48 83 C4 28 C3";

    // Thread creation monitor — patch to skip
    private const string ThreadMonitor =
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B 05 ?? ?? ?? ?? 48 8B F9 48 8B DA";

    // ── Additional bypass: patch the game's own RPM/WPM wrappers ──
    // These are internal functions the game uses to read/write its own memory.
    // By hooking them, we can hide our modifications from the game's scanner.

    private const string InternalMemCompare =
        "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B D9 48 8B 0D";

    public CrcBypassEngine(MemoryService memory, SignatureService signature)
    {
        _memory = memory;
        _signature = signature;
    }

    public bool Initialize()
    {
        if (!_memory.IsAttached) return false;

        try
        {
            int patched = 0;
            int failed = 0;

            // Patch each CRC/integrity location
            patched += TryPatch("CrcPrimary", CrcCheckPrimary, PatchReturnZero, 16);
            patched += TryPatch("IntegrityVerify", IntegrityVerifyCall, PatchNop, 5);
            patched += TryPatch("HashCheck", HashCheckFunction, PatchReturnZero, 16);
            patched += TryPatch("AntiTamper", AntiTamperDetect, PatchNop, 5);
            patched += TryPatch("ThreadMonitor", ThreadMonitor, PatchReturnZero, 16);
            patched += TryPatch("MemCompare", InternalMemCompare, PatchReturnZero, 16);

            // Also scan for and NOP any remaining E8 calls to integrity functions
            PatchIntegrityCallSites();

            if (patched > 0)
            {
                OnLog?.Invoke($"🛡️ CRC bypass: {patched} patches applied, {failed} failed", LogLevel.Success);
            }
            else
            {
                OnLog?.Invoke("⚠️ No CRC patterns matched — game may have updated", LogLevel.Warning);
            }

            // Start dual timers: fast (500ms) for critical patches, slow (5s) for others
            StartCrcTimers();

            _crcBypassActive = true;
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"CRC bypass init failed: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    private int TryPatch(string name, string signature, Action<nint, int> patchAction, int patchSize)
    {
        var addr = _signature.FindPattern("ForzaHorizon6.exe", signature);
        if (addr == nint.Zero)
        {
            OnLog?.Invoke($"  ⚠ {name}: pattern not found", LogLevel.Warning);
            return 0;
        }

        try
        {
            var original = _memory.ReadBytes(addr, patchSize);
            _patches[name] = (addr, original);
            patchAction(addr, patchSize);
            OnLog?.Invoke($"  ✔ {name} @ 0x{addr:X}", LogLevel.Info);
            return 1;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"  ✘ {name}: {ex.Message}", LogLevel.Error);
            return 0;
        }
    }

    private void PatchReturnZero(nint address, int size)
    {
        // xor eax, eax; ret — makes the function return 0 (success/ok)
        byte[] patch = { 0x31, 0xC0, 0xC3 };
        _memory.WriteProtectedBytes(address, patch);
    }

    private void PatchNop(nint address, int size)
    {
        _memory.WriteNop(address, size);
    }

    /// <summary>
    /// Scans for CALL instructions targeting integrity functions and NOPs them.
    /// This catches indirect call sites that the main signatures might miss.
    /// </summary>
    private void PatchIntegrityCallSites()
    {
        try
        {
            nint moduleBase = _memory.GetModuleBase("ForzaHorizon6.exe");
            uint moduleSize = _memory.GetModuleSize("ForzaHorizon6.exe");
            if (moduleBase == nint.Zero || moduleSize == 0) return;

            // Read the .text section (first 100MB should cover it)
            int scanSize = (int)Math.Min(moduleSize, 100_000_000);
            byte[] code = _memory.ReadBytes(moduleBase, scanSize);

            int callSitesPatched = 0;

            // Look for: E8 ?? ?? ?? ?? 84 C0 0F 84 (call + test al,al + jz far)
            // This pattern is common for integrity check call sites
            byte[] callTestPattern = { 0xE8, 0x00, 0x00, 0x00, 0x00, 0x84, 0xC0, 0x0F, 0x84 };
            bool[] callTestMask = { true, false, false, false, false, true, true, true, true };

            for (int i = 0; i < code.Length - callTestPattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < callTestPattern.Length; j++)
                {
                    if (callTestMask[j] && code[i + j] != callTestPattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match && callSitesPatched < 20) // Limit to 20 call sites
                {
                    nint callAddr = moduleBase + i;
                    // NOP the 5-byte CALL + 2-byte TEST AL,AL = 7 bytes total
                    // Then also NOP the conditional jump (6 bytes)
                    _memory.WriteNop(callAddr, 5);  // NOP the CALL
                    _memory.WriteNop(callAddr + 5, 2); // NOP TEST AL,AL
                    // Change JZ to JMP (always skip the error path)
                    // 0F 84 -> 90 E9 (NOP + JMP)
                    byte[] jmpPatch = { 0x90, 0xE9 };
                    _memory.WriteProtectedBytes(callAddr + 7, jmpPatch);
                    callSitesPatched++;
                    i += 12; // Skip ahead
                }
            }

            if (callSitesPatched > 0)
                OnLog?.Invoke($"  ✔ NOP'd {callSitesPatched} integrity call sites", LogLevel.Info);
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"  ⚠ Call site scan: {ex.Message}", LogLevel.Warning);
        }
    }

    private void StartCrcTimers()
    {
        // Fast timer: re-apply critical patches every 500ms
        _fastTimer = new System.Timers.Timer(500);
        _fastTimer.Elapsed += (_, _) =>
        {
            if (!_memory.IsAttached || _disposed)
            {
                StopCrcTimers();
                return;
            }
            ReapplyCriticalPatches();
        };
        _fastTimer.AutoReset = true;
        _fastTimer.Start();

        // Slow timer: full re-verify every 5 seconds
        _crcTimer = new System.Timers.Timer(5000);
        _crcTimer.Elapsed += (_, _) =>
        {
            if (!_memory.IsAttached || _disposed)
            {
                StopCrcTimers();
                return;
            }
            EnsureAllPatches();
        };
        _crcTimer.AutoReset = true;
        _crcTimer.Start();
    }

    private void ReapplyCriticalPatches()
    {
        try
        {
            // Only re-apply the most critical patches at high frequency
            var critical = new[] { "CrcPrimary", "IntegrityVerify", "AntiTamper" };
            foreach (var name in critical)
            {
                if (_patches.TryGetValue(name, out var p))
                {
                    var current = _memory.ReadBytes(p.Address, name switch
                    {
                        "IntegrityVerify" or "AntiTamper" => 5,
                        _ => 3
                    });

                    bool needsReapply = name switch
                    {
                        "IntegrityVerify" or "AntiTamper" =>
                            current[0] != 0x90 || current[1] != 0x90,
                        _ =>
                            current[0] != 0x31 || current[1] != 0xC0 || current[2] != 0xC3
                    };

                    if (needsReapply)
                    {
                        if (name is "IntegrityVerify" or "AntiTamper")
                            _memory.WriteNop(p.Address, 5);
                        else
                        {
                            byte[] patch = { 0x31, 0xC0, 0xC3 };
                            _memory.WriteProtectedBytes(p.Address, patch);
                        }
                    }
                }
            }
        }
        catch { /* silently continue — game may be in a critical section */ }
    }

    private void EnsureAllPatches()
    {
        try
        {
            foreach (var (name, (addr, _)) in _patches)
            {
                int expectedSize = name is "IntegrityVerify" or "AntiTamper" ? 5 : 3;
                var current = _memory.ReadBytes(addr, expectedSize);

                bool intact = name switch
                {
                    "IntegrityVerify" or "AntiTamper" =>
                        current[0] == 0x90 && current[1] == 0x90,
                    _ =>
                        current[0] == 0x31 && current[1] == 0xC0 && current[2] == 0xC3
                };

                if (!intact)
                {
                    if (name is "IntegrityVerify" or "AntiTamper")
                        _memory.WriteNop(addr, 5);
                    else
                    {
                        byte[] patch = { 0x31, 0xC0, 0xC3 };
                        _memory.WriteProtectedBytes(addr, patch);
                    }
                }
            }
        }
        catch { /* silently continue */ }
    }

    public void StopCrcTimers()
    {
        _fastTimer?.Stop();
        _fastTimer?.Dispose();
        _fastTimer = null;

        _crcTimer?.Stop();
        _crcTimer?.Dispose();
        _crcTimer = null;
    }

    public void RestoreAll()
    {
        StopCrcTimers();
        foreach (var (name, (addr, original)) in _patches)
        {
            try
            {
                _memory.WriteProtectedBytes(addr, original);
                OnLog?.Invoke($"Restored: {name}", LogLevel.Info);
            }
            catch { }
        }
        _patches.Clear();
        _crcBypassActive = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        RestoreAll();
    }
}
