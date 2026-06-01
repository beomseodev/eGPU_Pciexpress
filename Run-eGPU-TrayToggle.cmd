@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "APP=%SCRIPT_DIR%dist\eGPU-TrayToggle\EGPU.TrayToggle.exe"

if exist "%APP%" (
    start "" "%APP%"
    exit /b 0
)

set "APP=%SCRIPT_DIR%EGPU.TrayToggle\bin\Release\net8.0-windows\win-x64\EGPU.TrayToggle.exe"

if exist "%APP%" (
    start "" "%APP%"
    exit /b 0
)

echo EGPU.TrayToggle.exe was not found.
echo Build it first with:
echo dotnet publish "%SCRIPT_DIR%EGPU.TrayToggle\EGPU.TrayToggle.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%SCRIPT_DIR%dist\eGPU-TrayToggle"
exit /b 1
