Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Start-ElevatedSelf {
    $scriptPath = $PSCommandPath
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        [System.Windows.Forms.MessageBox]::Show(
            '스크립트 경로를 확인할 수 없습니다. 파일로 저장된 상태에서 다시 실행해주세요.',
            'eGPU PCI Express Manager',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
        exit 1
    }

    $arguments = @(
        '-NoProfile'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        "`"$scriptPath`""
    ) -join ' '

    Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -Verb RunAs | Out-Null
    exit 0
}

function Invoke-Bcdedit {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "$env:SystemRoot\System32\bcdedit.exe"
    $psi.Arguments = $Arguments -join ' '
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    [void] $process.Start()
    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    [PSCustomObject]@{
        ExitCode = $process.ExitCode
        Output = $standardOutput.Trim()
        Error = $standardError.Trim()
    }
}

function Get-PciexpressMode {
    $result = Invoke-Bcdedit -Arguments @('/enum')
    if ($result.ExitCode -ne 0) {
        $message = if ($result.Error) { $result.Error } else { $result.Output }
        throw "bcdedit 상태 조회 실패: $message"
    }

    $mode = $null
    foreach ($line in ($result.Output -split "\r?\n")) {
        if ($line -match '^\s*pciexpress\s+(.+?)\s*$') {
            $mode = $Matches[1].Trim()
            break
        }
    }

    if ([string]::IsNullOrWhiteSpace($mode)) {
        return 'Default'
    }

    return $mode
}

function Set-PciexpressMode {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('ForceDisable', 'Default')]
        [string] $Mode
    )

    $result = Invoke-Bcdedit -Arguments @('/set', 'pciexpress', $Mode)
    if ($result.ExitCode -ne 0) {
        $message = if ($result.Error) { $result.Error } else { $result.Output }
        throw "bcdedit 설정 실패: $message"
    }
}

if (-not (Test-IsAdministrator)) {
    Start-ElevatedSelf
}

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)

$font = New-Object System.Drawing.Font('Segoe UI', 10)
$titleFont = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
$statusFont = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)

$form = New-Object System.Windows.Forms.Form
$form.Text = 'eGPU PCI Express Manager'
$form.StartPosition = 'CenterScreen'
$form.ClientSize = New-Object System.Drawing.Size(540, 370)
$form.MinimumSize = New-Object System.Drawing.Size(520, 360)
$form.Font = $font
$form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
$form.MaximizeBox = $false

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = 'eGPU PCI Express Manager'
$titleLabel.Font = $titleFont
$titleLabel.AutoSize = $true
$titleLabel.Location = New-Object System.Drawing.Point(24, 22)

$descriptionLabel = New-Object System.Windows.Forms.Label
$descriptionLabel.Text = 'WHEA-Logger 오류 완화를 위해 PCI Express 전원 관리 설정을 전환합니다.'
$descriptionLabel.AutoSize = $false
$descriptionLabel.Size = New-Object System.Drawing.Size(500, 42)
$descriptionLabel.Location = New-Object System.Drawing.Point(26, 62)

$statusCaptionLabel = New-Object System.Windows.Forms.Label
$statusCaptionLabel.Text = '현재 상태'
$statusCaptionLabel.AutoSize = $true
$statusCaptionLabel.Location = New-Object System.Drawing.Point(26, 118)

$statusValueLabel = New-Object System.Windows.Forms.Label
$statusValueLabel.Text = '확인 중...'
$statusValueLabel.Font = $statusFont
$statusValueLabel.AutoSize = $false
$statusValueLabel.Size = New-Object System.Drawing.Size(500, 32)
$statusValueLabel.Location = New-Object System.Drawing.Point(26, 144)

$onButton = New-Object System.Windows.Forms.Button
$onButton.Text = 'ON - eGPU 안정화 적용'
$onButton.Size = New-Object System.Drawing.Size(230, 44)
$onButton.Location = New-Object System.Drawing.Point(26, 195)

$offButton = New-Object System.Windows.Forms.Button
$offButton.Text = 'OFF - 기본값으로 복원'
$offButton.Size = New-Object System.Drawing.Size(230, 44)
$offButton.Location = New-Object System.Drawing.Point(278, 195)

$refreshButton = New-Object System.Windows.Forms.Button
$refreshButton.Text = '상태 새로고침'
$refreshButton.Size = New-Object System.Drawing.Size(150, 34)
$refreshButton.Location = New-Object System.Drawing.Point(26, 258)

$messageBox = New-Object System.Windows.Forms.TextBox
$messageBox.Multiline = $true
$messageBox.ReadOnly = $true
$messageBox.ScrollBars = [System.Windows.Forms.ScrollBars]::Vertical
$messageBox.Size = New-Object System.Drawing.Size(482, 54)
$messageBox.Location = New-Object System.Drawing.Point(26, 305)

$script:currentMode = $null

function Set-UiBusy {
    param([bool] $Busy)

    $onButton.Enabled = -not $Busy
    $offButton.Enabled = -not $Busy
    $refreshButton.Enabled = -not $Busy
    $form.Cursor = if ($Busy) {
        [System.Windows.Forms.Cursors]::WaitCursor
    } else {
        [System.Windows.Forms.Cursors]::Default
    }
}

function Update-StatusDisplay {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Mode,
        [string] $Message = ''
    )

    $script:currentMode = $Mode
    if ($Mode -ieq 'ForceDisable') {
        $statusValueLabel.Text = 'ON - eGPU 안정화 적용됨 (pciexpress ForceDisable)'
        $statusValueLabel.ForeColor = [System.Drawing.Color]::FromArgb(0, 115, 55)
    } else {
        $statusValueLabel.Text = "OFF - 기본값 사용 중 (pciexpress $Mode)"
        $statusValueLabel.ForeColor = [System.Drawing.Color]::FromArgb(35, 35, 35)
    }

    if ($Message) {
        $messageBox.Text = $Message
    }
}

function Refresh-PciexpressStatus {
    Set-UiBusy $true
    try {
        $mode = Get-PciexpressMode
        Update-StatusDisplay -Mode $mode -Message '상태를 확인했습니다.'
    } catch {
        $statusValueLabel.Text = '상태 확인 실패'
        $statusValueLabel.ForeColor = [System.Drawing.Color]::FromArgb(170, 20, 20)
        $messageBox.Text = $_.Exception.Message
    } finally {
        Set-UiBusy $false
    }
}

function Apply-PciexpressMode {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('ForceDisable', 'Default')]
        [string] $Mode
    )

    Set-UiBusy $true
    try {
        Set-PciexpressMode -Mode $Mode
        $modeAfterChange = Get-PciexpressMode
        $successMessage = '설정을 변경했습니다. 변경 사항은 재부팅 후 적용될 수 있습니다.'
        Update-StatusDisplay -Mode $modeAfterChange -Message $successMessage
        [System.Windows.Forms.MessageBox]::Show(
            $successMessage,
            'eGPU PCI Express Manager',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information
        ) | Out-Null
    } catch {
        $messageBox.Text = $_.Exception.Message
        [System.Windows.Forms.MessageBox]::Show(
            $_.Exception.Message,
            'eGPU PCI Express Manager',
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error
        ) | Out-Null
    } finally {
        Set-UiBusy $false
    }
}

$onButton.Add_Click({
    Apply-PciexpressMode -Mode 'ForceDisable'
})

$offButton.Add_Click({
    Apply-PciexpressMode -Mode 'Default'
})

$refreshButton.Add_Click({
    Refresh-PciexpressStatus
})

$form.Add_Shown({
    Refresh-PciexpressStatus
})

[void] $form.Controls.Add($titleLabel)
[void] $form.Controls.Add($descriptionLabel)
[void] $form.Controls.Add($statusCaptionLabel)
[void] $form.Controls.Add($statusValueLabel)
[void] $form.Controls.Add($onButton)
[void] $form.Controls.Add($offButton)
[void] $form.Controls.Add($refreshButton)
[void] $form.Controls.Add($messageBox)

[System.Windows.Forms.Application]::Run($form)
