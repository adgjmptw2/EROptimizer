# EROptimizerNative

이터널 리턴(Steam `1049590`)용 — Windows·게임 쪽 세팅 한번에 만지는 콘솔 도구.

돌리기 전에 `Y` 받고, 바꾸기 전 값은 `EROptimizer_Backup\<날짜시간>`에 남김. 대상은 DNS, 게임바 레지, GPU 선호도, 전원, `%TEMP%` 맨 윗층 파일, (조건부) NV 프로필 JSON, `boot.config` 병합.

## 게임 경로

부팅할 때마다 HKCU Steam 경로 → `libraryfolders.vdf`로 라이브러리 모으고 → 각 `steamapps`에서 `appmanifest_1049590.acf`로 설치 폴더 잡음. exe는 `EternalReturn.exe` 우선.

없으면 같은 폴더의 다른 exe 중에서, 크래시 핸들러·vc_redist 빼고 고름.

자동 실패 시 `[6]`에서 경로 붙여 넣거나 파일 창으로 지정. `[5]`는 설정 안 건드리고 탐색만 다시.

## 메뉴

1. 기본 패키지 전부  
2. `boot.config`만 — 새 백업 세션 + `summary.json`  
3. 예전 세션 골라서 레지·전원·`boot.config` 복원  
4. `boot.config` bak만 골라서 게임 쪽으로 복사  
5. 경로 스캔만 다시  
6. exe 수동 지정  
Q. 종료

## 기본 패키지

**DNS** — `ipconfig /displaydns`를 `logs\dns_before_<세션>.txt`에 남긴 다음 flush. 출력이 커서 cmd 리다이렉트로만 처리.

**게임 바 / DVR / 모드** — HKCU·일부 HKLM DWORD로 끔. 이전 값 → `registry_backup.json`. ※ 없는 키는 빌드마다 다를 수 있어서, 일부는 optional로 실패해도 넘어감.

**GPU** — `HKCU\...\UserGpuPreferences`에 게임 exe로 `GpuPreference=2;`. 예전 값은 백업에 기록.

**전원** — 지금 쓰는 계획 GUID를 `power_plan_backup.txt`에 적어 둠 → 표준 고성능이 목록에 있으면 그걸로 전환. **없으면 건너뜀**.

**TEMP** — `%TEMP%` **한 겹 아래 파일만** 삭제. 같은 층의 **디렉터리는 삭제 안 함**. `C:\Windows\Temp`는 범위 밖. 잠긴 건 스킵.

**NVIDIA JSON** — WMI에 지포스가 보일 때만. 내장 `er_profile_backup.json`을 잘라서 `files\er_profile_safe_export.json`으로 저장 (동기화·VRR·G-SYNC·주사율·FRTC·DLSS 강제 등은 빼는 쪽). **레지/NvAPI에 직접 안 씀** — 쓰려면 NVIDIA 앱이나 Profile Inspector에서 import.

**boot.config** — `EternalReturn_Data\boot.config` 백업 후 옵션 블록 병합. `build-guid`류는 유지. `job-worker-count` = `max(1, 논리 CPU − 1)`.

끝나면 콘솔에 단계별 결과 한 줄씩, 세션 폴더에 `summary.json`. 자세한 건 `logs\optimizer_<세션>.log`. INFO는 기본 파일만, WARN/ERROR는 콘솔에도.

## 복원

`[3]` — `registry_backup.json`으로 레지 되돌림 + `power_plan_backup.txt`의 GUID로 `powercfg /setactive`.

`boot.config` — 그 세션 `files\boot.config.bak_*`를 **경로 문자열 오름차순 맨 앞** 걸 씀 (파일명에 타임스탬프 붙어 있으면 대개 가장 이른 쪽). `[4]`는 같은 목록을 번호로 보여 주고 골라 복원.

## 빌드·실행

Windows x64, .NET Framework 4.8, 관리자 실행 권장.

4.8 없으면: https://dotnet.microsoft.com/download/dotnet-framework/net48

```powershell
dotnet build EROptimizerNative.sln -c Release
```

출력: `EROptimizer.Cli\bin\Release\net48\`

**Release 배포물:** `EROptimizer.exe` + `EROptimizer.exe.config` 두 개면 됨 (pdb는 선택). JSON은 **Newtonsoft.Json**만 쓰고, `EROptimizer.Core`·`Newtonsoft`·`System.CodeDom`은 **ILRepack**으로 exe 하나로 합침 (Costura 아님).

`dotnet build ... -c Debug` 는 repack 안 함 → `Core.dll` 등이 그대로 옆에 생김. 배포용은 **반드시 Release**.

아이콘은 저장소에 없음. 로컬에서 `tools/build_app_icon.py`로 `EROptimizer.Cli/app.ico` 만든 뒤, 원하면 csproj에 `<ApplicationIcon>app.ico</ApplicationIcon>` 한 줄 다시 넣으면 됨.

## 주의

레지·전원·TEMP까지 건드림. **백업 폴더(`EROptimizer_Backup`)는 지우지 마세요.** 나중에 되돌릴 때 필요함.

게임 정기점검으로 `boot.config` 내용 바뀌면 병합 결과도 달라질 수 있음.