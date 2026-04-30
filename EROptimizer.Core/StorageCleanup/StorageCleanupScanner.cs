namespace EROptimizer.Core.StorageCleanup;

public static class StorageCleanupIds
{
    public const string UserTemp = "user_temp";
    public const string WindowsTemp = "windows_temp";
    public const string NvidiaInstallerCache = "nvidia_installer_cache";
    public const string AmdInstallerCache = "amd_installer_cache";
    public const string WerAndCrashDumps = "wer_crash_dumps";
    public const string DeliveryOptimization = "delivery_optimization";
    public const string DirectXShaderCache = "directx_shader_cache";
    public const string WindowsOldInfo = "windows_old_info";
}

public static class StorageCleanupScanner
{
    private const long LowSpaceBytes = 5L * 1024 * 1024 * 1024;

    public static StorageAnalysisReport ScanStorageCleanupTargets(CleanupProtectionContext ctx)
    {
        var userTemp = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.UserTemp);
        var winTemp = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.WindowsTemp);
        var werQ = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.WerReportQueue);
        var werA = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.WerReportArchive);
        var crash = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.LocalCrashDumps);
        var doCache = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.DeliveryOptimizationCache);
        var d3d = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.DirectXD3DCache);
        var nvidia = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.NvidiaRootCache);
        var amd = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.AmdRootCache);

        var driveRows = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d =>
            {
                try
                {
                    return new DriveSummaryRow
                    {
                        Name = d.Name,
                        TotalBytes = d.TotalSize,
                        FreeBytes = d.AvailableFreeSpace
                    };
                }
                catch
                {
                    return new DriveSummaryRow { Name = d.Name, TotalBytes = 0, FreeBytes = 0 };
                }
            })
            .ToList();

        var driveC = driveRows.FirstOrDefault(x => x.Name.StartsWith("C:", StringComparison.OrdinalIgnoreCase));
        var lowC = driveC != null && driveC.FreeBytes < LowSpaceBytes;

        var windowsOld = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") ?? "C:", "Windows.old");
        windowsOld = CleanupPathSafety.NormalizePath(windowsOld);
        var winOldExists = Directory.Exists(windowsOld);
        var winOldSize = winOldExists ? StorageCleanupFileOps.GetDirectorySizeSafe(windowsOld) : 0;

        long recycleSize = 0;
        var recycleOk = RecycleBinInterop.TryQueryRecycleBinForDrive("C:\\", out recycleSize);

        var nvidiaExists = Directory.Exists(nvidia);
        var nvidiaReparse = nvidiaExists && CleanupPathSafety.IsReparsePoint(nvidia);
        var nvidiaSize = nvidiaExists && !nvidiaReparse ? StorageCleanupFileOps.GetDirectorySizeSafe(nvidia) : 0;

        var amdExists = Directory.Exists(amd);
        var amdReparse = amdExists && CleanupPathSafety.IsReparsePoint(amd);
        var amdSize = amdExists && !amdReparse ? StorageCleanupFileOps.GetDirectorySizeSafe(amd) : 0;

        return new StorageAnalysisReport
        {
            Drives = driveRows,
            DriveCLowSpace = lowC,
            DriveCLowSpaceNote = lowC ? $"C: 여유 공간이 약 {ByteSizeFormat.Format(driveC!.FreeBytes)} 입니다. ({ByteSizeFormat.Format(LowSpaceBytes)} 미만이면 부족으로 표시)" : null,
            UserTempBytes = StorageCleanupFileOps.GetDirectorySizeSafe(userTemp),
            UserTempPath = userTemp,
            WindowsTempBytes = StorageCleanupFileOps.GetDirectorySizeSafe(winTemp),
            WindowsTempPath = winTemp,
            RecycleBinEstimatedBytes = recycleOk ? recycleSize : 0,
            RecycleBinKnown = recycleOk,
            WindowsOldExists = winOldExists,
            WindowsOldBytes = winOldSize,
            WindowsOldPath = windowsOld,
            NvidiaRootExists = nvidiaExists,
            NvidiaRootIsReparse = nvidiaReparse,
            NvidiaRootBytes = nvidiaSize,
            AmdRootExists = amdExists,
            AmdRootIsReparse = amdReparse,
            AmdRootBytes = amdSize,
            DeliveryOptimizationCacheBytes = StorageCleanupFileOps.GetDirectorySizeSafe(doCache),
            DeliveryOptimizationCachePath = doCache,
            DirectXD3DCacheBytes = StorageCleanupFileOps.GetDirectorySizeSafe(d3d),
            DirectXD3DCachePath = d3d,
            WerQueuedBytes = StorageCleanupFileOps.GetDirectorySizeSafe(werQ),
            WerArchiveBytes = StorageCleanupFileOps.GetDirectorySizeSafe(werA),
            LocalCrashDumpBytes = StorageCleanupFileOps.GetDirectorySizeSafe(crash),
            WerReportQueuePath = werQ,
            WerReportArchivePath = werA,
            LocalCrashDumpPath = crash
        };
    }

    public static List<CleanupPreviewRow> BuildCleanupPreview(StorageAnalysisReport a)
    {
        var list = new List<CleanupPreviewRow>
        {
            new()
            {
                Id = StorageCleanupIds.UserTemp,
                Title = "사용자 TEMP (%TEMP%)",
                Risk = CleanupRiskLabel.Recommended,
                PathOrDescription = a.UserTempPath,
                EstimatedBytes = a.UserTempBytes,
                SelectedByDefault = true,
                RecoverabilityNote = "삭제 후 복원은 어렵습니다. 다만 대부분은 임시 파일입니다.",
                CautionNote = "사용 중인 파일은 건너뜁니다."
            },
            new()
            {
                Id = StorageCleanupIds.WindowsTemp,
                Title = "Windows TEMP (C:\\Windows\\Temp)",
                Risk = CleanupRiskLabel.Caution,
                PathOrDescription = a.WindowsTempPath,
                EstimatedBytes = a.WindowsTempBytes,
                SelectedByDefault = false,
                RecoverabilityNote = "삭제형 정리는 되돌리기 어렵습니다.",
                CautionNote = "관리자 권한이 필요할 수 있습니다. 설치·업데이트 중이면 건너뛰는 것이 좋습니다."
            },
            new()
            {
                Id = StorageCleanupIds.NvidiaInstallerCache,
                Title = "NVIDIA 설치 압축 해제 캐시 후보 (C:\\NVIDIA)",
                Risk = CleanupRiskLabel.Recommended,
                PathOrDescription = a.NvidiaRootExists
                    ? $"C:\\NVIDIA — 현재 설치된 드라이버가 아니라 설치 압축 해제 캐시 후보입니다. {(a.NvidiaRootIsReparse ? "(재분석 지점이라 정리 불가)" : "")}"
                    : "C:\\NVIDIA 없음",
                EstimatedBytes = a.NvidiaRootBytes,
                SelectedByDefault = a.NvidiaRootExists && !a.NvidiaRootIsReparse && a.NvidiaRootBytes > 0,
                RecoverabilityNote = "삭제 후에는 설치 파일을 다시 받아야 할 수 있습니다. 설치된 드라이버 자체는 건드리지 않습니다.",
                CautionNote = "루트 C:\\NVIDIA 만 대상입니다. Program Files 는 포함하지 않습니다.",
                CanExecute = a.NvidiaRootExists && !a.NvidiaRootIsReparse
            },
            new()
            {
                Id = StorageCleanupIds.AmdInstallerCache,
                Title = "AMD 설치 압축 해제 캐시 후보 (C:\\AMD)",
                Risk = CleanupRiskLabel.Recommended,
                PathOrDescription = a.AmdRootExists
                    ? $"C:\\AMD — 현재 설치된 드라이버가 아니라 설치 압축 해제 캐시 후보입니다. {(a.AmdRootIsReparse ? "(재분석 지점이라 정리 불가)" : "")}"
                    : "C:\\AMD 없음",
                EstimatedBytes = a.AmdRootBytes,
                SelectedByDefault = a.AmdRootExists && !a.AmdRootIsReparse && a.AmdRootBytes > 0,
                RecoverabilityNote = "삭제 후에는 설치 파일을 다시 받아야 할 수 있습니다. 설치된 드라이버 자체는 건드리지 않습니다.",
                CautionNote = "루트 C:\\AMD 만 대상입니다.",
                CanExecute = a.AmdRootExists && !a.AmdRootIsReparse
            },
            new()
            {
                Id = StorageCleanupIds.WerAndCrashDumps,
                Title = "Windows 오류 보고 대기/보관 + 로컬 크래시 덤프",
                Risk = CleanupRiskLabel.Recommended,
                PathOrDescription = $"{a.WerReportQueuePath} ; {a.WerReportArchivePath} ; {a.LocalCrashDumpPath}",
                EstimatedBytes = a.WerQueuedBytes + a.WerArchiveBytes + a.LocalCrashDumpBytes,
                SelectedByDefault = true,
                RecoverabilityNote = "문제 진단에 쓰이던 복사본이 사라집니다. 완전 복구는 어렵습니다.",
                CautionNote = "일반적인 보고·덤프 위치만 포함합니다."
            },
            new()
            {
                Id = StorageCleanupIds.DeliveryOptimization,
                Title = "배달 최적화 캐시 (Delivery Optimization)",
                Risk = CleanupRiskLabel.Caution,
                PathOrDescription =
                    $"{a.DeliveryOptimizationCachePath} — 불확실하면 Windows 설정 → 시스템 → 저장소 → 임시 파일 에서 정리하세요.",
                EstimatedBytes = a.DeliveryOptimizationCacheBytes,
                SelectedByDefault = false,
                RecoverabilityNote = "삭제 후 필요하면 Windows가 다시 받습니다.",
                CautionNote =
                    "확인된 캐시 폴더만 직접 정리합니다. 애매하면 설정 화면의 정리를 권장합니다."
            },
            new()
            {
                Id = StorageCleanupIds.DirectXShaderCache,
                Title = "DirectX Shader Cache (D3DSCache)",
                Risk = CleanupRiskLabel.Caution,
                PathOrDescription = a.DirectXD3DCachePath,
                EstimatedBytes = a.DirectXD3DCacheBytes,
                SelectedByDefault = false,
                RecoverabilityNote = "삭제 후 필요하면 Windows·게임이 다시 생성합니다.",
                CautionNote =
                    "첫 실행 때 셰이더 캐시를 다시 만들면서 잠시 끊길 수 있습니다. 그래픽 문제 해결용으로 쓰일 때가 많습니다."
            },
            new()
            {
                Id = StorageCleanupIds.WindowsOldInfo,
                Title = "Windows 업데이트 잔여 데이터 · 덤프 (Windows.old)",
                Risk = CleanupRiskLabel.Caution,
                PathOrDescription = a.WindowsOldExists
                    ? $"{a.WindowsOldPath} — 대형 업데이트 후 남는 이전 설치·덤프에 가까운 사본입니다. 이 도구에서는 삭제하지 않습니다."
                    : "업데이트 잔여 폴더 없음 (Windows.old)",
                EstimatedBytes = a.WindowsOldBytes,
                SelectedByDefault = false,
                RecoverabilityNote = "삭제는 이 버전에서 제공하지 않습니다.",
                CautionNote =
                    "삭제 시 이전 Windows 버전으로 되돌리기 어려워질 수 있습니다. 설정 → 시스템 → 저장소 의 이전 Windows 설치 항목을 확인하세요.",
                CanExecute = false
            }
        };

        return list;
    }
}
