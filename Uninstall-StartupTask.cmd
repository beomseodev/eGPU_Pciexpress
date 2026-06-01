@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-StartupTask.ps1"

endlocal
