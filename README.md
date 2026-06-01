# eGPU PCI Express Manager

eGPU 연결 시 `WHEA-Logger` 오류와 시스템 다운이 반복되는 환경에서,
Windows 부팅 설정의 `pciexpress` 값을 버튼으로 전환하기 위한 간단한 도구입니다.

## GUI version

새 GUI version은 C# Windows Forms 기반의 트레이 앱입니다.
PowerShell 창이나 콘솔 창 없이 실행되며, 관리자 권한 UAC 승인 후 현재 상태를 자동으로 반대로 전환합니다.

### 실행 방법

이미 빌드된 실행 파일이 있는 경우 아래 파일을 더블클릭합니다.

```text
dist\eGPU-TrayToggle-signal\EGPU.TrayToggle.exe
```

배포용 zip을 받은 경우:

1. `eGPU-TrayToggle-signal-win-x64.zip` 압축을 풉니다.
2. 압축을 푼 폴더의 `EGPU.TrayToggle.exe`를 더블클릭합니다.
3. Windows UAC 창이 표시되면 관리자 권한 실행을 허용합니다.
4. 앱이 현재 `pciexpress` 상태를 읽고 자동으로 반대 상태로 전환합니다.
5. 전환 후 시스템 트레이 아이콘에서 현재 ON/OFF 상태를 확인합니다.

저장소에서 실행하는 경우 아래 런처를 사용할 수도 있습니다.

```powershell
.\Run-eGPU-TrayToggle.cmd
```

주의: `EGPU.TrayToggle.exe`는 실행 즉시 ON/OFF를 토글합니다. 단순히 열어보는 것만으로도 `bcdedit` 설정이 바뀔 수 있습니다.

### 동작 방식

- 현재 상태가 ON이면 실행 시 OFF로 복원합니다.
- 현재 상태가 OFF이면 실행 시 ON으로 적용합니다.
- 실행 후 시스템 트레이에 상주하며 아이콘으로 상태를 표시합니다.
- 초록불 아이콘: `pciexpress ForceDisable`
- 빨간불 아이콘: 기본 PCI Express 설정
- 트레이 아이콘 더블클릭 또는 메뉴의 `Toggle`로 다시 전환할 수 있습니다.
- 트레이 메뉴는 `Toggle`, `Refresh`, `Exit`을 제공합니다.
- 이미 실행 중인 상태에서 다시 실행하면 새 앱을 띄우지 않고 기존 트레이 앱에 토글 명령을 보냅니다.

### 빌드

현재 저장소에는 소스 코드만 포함되어 있습니다.
빌드하려면 .NET 8 SDK가 필요합니다.

```powershell
dotnet publish .\EGPU.TrayToggle\EGPU.TrayToggle.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\dist\eGPU-TrayToggle-signal
```

빌드 후 실행:

```powershell
.\Run-eGPU-TrayToggle.cmd
```

또는 빌드 산출물 실행:

```powershell
.\dist\eGPU-TrayToggle-signal\EGPU.TrayToggle.exe
```

## PowerShell ver1

초기 버전은 PowerShell WinForms 앱입니다.
추가 SDK 없이 실행해야 하는 경우 아래 파일을 사용할 수 있습니다.

```powershell
.\Run-eGPU-Manager.cmd
```

## 기능

- ON: `bcdedit /set pciexpress ForceDisable`
- OFF: `bcdedit /set pciexpress Default`
- 현재 `bcdedit` 상태 조회 후 ON/OFF 표시
- 관리자 권한이 아니면 UAC 승격으로 다시 실행
- 설정 변경 후 재부팅 필요 가능성 안내

## 사용 방법

1. `Run-eGPU-Manager.cmd`를 실행합니다.
2. Windows UAC 창이 표시되면 관리자 권한 실행을 허용합니다.
3. eGPU를 사용할 때는 `ON - eGPU 안정화 적용`을 누릅니다.
4. eGPU를 제거하거나 기본 설정으로 되돌릴 때는 `OFF - 기본값으로 복원`을 누릅니다.
5. 설정 변경 후 문제가 계속되거나 반영되지 않은 것처럼 보이면 Windows를 재부팅합니다.

## 주의사항

- 이 도구는 Windows의 부팅 설정을 변경하므로 관리자 권한이 필요합니다.
- `ForceDisable`은 PCI Express 관련 전원 관리 기능을 비활성화하는 설정입니다.
- PC 환경에 따라 변경 사항은 재부팅 후 적용될 수 있습니다.
- 이 도구는 eGPU 문제 완화를 돕는 보조 도구이며, 모든 WHEA-Logger 오류를 해결한다고 보장하지 않습니다.

## 개발 메모

GUI version은 C# Windows Forms로 작성했습니다.
PowerShell ver1은 추가 SDK 설치 없이 실행할 수 있도록 PowerShell WinForms로 유지합니다.
커밋 시에는 커밋 컨벤션을 따릅니다. 예: `feat: add eGPU PCI Express toggle app`

