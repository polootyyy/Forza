using System.Runtime.InteropServices;

namespace Polootyyy.Services;

public class HotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;

    
[Flags]
public enum ModifierKeys : uint
{
    Alt = MOD_ALT,
    Control = MOD_CONTROL,
    Shift = MOD_SHIFT
}

    private readonly Dictionary<int, (string Name, Action Action)> _hotkeys = new();
    private int _nextId = 1;
    private nint _windowHandle;

    public event Action<string>? OnHotkeyPressed;

    public void Initialize(nint windowHandle) => _windowHandle = windowHandle;

    public int Register(string name, uint key, ModifierKeys mods, Action action)
    {
        uint m = 0;
        if (mods.HasFlag(ModifierKeys.Alt)) m |= MOD_ALT;
        if (mods.HasFlag(ModifierKeys.Control)) m |= MOD_CONTROL;
        if (mods.HasFlag(ModifierKeys.Shift)) m |= MOD_SHIFT;
        m |= MOD_NOREPEAT;

        uint vk = key;
        int id = _nextId++;

        if (RegisterHotKey(_windowHandle, id, m, vk))
        {
            _hotkeys[id] = (name, action);
            return id;
        }
        return -1;
    }

    public void Unregister(int id)
    {
        if (_hotkeys.ContainsKey(id))
        {
            UnregisterHotKey(_windowHandle, id);
            _hotkeys.Remove(id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Keys.ToList())
            UnregisterHotKey(_windowHandle, id);
        _hotkeys.Clear();
    }

    public void HandleMessage(int id)
    {
        if (_hotkeys.TryGetValue(id, out var entry))
        {
            OnHotkeyPressed?.Invoke(entry.Name);
            entry.Action?.Invoke();
        }
    }

    public void Dispose() => UnregisterAll();
}
