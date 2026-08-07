# Polootyyy - Forza Horizon 6 Trainer

A powerful, feature-rich trainer for Forza Horizon 6 built with .NET MAUI.

## Features

### 💰 Currency
- **Infinite Credits** - Set credits to 999,999,999 (Ctrl+F1)
- **Infinite Wheelspins** - 999 wheelspins (Ctrl+F2)
- **Infinite Super Wheelspins** - 999 super spins (Ctrl+F3)
- **Infinite Forzathon Points** - 9,999 FP (Ctrl+F4)

### 🎮 Gameplay
- **Drift Multiplier** - 1-100x drift score boost (Ctrl+F6)
- **Speed Multiplier** - 1-10x speed boost (Ctrl+F5)
- **Infinite Skill Points** - 999 SP (Ctrl+F7)
- **No Skill Break** - Skills never break (Ctrl+Shift+F7)

### 📈 Progression
- **Max XP** - Max level XP (Ctrl+F8)
- **Sell Factor** - 1-100x sell price multiplier (Ctrl+Shift+F8)

### 🔓 Unlocks
- **Unlock All** - Everything unlocked (Ctrl+F9)
- **All Cars** - Full car collection (Ctrl+F10)
- **Autoshow Unlock** - All cars in autoshow (Ctrl+Shift+F9)
- **Free Cars** - Cars cost 0 CR (Ctrl+Shift+F10)
- **Free Upgrades** - Upgrades cost 0 CR
- **Hidden Cars** - Reveal hidden cars
- **Install Flags** - All install flags
- **Clear New Tags** - Remove NEW badges

### 🛡️ Anti-Cheat Bypass
- CRC check bypass with auto-reapply timer
- Profile hook patching
- SeDebugPrivilege escalation

## Requirements

- Windows 10/11 (x64)
- .NET 10 SDK
- Forza Horizon 6 running

## How to Build

### Option 1: Using build.bat
```
build.bat
```

### Option 2: Manual
```powershell
# Install MAUI workload
dotnet workload install maui-windows

# Restore packages
dotnet restore FH6Trainer.csproj

# Build Release
dotnet publish FH6Trainer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## How to Use

1. **Run as Administrator** - Right-click `Polootyyy.exe` → Run as Administrator
2. Launch Forza Horizon 6
3. Click **ATTACH** in the trainer
4. Toggle cheats on/off using the switches or hotkeys
5. Use **EMERGENCY RESET** to restore all patches

## Tech Stack

- .NET 10 MAUI (Windows x64)
- Reloaded.Memory.Sigscan for pattern scanning
- P/Invoke for memory operations
- MVVM architecture with dependency injection

## Credits

Built by Polootyyy - Based on reverse engineering research.
