@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "APP=%SCRIPT_DIR%EGPU.TrayToggle\bin\Release\net8.0-windows\EGPU.TrayToggle.exe"

if exist "%APP%" (
    start "" "%APP%"
    exit /b 0
)

echo EGPU.TrayToggle.exe was not found.
echo Build it first with:
echo dotnet build "%SCRIPT_DIR%EGPU.TrayToggle\EGPU.TrayToggle.csproj" -c Release
exit /b 1

