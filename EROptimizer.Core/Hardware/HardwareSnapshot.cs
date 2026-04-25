namespace EROptimizer.Core.Hardware;

public enum CpuVendor
{
    Unknown,
    Intel,
    Amd
}

public enum GpuVendor
{
    Unknown,
    Nvidia,
    Amd,
    Intel
}

public sealed class HardwareSnapshot
{
    public CpuVendor Cpu { get; init; }
    public GpuVendor PrimaryDiscrete { get; init; }
    public IReadOnlyList<string> AdapterNames { get; init; } = Array.Empty<string>();

    public bool HasNvidia => AdapterNames.Any(n => n.Contains("nvidia", StringComparison.OrdinalIgnoreCase));
    public bool HasAmdGpu => AdapterNames.Any(n =>
        n.Contains("amd", StringComparison.OrdinalIgnoreCase) ||
        n.Contains("radeon", StringComparison.OrdinalIgnoreCase));
    public bool HasIntelGpu => AdapterNames.Any(n => n.Contains("intel", StringComparison.OrdinalIgnoreCase));

    public bool ShouldExportNvidiaSafeProfile => HasNvidia;

    public string SummaryLine =>
        $"CPU={Cpu}, 주GPU(추정)={PrimaryDiscrete}, NV안전프로필={(ShouldExportNvidiaSafeProfile ? "생성" : "생략")}, 어댑터={string.Join(" | ", AdapterNames.Select(a => a.Length > 40 ? a[..40] + "…" : a))}";
}
