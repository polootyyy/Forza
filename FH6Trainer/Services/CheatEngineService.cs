using Polootyyy.Models;

namespace Polootyyy.Services;

public class CheatEngineService
{
    private readonly MemoryService _memory;
    private readonly SignatureService _signature;
    private readonly CrcBypassEngine _crcBypass;
    private readonly VehHookingEngine _veh;
    private readonly Dictionary<string, nint> _resolved = new();
    private bool _initialized;

    public event Action<string, LogLevel>? OnLog;
    public event Action<string, bool>? OnCheatChanged;

    public static class Offsets
    {
        public const string Credits = "Credits";
        public const string Wheelspins = "Wheelspins";
        public const string SuperWheelspins = "SuperWheelspins";
        public const string ForzathonPoints = "ForzathonPoints";
        public const string SkillPoints = "SkillPoints";
        public const string XP = "XP";
        public const string DriftScore = "DriftScore";
        public const string Speed = "Speed";
        public const string UnlockAll = "UnlockAll";
        public const string CarCollection = "CarCollection";
        public const string AutoshowUnlock = "AutoshowUnlock";
        public const string FreeCars = "FreeCars";
        public const string FreeUpgrades = "FreeUpgrades";
        public const string HiddenCars = "HiddenCars";
        public const string InstallFlags = "InstallFlags";
        public const string ClearNewTag = "ClearNewTag";
        public const string SellFactor = "SellFactor";
        public const string NoSkillBreak = "NoSkillBreak";
    }

    // ── Signature definitions ──────────────────────────────────────
    // Each entry: (Module, Signature, ExtraOffset, IsRipRelative, RipOffset)
    // RipOffset = how many bytes into the pattern the RIP-relative displacement starts
    // IsRipRelative = true if the pattern is a RIP-relative instruction (e.g. "48 8B 05 ?? ?? ?? ??")
    //
    // IMPORTANT: Only patterns starting with 48 8B 05 / 48 8B 0D / 48 8D 0D / 48 8D 05 / 
    //            8B 05 / 89 0D / F3 0F 11 05 / 80 3D / C7 05 are RIP-relative.
    //            Patterns like "48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ?? 8B 81" are NOT
    //            simple RIP-relative — the ?? ?? ?? ?? is part of a longer instruction.

    private static readonly Dictionary<string, (string Module, string Sig, int Extra, bool IsRip, int RipOff)> Signatures = new()
    {
        // Currency — these are RIP-relative (48 8B 05 = MOV RAX, [RIP+disp])
        [Offsets.Credits]         = ("ForzaHorizon6.exe", "48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? 8B 88 ?? ?? ?? ??", 0, true, 3),
        [Offsets.Wheelspins]      = ("ForzaHorizon6.exe", "89 0D ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 85 C0", 0, true, 2),
        [Offsets.SuperWheelspins] = ("ForzaHorizon6.exe", "89 0D ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9", 0, true, 2),
        [Offsets.ForzathonPoints] = ("ForzaHorizon6.exe", "8B 05 ?? ?? ?? ?? 89 45 ?? 48 8B 0D", 0, true, 2),
        [Offsets.SkillPoints]     = ("ForzaHorizon6.exe", "89 0D ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? E8", 0, true, 2),
        [Offsets.XP]              = ("ForzaHorizon6.exe", "48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ?? 8B 81 ?? ?? ?? ??", 0, true, 3),
        [Offsets.DriftScore]      = ("ForzaHorizon6.exe", "F3 0F 11 05 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ??", 0, true, 4),
        [Offsets.Speed]           = ("ForzaHorizon6.exe", "F3 0F 11 05 ?? ?? ?? ?? F3 0F 10 0D", 0, true, 4),
        // Unlocks — these are RIP-relative CMP with byte ptr (80 3D = CMP BYTE PTR [RIP+disp], imm)
        [Offsets.UnlockAll]       = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 74 ?? 48 8B 0D", 0, true, 2),
        [Offsets.CarCollection]   = ("ForzaHorizon6.exe", "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 74", 0, true, 3),
        [Offsets.AutoshowUnlock]  = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 74 ?? 48 8D 0D", 0, true, 2),
        [Offsets.FreeCars]        = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 75 ?? 48 8B 0D", 0, true, 2),
        [Offsets.FreeUpgrades]    = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 74 ?? 48 8D 15", 0, true, 2),
        [Offsets.HiddenCars]      = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 74 ?? 48 8B 05", 0, true, 2),
        [Offsets.InstallFlags]    = ("ForzaHorizon6.exe", "C7 05 ?? ?? ?? ?? ?? ?? ?? ?? 48 8B 0D", 0, true, 2),
        [Offsets.ClearNewTag]     = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 74 ?? 48 8B 0D ?? ?? ?? ??", 0, true, 2),
        [Offsets.SellFactor]      = ("ForzaHorizon6.exe", "F3 0F 11 05 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? E8", 0, true, 4),
        [Offsets.NoSkillBreak]    = ("ForzaHorizon6.exe", "80 3D ?? ?? ?? ?? 00 75 ?? 48 8B 0D ?? ?? ?? ?? E8", 0, true, 2),
    };

    public CheatEngineService(MemoryService memory, SignatureService signature, CrcBypassEngine crcBypass, VehHookingEngine veh)
    {
        _memory = memory;
        _signature = signature;
        _crcBypass = crcBypass;
        _veh = veh;
        _memory.OnLog += (m, l) => OnLog?.Invoke(m, l);
        _signature.OnLog += (m, l) => OnLog?.Invoke(m, l);
        _crcBypass.OnLog += (m, l) => OnLog?.Invoke(m, l);
        _veh.OnLog += (m, l) => OnLog?.Invoke(m, l);
    }

    public bool Initialize()
    {
        if (!_memory.IsAttached) return false;
        OnLog?.Invoke("🔍 Scanning memory signatures...", LogLevel.Info);

        // Step 1: Install VEH engine for hardware breakpoint support
        _veh.Install();

        // Step 2: Bypass CRC/integrity checks
        _crcBypass.Initialize();

        // Step 3: Suspend game process for safe scanning
        bool wasSuspended = _memory.SuspendProcess();

        int resolved = 0;
        int failed = 0;
        try
        {
            foreach (var (name, (module, sig, extra, isRip, ripOff)) in Signatures)
            {
                var addr = _signature.FindPattern(module, sig, extra);
                if (addr != nint.Zero)
                {
                    if (isRip)
                    {
                        int disp = _memory.Read<int>(addr + ripOff);
                        nint resolvedAddr = addr + ripOff + 4 + disp;
                        _resolved[name] = resolvedAddr;
                        OnLog?.Invoke($"  ✔ {name} → 0x{resolvedAddr:X}", LogLevel.Info);
                    }
                    else
                    {
                        _resolved[name] = addr;
                        OnLog?.Invoke($"  ✔ {name} → 0x{addr:X}", LogLevel.Info);
                    }
                    resolved++;
                }
                else
                {
                    failed++;
                    OnLog?.Invoke($"  ✘ {name}: pattern not found", LogLevel.Warning);
                }
            }
        }
        finally
        {
            // Resume the game process
            if (wasSuspended)
                _memory.ResumeProcess();
        }

        OnLog?.Invoke($"✅ Resolved {resolved}/{Signatures.Count} offsets ({failed} failed)",
            failed == 0 ? LogLevel.Success : LogLevel.Warning);
        _initialized = true;
        return true;
    }

    public void SetCredits(int amount) { if (TryGet(Offsets.Credits, out var a)) { _memory.Write(a, amount); OnLog?.Invoke($"💰 Credits: {amount:N0}", LogLevel.Success); } }
    public void SetWheelspins(int amount) { if (TryGet(Offsets.Wheelspins, out var a)) { _memory.Write(a, amount); OnLog?.Invoke($"🎰 Wheelspins: {amount}", LogLevel.Success); } }
    public void SetSuperWheelspins(int amount) { if (TryGet(Offsets.SuperWheelspins, out var a)) { _memory.Write(a, amount); OnLog?.Invoke($"🎰 Super: {amount}", LogLevel.Success); } }
    public void SetForzathonPoints(int amount) { if (TryGet(Offsets.ForzathonPoints, out var a)) { _memory.Write(a, amount); OnLog?.Invoke($"🏆 FP: {amount}", LogLevel.Success); } }
    public void SetSkillPoints(int amount) { if (TryGet(Offsets.SkillPoints, out var a)) { _memory.Write(a, amount); OnLog?.Invoke($"⭐ Skill: {amount}", LogLevel.Success); } }
    public void SetXP(int amount) { if (TryGet(Offsets.XP, out var a)) { _memory.Write(a, amount); OnLog?.Invoke($"📈 XP: {amount:N0}", LogLevel.Success); } }
    public void SetDriftMultiplier(float m) { if (TryGet(Offsets.DriftScore, out var a)) { _memory.Write(a, m); OnLog?.Invoke($"🌀 Drift: {m:F1}x", LogLevel.Success); } }
    public void SetSpeedMultiplier(float m) { if (TryGet(Offsets.Speed, out var a)) { _memory.Write(a, m); OnLog?.Invoke($"🏎️ Speed: {m:F1}x", LogLevel.Success); } }
    public void UnlockAll(bool e) { if (TryGet(Offsets.UnlockAll, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🔓 Unlock All ON" : "🔓 Unlock All OFF", LogLevel.Success); } }
    public void UnlockAllCars(bool e) { if (TryGet(Offsets.CarCollection, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🚗 All Cars ON" : "🚗 Cars Default", LogLevel.Success); } }
    public void AutoshowUnlock(bool e) { if (TryGet(Offsets.AutoshowUnlock, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🏪 Autoshow ON" : "🏪 Autoshow OFF", LogLevel.Success); } }
    public void FreeCars(bool e) { if (TryGet(Offsets.FreeCars, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🆓 Free Cars ON" : "🆓 Free Cars OFF", LogLevel.Success); } }
    public void FreeUpgrades(bool e) { if (TryGet(Offsets.FreeUpgrades, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🔧 Free Upgrades ON" : "🔧 Free Upgrades OFF", LogLevel.Success); } }
    public void HiddenCars(bool e) { if (TryGet(Offsets.HiddenCars, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "👻 Hidden Cars ON" : "👻 Hidden Cars OFF", LogLevel.Success); } }
    public void InstallFlags(bool e) { if (TryGet(Offsets.InstallFlags, out var a)) { _memory.Write<int>(a, e ? -1 : 0); OnLog?.Invoke(e ? "🏁 Install Flags ON" : "🏁 Install Flags OFF", LogLevel.Success); } }
    public void ClearNewTag(bool e) { if (TryGet(Offsets.ClearNewTag, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🏷️ Clear Tags ON" : "🏷️ Clear Tags OFF", LogLevel.Success); } }
    public void SetSellFactor(float f) { if (TryGet(Offsets.SellFactor, out var a)) { _memory.Write(a, f); OnLog?.Invoke($"💵 Sell: {f:F1}x", LogLevel.Success); } }
    public void NoSkillBreak(bool e) { if (TryGet(Offsets.NoSkillBreak, out var a)) { _memory.Write<byte>(a, e ? (byte)1 : (byte)0); OnLog?.Invoke(e ? "🔥 No Break ON" : "🔥 No Break OFF", LogLevel.Success); } }

    public void ResetAll()
    {
        _veh.ClearAllBreakpoints();
        _crcBypass.RestoreAll();
        OnLog?.Invoke("🔄 All cheats reset", LogLevel.Success);
    }

    private bool TryGet(string name, out nint addr)
    {
        if (!_initialized) { OnLog?.Invoke("Not initialized.", LogLevel.Warning); addr = nint.Zero; return false; }
        return _resolved.TryGetValue(name, out addr);
    }
}
