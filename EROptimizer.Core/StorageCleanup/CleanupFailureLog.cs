using System.Linq;

namespace EROptimizer.Core.StorageCleanup;

public static class CleanupFailureLog
{
    public static IReadOnlyList<(string Label, int Count)> SummarizeFailures(IReadOnlyList<CleanupFailureRecord> failures)
    {
        if (failures.Count == 0)
            return Array.Empty<(string, int)>();
        return failures
            .GroupBy(f => ClassifyReason(f.Reason))
            .Select(g => (g.Key, g.Count()))
            .ToArray();
    }

    public static void LogAggregatedFailures(ErLog log, IReadOnlyList<CleanupFailureRecord> failures)
    {
        if (failures.Count == 0)
            return;

        foreach (var g in failures.GroupBy(f => ClassifyReason(f.Reason)))
            log.Warn($"삭제 실패 : {g.Key} [{g.Count()}건] - SKIP.");
    }

    private static string ClassifyReason(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
            return "알 수 없는 오류";

        var r = (reason ?? "").Replace("\r\n", " ").Trim();

        if (r.Contains("다른 프로세스에서 사용 중", StringComparison.Ordinal)
            || r.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
            return "다른 프로세스에서 사용 중이므로 프로세스에서 액세스 할 수 없습니다.";

        if (r.Contains("거부", StringComparison.Ordinal)
            || r.Contains("access denied", StringComparison.OrdinalIgnoreCase)
            || r.Contains("액세스가 거부", StringComparison.Ordinal))
            return "접근이 거부되었습니다.";

        if (r.Contains("경로의 한 부분을 찾을 수 없습니다", StringComparison.Ordinal)
            || r.Contains("could not find a part of the path", StringComparison.OrdinalIgnoreCase))
            return "경로를 찾을 수 없습니다.";

        if (r.Contains("보호 경로", StringComparison.Ordinal) || r.Contains("허용 루트", StringComparison.Ordinal))
            return r.Length <= 120 ? r : r[..117] + "...";

        if (r.Length > 120)
            return r[..117] + "...";
        return r;
    }
}
