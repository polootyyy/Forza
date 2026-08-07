# CI Build Patcher - Fixes compatibility issues for MAUI Windows build
# This script patches the TEMPORARY CI copy only, not the archived source files.

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectDir
)

Write-Host "=== CI Build Patcher ==="
Write-Host "Project directory: $ProjectDir"

# 1. Fix csproj: replace invalid WebView2 package reference
$projPath = Join-Path $ProjectDir "FH6Trainer.csproj"
Write-Host "Patching csproj: $projPath"
[xml]$projXml = Get-Content $projPath
$found = $false
foreach ($itemGroup in $projXml.Project.ItemGroup) {
    foreach ($pkg in $itemGroup.PackageReference) {
        if ($pkg.Include -eq "Microsoft.Web.WebView2.Core.Projection") {
            $pkg.Include = "Microsoft.Web.WebView2"
            $found = $true
            Write-Host "  Replaced Microsoft.Web.WebView2.Core.Projection -> Microsoft.Web.WebView2"
        }
    }
}
if (-not $found) {
    Write-Host "  WebView2 reference not found (may already be fixed)"
}
$projXml.Save($projPath)

# 2. Fix HotkeyService.cs: replace WPF key types with raw virtual key codes
$hotkeyPath = Join-Path $ProjectDir "Services" "HotkeyService.cs"
Write-Host "Patching HotkeyService: $hotkeyPath"
$content = Get-Content $hotkeyPath -Raw

# Replace the Register method signature to use uint instead of System.Windows.Input.Key
$content = $content.Replace(
    "public int Register(string name, System.Windows.Input.Key key, ModifierKeys mods, Action action)",
    "public int Register(string name, uint key, ModifierKeys mods, Action action)"
)

# Replace KeyInterop call with direct key usage
$content = $content.Replace(
    "uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);",
    "uint vk = key;"
)

# Add ModifierKeys enum if not present (replaces WPF ModifierKeys)
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
Write-Host "  HotkeyService patched successfully"

Write-Host "=== CI Build Patcher Complete ==="
