# eGPU PCI Express Tray Toggle

eGPU 사용 중 `WHEA-Logger` 오류와 시스템 다운이 반복되는 환경에서,
Windows 부팅 설정의 `pciexpress` 값을 트레이 아이콘 앱으로 전환하는 도구입니다.

## 실행 파일

일반 사용자는 아래 파일만 실행하면 됩니다.

```text
dist\eGPU-TrayToggle\EGPU.TrayToggle.exe
```

실행하면 Windows UAC 관리자 권한 창이 뜹니다.
승인하면 앱이 현재 상태를 읽고 즉시 반대 상태로 전환합니다.

- 현재 ON이면 OFF로 전환: `bcdedit /set pciexpress Default`
- 현재 OFF이면 ON으로 전환: `bcdedit /set pciexpress ForceDisable`

주의: 실행 파일을 더블클릭하는 것만으로도 ON/OFF가 토글됩니다.

## 상태 확인

상태는 Windows 작업 표시줄의 트레이 아이콘으로 확인합니다.

- 초록불: ON, `pciexpress ForceDisable`
- 빨간불: OFF, 기본 PCI Express 설정

트레이 아이콘을 우클릭하면 다음 메뉴를 사용할 수 있습니다.

- `Turn ON` 또는 `Turn OFF`: 현재 상태를 반대로 전환
- `Refresh`: 현재 `bcdedit` 상태 다시 확인
- `Exit`: 트레이 앱 종료

이미 앱이 실행 중인 상태에서 exe를 다시 실행하면 새 앱을 띄우지 않고,
기존 트레이 앱에 토글 명령만 전달합니다.

## 빌드

소스에서 다시 빌드하려면 .NET 8 SDK가 필요합니다.

```powershell
dotnet publish .\EGPU.TrayToggle\EGPU.TrayToggle.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\dist\eGPU-TrayToggle
```

## 주의사항

- 이 도구는 Windows 부팅 설정을 변경하므로 관리자 권한이 필요합니다.
- 변경 사항은 PC 환경에 따라 재부팅 후 적용될 수 있습니다.
- 모든 WHEA-Logger 오류를 해결한다고 보장하지는 않습니다.
- 커밋 시에는 커밋 컨벤션을 따릅니다. 예: `feat: add tray toggle app`
