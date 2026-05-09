namespace EROptimizer.Core.Diagnostics;

public static class PostApplyDiagnosticService
{
    public static PostApplyDiagnosticReport Run(string? gameExe, string? relatedPackageSessionId,
        Action<string, int>? progress)
    {
        void P(string msg, int pct)
        {
            progress?.Invoke(msg, pct);
        }

        P("시스템 항목 읽는 중...", 15);
        var sys = SystemDiagnosisProbe.Collect(gameExe);

        P("모니터 주사율 확인 중...", 45);
        var disp = DisplayDiagnosticsService.GetPrimaryDisplayRefresh();

        P("오버레이 프로세스 확인 중...", 70);
        var overlay = OverlayProcessScanner.Scan(1000);

        P("결과 정리 중...", 90);
        var gpus = new List<GpuDiagnosticRow>();
        foreach (var a in sys.Adapters)
        {
            var v = GpuVendorDetector.Detect(a.PnpDeviceId, a.Name);
            var dt = DriverAgeEvaluator.TryParseDriverDateYyyyMmDd(a.DriverDate);
            gpus.Add(new GpuDiagnosticRow
            {
                Name = a.Name,
                Vendor = VendorLabel(v),
                DriverVersion = a.DriverVersion,
                DriverDate = a.DriverDate,
                DriverAgeReference = DriverAgeEvaluator.EvaluateReferenceLabel(dt)
            });
        }

        var hzText = disp.CandidateHz.Count > 0
            ? string.Join(" / ", disp.CandidateHz.Select(h => h + "Hz"))
            : "확인 불가";

        var report = new PostApplyDiagnosticReport
        {
            GeneratedUtc = DateTime.UtcNow.ToString("o"),
            RelatedPackageSessionId = relatedPackageSessionId,
            Disclaimers =
            [
                "그래픽 드라이버 최신 여부는 인터넷으로 확인하지 않습니다.",
                "현재 설치된 드라이버 날짜만 기준으로 참고 판단합니다."
            ],
            PowerPlanDetail = sys.PowerDetail,
            PowerHighPerformance = sys.PowerHighPerformance,
            GameBarDetail = sys.GameBarDetail,
            GameBarOk = sys.GameBarOk,
            GameDvrDetail = sys.GameDvrDetail,
            GameDvrOk = sys.GameDvrOk,
            GameGpuDetail = sys.GameGpuDetail,
            GameGpuOk = sys.GameGpuHighPerformance,
            BootConfigDetail = sys.BootConfigDetail,
            BootConfigOk = sys.BootConfigOk,
            Gpus = gpus,
            MonitorCurrentHz = disp.CurrentHz,
            MonitorCandidateHz = disp.CandidateHz,
            MonitorCandidatesText = hzText,
            MonitorJudgment = disp.DetailNote,
            Overlays = overlay
        };

        P("완료", 100);
        return report;
    }

    private static string VendorLabel(GpuVendor v) =>
        v switch
        {
            GpuVendor.Nvidia => "NVIDIA",
            GpuVendor.Amd => "AMD",
            GpuVendor.Intel => "Intel",
            _ => "Unknown"
        };
}