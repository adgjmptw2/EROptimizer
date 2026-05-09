using Newtonsoft.Json;

namespace EROptimizer.Core.Diagnostics;

public static class DiagnosticResultWriter
{
    public static void Save(string logsDirectory, string fileSessionId, PostApplyDiagnosticReport report)
    {
        Directory.CreateDirectory(logsDirectory);
        var jsonPath = Path.Combine(logsDirectory, $"diagnostic_result_{fileSessionId}.json");
        var logPath = Path.Combine(logsDirectory, $"diagnostic_{fileSessionId}.log");

        var json = JsonConvert.SerializeObject(report, Formatting.Indented);
        File.WriteAllText(jsonPath, json);

        var lines = new List<string>
        {
            $"generatedUtc={report.GeneratedUtc}",
            $"relatedPackageSession={report.RelatedPackageSessionId ?? ""}",
            "",
            "[요약]",
            $"전원: {(report.PowerHighPerformance ? "고성능 쪽" : "기타")} — {report.PowerPlanDetail}",
            $"Game Bar: {report.GameBarDetail}",
            $"Game DVR: {report.GameDvrDetail}",
            $"ER GPU: {report.GameGpuDetail}",
            $"boot.config: {report.BootConfigDetail}",
            "",
            "[GPU]",
        };
        foreach (var g in report.Gpus)
        {
            lines.Add(
                $"{g.Name} | {g.Vendor} | ver={g.DriverVersion} | date={g.DriverDate ?? "?"} | 참고={g.DriverAgeReference}");
        }

        lines.Add("");
        lines.Add("[모니터]");
        lines.Add($"현재: {(report.MonitorCurrentHz > 0 ? report.MonitorCurrentHz + "Hz" : "확인 불가")}");
        lines.Add($"후보: {report.MonitorCandidatesText}");
        lines.Add($"메모: {report.MonitorJudgment}");
        lines.Add("");
        lines.Add("[오버레이·녹화·런처]");
        foreach (var o in report.Overlays)
        {
            var mb = o.RamBytes / (1024.0 * 1024.0);
            lines.Add(
                $"{o.Label}: {(o.Running ? "실행 중" : "없음")} CPU≈{o.CpuPercentApprox}% RAM≈{mb:0}MB");
        }

        lines.Add("");
        lines.Add($"full_json={jsonPath}");
        File.WriteAllText(logPath, string.Join(Environment.NewLine, lines));
    }
}
