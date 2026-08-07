using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Polootyyy.Models;
using Polootyyy.Services;

namespace Polootyyy.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly MemoryService _memory;
    private readonly CheatEngineService _cheats;
    private readonly HotkeyService _hotkey;

    public ObservableCollection<CheatItem> Cheats { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    private bool _isAttached;
    public bool IsAttached { get => _isAttached; set { _isAttached = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotAttached)); } }
    public bool IsNotAttached => !IsAttached;

    private bool _isScanning;
    public bool IsScanning { get => _isScanning; set { _isScanning = value; OnPropertyChanged(); } }

    private string _statusText = "Not attached - Click Attach to begin";
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

    private float _driftMultiplier = 10f;
    public float DriftMultiplier { get => _driftMultiplier; set { _driftMultiplier = value; OnPropertyChanged(); } }

    private float _speedMultiplier = 2f;
    public float SpeedMultiplier { get => _speedMultiplier; set { _speedMultiplier = value; OnPropertyChanged(); } }

    private float _sellFactor = 10f;
    public float SellFactor { get => _sellFactor; set { _sellFactor = value; OnPropertyChanged(); } }

    public ICommand AttachCommand { get; }
    public ICommand DetachCommand { get; }
    public ICommand RescanCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand ResetAllCommand { get; }

    public MainViewModel(MemoryService memory, CheatEngineService cheats, HotkeyService hotkey)
    {
        _memory = memory;
        _cheats = cheats;
        _hotkey = hotkey;

        AttachCommand = new Command(async () => await Attach());
        DetachCommand = new Command(Detach);
        RescanCommand = new Command(async () => await Rescan());
        ClearLogCommand = new Command(() => LogEntries.Clear());
        ResetAllCommand = new Command(() => _cheats.ResetAll());

        _memory.OnLog += AddLog;
        _cheats.OnLog += AddLog;

        InitCheats();
    }

    private void InitCheats()
    {
        // Currency
        Cheats.Add(new() { Name = "Infinite Credits", Description = "999,999,999 CR", Category = CheatCategory.Currency, Hotkey = "Ctrl+F1" });
        Cheats.Add(new() { Name = "Infinite Wheelspins", Description = "999 spins", Category = CheatCategory.Currency, Hotkey = "Ctrl+F2" });
        Cheats.Add(new() { Name = "Infinite Super Spins", Description = "999 super spins", Category = CheatCategory.Currency, Hotkey = "Ctrl+F3" });
        Cheats.Add(new() { Name = "Infinite Forzathon", Description = "9,999 FP", Category = CheatCategory.Currency, Hotkey = "Ctrl+F4" });
        // Gameplay
        Cheats.Add(new() { Name = "Speed Multiplier", Description = "Boost speed", Category = CheatCategory.Vehicle, IsSlider = true, MinValue = 1, MaxValue = 10, Value = 2, Hotkey = "Ctrl+F5" });
        Cheats.Add(new() { Name = "Drift Multiplier", Description = "Boost drift score", Category = CheatCategory.Gameplay, IsSlider = true, MinValue = 1, MaxValue = 100, Value = 10, Hotkey = "Ctrl+F6" });
        Cheats.Add(new() { Name = "Infinite Skill Points", Description = "999 SP", Category = CheatCategory.Gameplay, Hotkey = "Ctrl+F7" });
        Cheats.Add(new() { Name = "No Skill Break", Description = "Skills never break", Category = CheatCategory.Gameplay, Hotkey = "Ctrl+Shift+F7" });
        // Progression
        Cheats.Add(new() { Name = "Max XP", Description = "Max level XP", Category = CheatCategory.Progression, Hotkey = "Ctrl+F8" });
        Cheats.Add(new() { Name = "Sell Factor", Description = "Multiply sell price", Category = CheatCategory.Progression, IsSlider = true, MinValue = 1, MaxValue = 100, Value = 10, Hotkey = "Ctrl+Shift+F8" });
        // Unlocks
        Cheats.Add(new() { Name = "Unlock All", Description = "Everything unlocked", Category = CheatCategory.Unlocks, Hotkey = "Ctrl+F9" });
        Cheats.Add(new() { Name = "All Cars", Description = "Full car collection", Category = CheatCategory.Unlocks, Hotkey = "Ctrl+F10" });
        Cheats.Add(new() { Name = "Autoshow Unlock", Description = "All in autoshow", Category = CheatCategory.Unlocks, Hotkey = "Ctrl+Shift+F9" });
        Cheats.Add(new() { Name = "Free Cars", Description = "Cars cost 0 CR", Category = CheatCategory.Unlocks, Hotkey = "Ctrl+Shift+F10" });
        Cheats.Add(new() { Name = "Free Upgrades", Description = "Upgrades cost 0 CR", Category = CheatCategory.Unlocks });
        Cheats.Add(new() { Name = "Hidden Cars", Description = "Reveal hidden cars", Category = CheatCategory.Unlocks });
        Cheats.Add(new() { Name = "Install Flags", Description = "All install flags", Category = CheatCategory.Unlocks });
        Cheats.Add(new() { Name = "Clear New Tags", Description = "Remove NEW badges", Category = CheatCategory.Unlocks });

        // Wire toggle events
        foreach (var cheat in Cheats)
        {
            cheat.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CheatItem.IsEnabled))
                    ToggleCheat((CheatItem)s!);
            };
        }
    }

    private async Task Attach()
    {
        IsScanning = true;
        StatusText = "Searching for Forza Horizon 6...";
        AddLog("🔍 Searching for game process...", LogLevel.Info);

        await Task.Run(() =>
        {
            if (_memory.AttachToGame("ForzaHorizon6"))
            {
                IsAttached = true;
                StatusText = "Initializing cheats...";
                _cheats.Initialize();
                StatusText = "✅ Ready - All systems online";
            }
            else
            {
                StatusText = "❌ Game not found. Is FH6 running?";
            }
        });

        IsScanning = false;
    }

    private void Detach()
    {
        _cheats.ResetAll();
        _memory.Detach();
        IsAttached = false;
        StatusText = "Not attached";
        foreach (var c in Cheats) c.IsEnabled = false;
    }

    private async Task Rescan()
    {
        if (!IsAttached) return;
        IsScanning = true;
        StatusText = "Rescanning offsets...";
        await Task.Run(() => _cheats.Initialize());
        StatusText = "✅ Ready";
        IsScanning = false;
    }

    public void ToggleCheat(CheatItem cheat)
    {
        if (!IsAttached || cheat == null) return;

        switch (cheat.Name)
        {
            case "Infinite Credits": _cheats.SetCredits(cheat.IsEnabled ? 999_999_999 : 0); break;
            case "Infinite Wheelspins": _cheats.SetWheelspins(cheat.IsEnabled ? 999 : 0); break;
            case "Infinite Super Spins": _cheats.SetSuperWheelspins(cheat.IsEnabled ? 999 : 0); break;
            case "Infinite Forzathon": _cheats.SetForzathonPoints(cheat.IsEnabled ? 9999 : 0); break;
            case "Speed Multiplier": if (cheat.IsEnabled) _cheats.SetSpeedMultiplier(SpeedMultiplier); break;
            case "Drift Multiplier": if (cheat.IsEnabled) _cheats.SetDriftMultiplier(DriftMultiplier); break;
            case "Infinite Skill Points": _cheats.SetSkillPoints(cheat.IsEnabled ? 999 : 0); break;
            case "No Skill Break": _cheats.NoSkillBreak(cheat.IsEnabled); break;
            case "Max XP": _cheats.SetXP(cheat.IsEnabled ? 999_999_999 : 0); break;
            case "Sell Factor": if (cheat.IsEnabled) _cheats.SetSellFactor(SellFactor); break;
            case "Unlock All": _cheats.UnlockAll(cheat.IsEnabled); break;
            case "All Cars": _cheats.UnlockAllCars(cheat.IsEnabled); break;
            case "Autoshow Unlock": _cheats.AutoshowUnlock(cheat.IsEnabled); break;
            case "Free Cars": _cheats.FreeCars(cheat.IsEnabled); break;
            case "Free Upgrades": _cheats.FreeUpgrades(cheat.IsEnabled); break;
            case "Hidden Cars": _cheats.HiddenCars(cheat.IsEnabled); break;
            case "Install Flags": _cheats.InstallFlags(cheat.IsEnabled); break;
            case "Clear New Tags": _cheats.ClearNewTag(cheat.IsEnabled); break;
        }
    }

    private void AddLog(string msg, LogLevel level)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogEntries.Add(new LogEntry { Message = msg, Level = level });
            while (LogEntries.Count > 200) LogEntries.RemoveAt(0);
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
