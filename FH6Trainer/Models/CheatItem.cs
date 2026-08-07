using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Polootyyy.Models;

public enum CheatCategory { Currency, Vehicle, Gameplay, Progression, Unlocks }

public class CheatItem : INotifyPropertyChanged
{
    private bool _isEnabled;
    private double _value;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CheatCategory Category { get; set; }
    public string? Hotkey { get; set; }
    public bool IsSlider { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
    }

    public double Value
    {
        get => _value;
        set { if (_value != value) { _value = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
