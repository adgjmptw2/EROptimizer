namespace EROptimizer.Core.Diagnostics;

public sealed class GpuDiagnosticRow
{
    public string Name { get; init; } = "";
    public string Vendor { get; init; } = "";
    public string DriverVersion { get; init; } = "";
    public string? DriverDate { get; init; }
    public string DriverAgeReference { get; init; } = "";
}

public sealed class PostApplyDiagnosticReport
{
    public string GeneratedUtc { get; init; } = "";
    public string? RelatedPackageSessionId { get; init; }

    public IReadOnlyList<string> Disclaimers { get; init; } = Array.Empty<string>();

    public string PowerPlanDetail { get; init; } = "";
    public bool PowerHighPerformance { get; init; }

    public string GameBarDetail { get; init; } = "";
    public bool GameBarOk { get; init; }

    public string GameDvrDetail { get; init; } = "";
    public bool GameDvrOk { get; init; }

    public string GameGpuDetail { get; init; } = "";
    public bool GameGpuOk { get; init; }

    public string BootConfigDetail { get; init; } = "";
    public bool BootConfigOk { get; init; }

    public IReadOnlyList<GpuDiagnosticRow> Gpus { get; init; } = Array.Empty<GpuDiagnosticRow>();

    public int MonitorCurrentHz { get; init; }
    public IReadOnlyList<int> MonitorCandidateHz { get; init; } = Array.Empty<int>();
    public string MonitorCandidatesText { get; init; } = "";
    public string MonitorJudgment { get; init; } = "";

    public IReadOnlyList<OverlayAppSnapshot> Overlays { get; init; } = Array.Empty<OverlayAppSnapshot>();
}
