# CI Build Patcher - Fixes compatibility issues for MAUI Windows build
# This script patches the TEMPORARY CI copy only, not the archived source files.

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectDir
)

$ErrorActionPreference = "Stop"
Write-Host "=== CI Build Patcher ==="
Write-Host "Project directory: $ProjectDir"

# 1. Fix csproj: replace invalid WebView2 package reference
$projPath = Join-Path $ProjectDir "FH6Trainer.csproj"
Write-Host "Patching csproj: $projPath"
$projContent = Get-Content $projPath -Raw
$projContent = $projContent.Replace(
    '<PackageReference Include="Microsoft.Web.WebView2.Core.Projection" Version="1.0.3179.45" />',
    '<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3179.45" />'
)
Set-Content -Path $projPath -Value $projContent -NoNewline
Write-Host "  Fixed WebView2 package reference"

# 2. Add MAUI.Controls package via dotnet CLI
Write-Host "Adding Microsoft.Maui.Controls package..."
dotnet add "$projPath" package Microsoft.Maui.Controls --version 10.0.20 2>&1 | Out-Host
Write-Host "  MAUI.Controls added"

# 3. Fix HotkeyService.cs: replace WPF key types
$hotkeyPath = Join-Path $ProjectDir "Services" "HotkeyService.cs"
Write-Host "Patching HotkeyService: $hotkeyPath"
$content = Get-Content $hotkeyPath -Raw

# Replace using statements - remove WPF Input reference
$content = $content.Replace("using System.Windows.Input;", "// using System.Windows.Input; // patched for MAUI compat")

# Replace Register method signature
$content = $content.Replace(
    "public int Register(string name, System.Windows.Input.Key key, ModifierKeys mods, Action action)",
    "public int Register(string name, uint key, ModifierKeys mods, Action action)"
)

# Replace KeyInterop call
$content = $content.Replace(
    "uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);",
    "uint vk = key;"
)

# Add ModifierKeys enum
if (-not $content.Contains("public enum ModifierKeys")) {
    $enumDef = @"

[Flags]
public enum ModifierKeys : uint
{
    Alt = MOD_ALT,
    Control = MOD_CONTROL,
    Shift = MOD_SHIFT
}
"@
    $content = $content.Replace(
        "private readonly Dictionary<int, (string Name, Action Action)> _hotkeys = new();",
        $enumDef + "`r`n`r`n    private readonly Dictionary<int, (string Name, Action Action)> _hotkeys = new();"
    )
    Write-Host "  Added ModifierKeys enum"
}

Set-Content -Path $hotkeyPath -Value $content -NoNewline
Write-Host "  HotkeyService patched"

# 4. Fix SignatureService.cs: replace Reloaded.Memory.Sigscan with manual scanner
$sigPath = Join-Path $ProjectDir "Services" "SignatureService.cs"
Write-Host "Patching SignatureService: $sigPath"
$sigContent = Get-Content $sigPath -Raw

# Remove Reloaded usings
$sigContent = $sigContent.Replace("using Reloaded.Memory.Sigscan;`r`n", "")
$sigContent = $sigContent.Replace("using Reloaded.Memory.Sigscan.Definitions;`r`n", "")

# Replace scanner block
$sigContent = $sigContent.Replace(
    'var scanner = new Scanner(patternBytes, mask);',
    '// Scanner replaced with manual FindPattern for MAUI compat'
)
$sigContent = $sigContent.Replace(
    'var result = scanner.FindPattern(moduleBytes);',
    'long foundOffset = FindPattern(moduleBytes, patternBytes, mask);'
)
$sigContent = $sigContent.Replace(
    'if (result != -1)',
    'if (foundOffset != -1)'
)
$sigContent = $sigContent.Replace(
    'nint address = moduleBase + result + additionalOffset;',
    'nint address = moduleBase + (int)foundOffset + additionalOffset;'
)

# Add FindPattern helper before last closing brace
$helper = @'

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
'@

$lastBrace = $sigContent.LastIndexOf("}")
if ($lastBrace -gt 0) {
    $sigContent = $sigContent.Insert($lastBrace, $helper)
}

Set-Content -Path $sigPath -Value $sigContent -NoNewline
Write-Host "  SignatureService patched"

Write-Host "=== CI Build Patcher Complete ==="
