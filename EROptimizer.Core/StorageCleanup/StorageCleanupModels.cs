namespace EROptimizer.Core.StorageCleanup;

public enum CleanupRiskLabel
{
    Recommended,
    Caution,
    Advanced
}

public sealed class CleanupPreviewRow
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public CleanupRiskLabel Risk { get; init; }
    public required string PathOrDescription { get; init; }
    public long EstimatedBytes { get; init; }
    public bool SelectedByDefault { get; init; }
    public required string RecoverabilityNote { get; init; }
    public required string CautionNote { get; init; }
    public bool CanExecute { get; init; } = true;
}

public sealed class DriveSummaryRow
{
    public required string Name { get; init; }
    public long TotalBytes { get; init; }
    public long FreeBytes { get; init; }
}

public sealed class StorageAnalysisReport
{
    public List<DriveSummaryRow> Drives { get; init; } = [];
    public bool DriveCLowSpace { get; init; }
    public string? DriveCLowSpaceNote { get; init; }
    public long UserTempBytes { get; init; }
    public required string UserTempPath { get; init; }
    public long WindowsTempBytes { get; init; }
    public required string WindowsTempPath { get; init; }
    public long RecycleBinEstimatedBytes { get; init; }
    public bool RecycleBinKnown { get; init; }
    public bool WindowsOldExists { get; init; }
    public long WindowsOldBytes { get; init; }
    public required string WindowsOldPath { get; init; }
    public bool NvidiaRootExists { get; init; }
    public bool NvidiaRootIsReparse { get; init; }
    public long NvidiaRootBytes { get; init; }
    public bool AmdRootExists { get; init; }
    public bool AmdRootIsReparse { get; init; }
    public long AmdRootBytes { get; init; }
    public long DeliveryOptimizationCacheBytes { get; init; }
    public required string DeliveryOptimizationCachePath { get; init; }
    public long DirectXD3DCacheBytes { get; init; }
    public required string DirectXD3DCachePath { get; init; }
    public long WerQueuedBytes { get; init; }
    public long WerArchiveBytes { get; init; }
    public long LocalCrashDumpBytes { get; init; }
    public required string WerReportQueuePath { get; init; }
    public required string WerReportArchivePath { get; init; }
    public required string LocalCrashDumpPath { get; init; }
}

public sealed class CleanupFailureRecord
{
    public required string Path { get; init; }
    public required string Reason { get; init; }
}

public sealed record CleanupItemResult
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public bool Ran { get; init; }
    public long BytesReclaimed { get; init; }
    public int FilesDeleted { get; init; }
    public int DirectoriesRemoved { get; init; }
    public int Skipped { get; init; }
    public List<CleanupFailureRecord> Failures { get; init; } = new();
}

public sealed class CleanupExecutionReport
{
    public DateTime StartedUtc { get; set; }
    public DateTime FinishedUtc { get; set; }
    public long TotalBytesReclaimed { get; set; }
    public List<CleanupItemResult> Items { get; } = [];
}
