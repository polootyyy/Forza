@echo off
echo ============================================
echo   Polootyyy FH6 Trainer - Build Script
echo ============================================
echo.

REM Check for .NET SDK
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET SDK not found! Install .NET 10 SDK from https://dotnet.microsoft.com
    pause
    exit /b 1
)

echo [1/3] Installing MAUI workload...
dotnet workload install maui-windows

echo.
echo [2/3] Restoring NuGet packages...
dotnet restore FH6Trainer.csproj

echo.
echo [3/3] Building Release (x64)...
dotnet publish FH6Trainer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo   BUILD SUCCESSFUL!
    echo   Output: .\publish\Polootyyy.exe
    echo ============================================
) else (
    echo.
    echo [ERROR] Build failed. Check errors above.
)

pause
