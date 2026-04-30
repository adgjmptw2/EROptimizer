namespace EROptimizer.Core.StorageCleanup;

public static class StorageCleanupExecutor
{
    public static CleanupExecutionReport ExecuteCleanupPlan(
        IReadOnlyCollection<string> selectedIds,
        CleanupProtectionContext ctx,
        ErLog log,
        IProgress<int>? progressPercent = null)
    {
        var started = DateTime.UtcNow;
        var report = new CleanupExecutionReport { StartedUtc = started };
        var list = selectedIds as IList<string> ?? selectedIds.ToList();
        var total = list.Count;
        if (total == 0)
        {
            progressPercent?.Report(100);
            report.FinishedUtc = DateTime.UtcNow;
            return report;
        }

        progressPercent?.Report(0);
        for (var i = 0; i < total; i++)
        {
            var id = list[i];
            switch (id)
            {
                case StorageCleanupIds.UserTemp:
                    AddResult(report, id, "사용자 TEMP", CleanSingleRoot(CleanupAllowKind.UserTemp, ctx, log));
                    break;
                case StorageCleanupIds.WindowsTemp:
                    AddResult(report, id, "Windows TEMP", CleanSingleRoot(CleanupAllowKind.WindowsTemp, ctx, log));
                    break;
                case StorageCleanupIds.NvidiaInstallerCache:
                    AddResult(report, id, "NVIDIA 설치 캐시 후보 (C:\\NVIDIA)",
                        CleanGpuRoot(CleanupAllowKind.NvidiaRootCache, ctx, log));
                    break;
                case StorageCleanupIds.AmdInstallerCache:
                    AddResult(report, id, "AMD 설치 캐시 후보 (C:\\AMD)",
                        CleanGpuRoot(CleanupAllowKind.AmdRootCache, ctx, log));
                    break;
                case StorageCleanupIds.WerAndCrashDumps:
                    report.Items.Add(RunWerAndCrash(ctx, log));
                    break;
                case StorageCleanupIds.DeliveryOptimization:
                    AddResult(report, id, "배달 최적화 캐시", CleanDeliveryOptimizationCache(ctx, log));
                    break;
                case StorageCleanupIds.DirectXShaderCache:
                    AddResult(report, id, "DirectX Shader Cache", CleanSingleRoot(CleanupAllowKind.DirectXD3DCache, ctx, log));
                    break;
                case StorageCleanupIds.WindowsOldInfo:
                    log.Info("Windows 업데이트 잔여(Windows.old)는 정보 전용 항목입니다. 정리를 건너뜁니다.");
                    break;
            }

            var pct = (int)Math.Round((i + 1) * 100.0 / total);
            if (pct > 100) pct = 100;
            progressPercent?.Report(pct);
        }

        report.FinishedUtc = DateTime.UtcNow;
        foreach (var item in report.Items)
            report.TotalBytesReclaimed += item.BytesReclaimed;
        return report;
    }

    private static void AddResult(CleanupExecutionReport report, string id, string title, CleanupItemResult r)
    {
        r = r with { Id = id, Title = title };
        report.Items.Add(r);
    }

    private static CleanupItemResult CleanSingleRoot(CleanupAllowKind kind, CleanupProtectionContext ctx, ErLog log)
    {
        var allow = CleanupPathSafety.GetCanonicalAllowRoot(kind);
        return DeleteUnderAllowRoot(allow, allow, ctx, log);
    }

    private static CleanupItemResult CleanGpuRoot(CleanupAllowKind kind, CleanupProtectionContext ctx, ErLog log)
    {
        var allow = CleanupPathSafety.GetCanonicalAllowRoot(kind);
        if (!Directory.Exists(allow))
        {
            log.Info($"{kind}: 폴더 없음 — 건너뜀");
            return new CleanupItemResult { Ran = true };
        }

        if (CleanupPathSafety.IsReparsePoint(allow))
        {
            log.Warn($"{kind}: 재분석 지점(junction/symlink) — 삭제하지 않음");
            return new CleanupItemResult { Ran = false };
        }

        return DeleteUnderAllowRoot(allow, allow, ctx, log);
    }

    private static CleanupItemResult DeleteUnderAllowRoot(string allowRoot, string deleteUnder, CleanupProtectionContext ctx,
        ErLog log)
    {
        var failures = new List<CleanupFailureRecord>();
        StorageCleanupFileOps.DeleteTreeContents(allowRoot, deleteUnder, ctx, failures, out var freed, out var files,
            out var dirs, out var skipped);

        CleanupFailureLog.LogAggregatedFailures(log, failures);

        var item = new CleanupItemResult
        {
            Ran = true,
            BytesReclaimed = freed,
            FilesDeleted = files,
            DirectoriesRemoved = dirs,
            Skipped = skipped
        };
        foreach (var x in failures.Take(200))
            item.Failures.Add(x);
        return item;
    }

    private static CleanupItemResult RunWerAndCrash(CleanupProtectionContext ctx, ErLog log)
    {
        long totalBytes = 0;
        var totalFiles = 0;
        var totalDirs = 0;
        var totalSkip = 0;
        var allFails = new List<CleanupFailureRecord>();

        foreach (var kind in new[]
                 {
                     CleanupAllowKind.WerReportQueue, CleanupAllowKind.WerReportArchive, CleanupAllowKind.LocalCrashDumps
                 })
        {
            var allow = CleanupPathSafety.GetCanonicalAllowRoot(kind);
            if (!Directory.Exists(allow))
                continue;
            StorageCleanupFileOps.DeleteTreeContents(allow, allow, ctx, allFails, out var b, out var f, out var d,
                out var s);
            totalBytes += b;
            totalFiles += f;
            totalDirs += d;
            totalSkip += s;
        }

        CleanupFailureLog.LogAggregatedFailures(log, allFails);

        var item = new CleanupItemResult
        {
            Id = StorageCleanupIds.WerAndCrashDumps,
            Title = "Windows 오류 보고 + 로컬 크래시 덤프",
            Ran = true,
            BytesReclaimed = totalBytes,
            FilesDeleted = totalFiles,
            DirectoriesRemoved = totalDirs,
            Skipped = totalSkip
        };
        foreach (var x in allFails.Take(200))
            item.Failures.Add(x);
        return item;
    }

    private static CleanupItemResult CleanDeliveryOptimizationCache(CleanupProtectionContext ctx, ErLog log)
    {
        var allow = CleanupPathSafety.GetCanonicalAllowRoot(CleanupAllowKind.DeliveryOptimizationCache);
        if (!Directory.Exists(allow))
        {
            log.Info("Delivery Optimization 캐시 폴더가 없습니다.");
            return new CleanupItemResult { Ran = true };
        }

        if (CleanupPathSafety.IsReparsePoint(allow))
        {
            log.Warn("Delivery Optimization 캐시 경로가 재분석 지점 — 건너뜀");
            return new CleanupItemResult { Ran = false };
        }

        return DeleteUnderAllowRoot(allow, allow, ctx, log);
    }
}
