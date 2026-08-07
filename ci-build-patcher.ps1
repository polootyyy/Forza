# CI Build Patcher - Fixes compatibility issues for MAUI Windows build
# This script patches the TEMPORARY CI copy only, not the archived source files.

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectDir
)

Write-Host "=== CI Build Patcher ==="
Write-Host "Project directory: $ProjectDir"

# 1. Fix csproj: replace invalid WebView2 package reference AND add MAUI.Controls
$projPath = Join-Path $ProjectDir "FH6Trainer.csproj"
Write-Host "Patching csproj: $projPath"
[xml]$projXml = Get-Content $projPath

# Fix WebView2 reference
$foundWebView = $false
foreach ($itemGroup in $projXml.Project.ItemGroup) {
    foreach ($pkg in $itemGroup.PackageReference) {
        if ($pkg.Include -eq "Microsoft.Web.WebView2.Core.Projection") {
            $pkg.Include = "Microsoft.Web.WebView2"
            $foundWebView = $true
            Write-Host "  Replaced Microsoft.Web.WebView2.Core.Projection -> Microsoft.Web.WebView2"
        }
    }
}

# Add MAUI.Controls package reference (required for .NET 10 MAUI)
$foundMaui = $false
foreach ($itemGroup in $projXml.Project.ItemGroup) {
    foreach ($pkg in $itemGroup.PackageReference) {
        if ($pkg.Include -eq "Microsoft.Maui.Controls") {
            $foundMaui = $true
        }
    }
}
if (-not $foundMaui) {
    $mauiRef = $projXml.CreateElement("PackageReference")
    $mauiRef.SetAttribute("Include", "Microsoft.Maui.Controls")
    $mauiRef.SetAttribute("Version", "10.0.20")
    $projXml.Project.ItemGroup[0].AppendChild($mauiRef) | Out-Null
    Write-Host "  Added Microsoft.Maui.Controls package reference"
}

$projXml.Save($projPath)

# 2. Fix HotkeyService.cs: replace WPF key types with raw virtual key codes
$hotkeyPath = Join-Path $ProjectDir "Services" "HotkeyService.cs"
Write-Host "Patching HotkeyService: $hotkeyPath"
$content = Get-Content $hotkeyPath -Raw

$content = $content.Replace(
    "public int Register(string name, System.Windows.Input.Key key, ModifierKeys mods, Action action)",
    "public int Register(string name, uint key, ModifierKeys mods, Action action)"
)
$content = $content.Replace(
    "uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);",
    "uint vk = key;"
)

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

# 3. Fix SignatureService.cs: replace Reloaded.Memory.Sigscan API with manual scanner
$sigPath = Join-Path $ProjectDir "Services" "SignatureService.cs"
Write-Host "Patching SignatureService: $sigPath"
$sigContent = Get-Content $sigPath -Raw

# Remove Reloaded.Memory.Sigscan usings
$sigContent = $sigContent.Replace("using Reloaded.Memory.Sigscan;`r`n", "")
$sigContent = $sigContent.Replace("using Reloaded.Memory.Sigscan.Definitions;`r`n", "")

# Replace the scanner usage block
$oldBlock = @'
            var scanner = new Scanner(patternBytes, mask);
            var result = scanner.FindPattern(moduleBytes);

            if (result != -1)
            {
                nint address = moduleBase + result + additionalOffset;
'@

$newBlock = @'
            long foundOffset = FindPattern(moduleBytes, patternBytes, mask);

            if (foundOffset != -1)
            {
                nint address = moduleBase + (int)foundOffset + additionalOffset;
'@

$sigContent = $sigContent.Replace($oldBlock, $newBlock)

# Add manual FindPattern helper method before the last closing brace
$helperMethod = @'

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

# Insert before the final closing brace of the class
$lastBrace = $sigContent.LastIndexOf("}")
if ($lastBrace -gt 0) {
    $sigContent = $sigContent.Insert($lastBrace, $helperMethod)
}

Set-Content -Path $sigPath -Value $sigContent -NoNewline
Write-Host "  SignatureService patched successfully"

Write-Host "=== CI Build Patcher Complete ==="
