namespace EROptimizer.Core.Diagnostics;

public sealed class SystemDiagnosisResult
{
    public bool GameBarOk { get; init; }
    public string GameBarDetail { get; init; } = "";

    public bool GameDvrOk { get; init; }
    public string GameDvrDetail { get; init; } = "";

    public bool PowerHighPerformance { get; init; }
    public string PowerDetail { get; init; } = "";

    public bool GameGpuHighPerformance { get; init; }
    public string GameGpuDetail { get; init; } = "";

    public bool BootConfigOk { get; init; }
    public string BootConfigDetail { get; init; } = "";

    public bool TempDriveEnoughSpace { get; init; }
    public string TempSpaceDetail { get; init; } = "";

    public IReadOnlyList<DisplayAdapterInfo> Adapters { get; init; } = Array.Empty<DisplayAdapterInfo>();
}

public sealed class DisplayAdapterInfo
{
    public string Name { get; init; } = "";
    public string DriverVersion { get; init; } = "";
    public string? DriverDate { get; init; }
    public string PnpDeviceId { get; init; } = "";
}
