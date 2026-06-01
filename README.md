# eGPU PCI Express Tray Toggle

eGPU 사용 중 `WHEA-Logger` 오류와 시스템 다운이 반복되는 환경에서,
Windows 부팅 설정의 `pciexpress` 값을 트레이 아이콘 앱으로 전환하는 도구입니다.

## 간단 사용법

개발을 몰라도 아래 순서대로 사용하면 됩니다.

1. `Run-eGPU-TrayToggle.cmd` 파일을 더블클릭합니다.
2. Windows에서 관리자 권한을 물어보면 `예`를 누릅니다.
3. 작업 표시줄 오른쪽 아래에 트레이 아이콘이 뜨면 실행된 상태입니다.
4. 설정을 다시 바꾸고 싶으면 트레이 아이콘을 더블클릭하거나, 우클릭 후 `Turn ON` 또는 `Turn OFF`를 누릅니다.
5. PC를 켤 때마다 자동으로 실행되게 하려면 트레이 아이콘을 우클릭한 뒤 `Start with Windows`를 켭니다.

트레이 아이콘 색상은 현재 상태를 뜻합니다.

- 초록불: eGPU 안정화 설정 ON
- 빨간불: eGPU 안정화 설정 OFF

주의: `Run-eGPU-TrayToggle.cmd`를 다시 실행하면 설정이 한 번 더 바뀝니다.

## 자세한 사용법

설정을 바꾸고 싶을 때 아래 파일을 실행합니다.

```text
Run-eGPU-TrayToggle.cmd
```

Windows UAC 관리자 권한 창이 뜨면 승인합니다. 앱이 현재 `pciexpress` 상태를 읽고 한 번 전환한 뒤 트레이 아이콘으로 남습니다.

- 현재 ON이면 OFF로 전환: `bcdedit /set pciexpress Default`
- 현재 OFF이면 ON으로 전환: `bcdedit /set pciexpress ForceDisable`

주의: `Run-eGPU-TrayToggle.cmd` 또는 exe를 다시 실행하면 ON/OFF가 한 번 더 전환됩니다.

## 트레이 아이콘 사용

상태는 Windows 작업 표시줄의 트레이 아이콘으로 확인합니다.

- 초록불: ON, `pciexpress ForceDisable`
- 빨간불: OFF, 기본 PCI Express 설정

트레이 아이콘을 우클릭하면 다음 메뉴를 사용할 수 있습니다.

- `Turn ON` 또는 `Turn OFF`: 현재 상태를 반대로 전환
- `Refresh`: 현재 `bcdedit` 상태 다시 확인
- `Start with Windows`: Windows 로그인 시 자동 실행 켜기/끄기
- `Exit`: 트레이 앱 종료

이미 앱이 실행 중인 상태에서 `Run-eGPU-TrayToggle.cmd` 또는 exe를 다시 실행하면 새 앱을 띄우지 않고, 기존 트레이 앱에 토글 명령만 전달합니다.

## 자동 실행

컴퓨터 로그인 시 트레이 앱을 자동으로 띄우려면 트레이 아이콘을 우클릭한 뒤 `Start with Windows`를 켭니다.

자동 실행은 `--no-toggle` 모드로 등록됩니다. 즉, 부팅하거나 로그인할 때는 ON/OFF를 뒤집지 않고 트레이 아이콘만 띄워 현재 상태를 표시합니다.

자동 실행을 해제하려면 트레이 아이콘을 우클릭한 뒤 `Start with Windows`를 다시 끕니다.

## 수동 실행과 관리

exe를 직접 실행할 수도 있습니다.

```text
dist\eGPU-TrayToggle\EGPU.TrayToggle.exe
```

트레이 메뉴를 사용할 수 없는 상황에서는 아래 파일로 자동 실행을 등록하거나 해제할 수 있습니다.

```text
Install-StartupTask.cmd
```

```text
Uninstall-StartupTask.cmd
```

## 빌드

소스에서 다시 빌드하려면 .NET 8 SDK가 필요합니다.

```powershell
dotnet publish .\EGPU.TrayToggle\EGPU.TrayToggle.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\dist\eGPU-TrayToggle
```

## 주의사항

- 이 도구는 Windows 부팅 설정을 변경하므로 관리자 권한이 필요합니다.
- 변경 사항은 PC 환경에 따라 재부팅 후 적용될 수 있습니다.
- 모든 WHEA-Logger 오류를 해결한다고 보장하지는 않습니다.
