namespace EROptimizer.Core.Services;

public static class BootConfigPackage
{
    public static int RecommendedJobWorkerCount() =>
        Math.Max(1, Environment.ProcessorCount - 1);

    public static string BuildBlockText()
    {
        var jw = RecommendedJobWorkerCount();
        const string anchor = "hdr-display-enabled=1";
        var block = ErConstants.BootConfigPackageBlock;
        var idx = block.IndexOf(anchor, StringComparison.Ordinal);
        if (idx < 0)
            return block.TrimEnd() + "\njob-worker-count=" + jw;
        var after = idx + anchor.Length;
        return block.Insert(after, "\njob-worker-count=" + jw);
    }
}
