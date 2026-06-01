$ErrorActionPreference = 'Stop'

$taskName = 'eGPU Tray Toggle'
$scriptRoot = Split-Path -Parent $PSCommandPath
$exePath = Join-Path $scriptRoot 'dist\eGPU-TrayToggle\EGPU.TrayToggle.exe'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    Start-Process -FilePath 'powershell.exe' -ArgumentList @(
        '-NoProfile'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        "`"$PSCommandPath`""
    ) -Verb RunAs | Out-Null
    exit 0
}

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Executable not found: $exePath"
}

$action = New-ScheduledTaskAction -Execute $exePath -Argument '--no-toggle' -WorkingDirectory (Split-Path -Parent $exePath)
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 0)

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Start eGPU Tray Toggle without toggling state at logon.' -Force | Out-Null

Write-Host "Installed startup task: $taskName"
