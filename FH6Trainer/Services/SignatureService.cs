using Polootyyy.Models;

namespace Polootyyy.Services;

public class SignatureService
{
    private readonly MemoryService _memory;
    private readonly Dictionary<string, nint> _cache = new();

    public event Action<string, LogLevel>? OnLog;

    public SignatureService(MemoryService memory)
    {
        _memory = memory;
    }

    public nint FindPattern(string moduleName, string pattern, int additionalOffset = 0)
    {
        string cacheKey = $"{moduleName}:{pattern}:{additionalOffset}";
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (!_memory.IsAttached)
        {
            OnLog?.Invoke("Not attached to game.", LogLevel.Warning);
            return nint.Zero;
        }

        try
        {
            nint moduleBase = _memory.GetModuleBase(moduleName);
            if (moduleBase == nint.Zero)
            {
                OnLog?.Invoke($"Module '{moduleName}' not found.", LogLevel.Warning);
                return nint.Zero;
            }

            uint moduleSize = _memory.GetModuleSize(moduleName);
            if (moduleSize == 0)
            {
                OnLog?.Invoke($"Could not read module '{moduleName}' size.", LogLevel.Error);
                return nint.Zero;
            }

            byte[] moduleBytes = _memory.ReadBytes(moduleBase, (int)Math.Min(moduleSize, 200_000_000));
            var (patternBytes, mask) = ParsePattern(pattern);

            // Scanner replaced with manual FindPattern for MAUI compat
            long foundOffset = FindPattern(moduleBytes, patternBytes, mask);

            if (foundOffset != -1)
            {
                nint address = moduleBase + (int)foundOffset + additionalOffset;
                _cache[cacheKey] = address;
                OnLog?.Invoke($"✔ Pattern found in {moduleName} @ 0x{address:X16}", LogLevel.Success);
                return address;
            }

            OnLog?.Invoke($"✘ Pattern not found: {pattern}", LogLevel.Warning);
            return nint.Zero;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"Scan error: {ex.Message}", LogLevel.Error);
            return nint.Zero;
        }
    }

    public nint ResolveRipRelative(nint instructionAddress, int instructionLength)
    {
        if (instructionAddress == nint.Zero) return nint.Zero;
        int displacement = _memory.Read<int>(instructionAddress + instructionLength - 4);
        return instructionAddress + instructionLength + displacement;
    }

    private static (byte[] pattern, string mask) ParsePattern(string patternStr)
    {
        var parts = patternStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        byte[] pattern = new byte[parts.Length];
        char[] mask = new char[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "??" || parts[i] == "?")
            {
                pattern[i] = 0;
                mask[i] = '?';
            }
            else
            {
                pattern[i] = Convert.ToByte(parts[i], 16);
                mask[i] = 'x';
            }
        }

        return (pattern, new string(mask));
    }

    private static long FindPattern(byte[] data, byte[] pattern, string mask)
    {
        for (long i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (mask[j] != '?' && data[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }
}
