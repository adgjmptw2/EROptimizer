namespace EROptimizer.Core.StorageCleanup;

public static class StorageCleanupFileOps
{
    public static long GetDirectorySizeSafe(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            return 0;
        try
        {
            var root = CleanupPathSafety.NormalizePath(directoryPath);
            if (CleanupPathSafety.IsReparsePoint(root))
                return 0;
            return MeasureRecursive(root);
        }
        catch
        {
            return 0;
        }
    }

    private static long MeasureRecursive(string dir)
    {
        long total = 0;
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            if (CleanupPathSafety.IsReparsePoint(file))
                continue;
            try
            {
                total += new FileInfo(file).Length;
            }
            catch { /* skip */ }
        }

        foreach (var sub in Directory.EnumerateDirectories(dir))
        {
            if (CleanupPathSafety.IsReparsePoint(sub))
                continue;
            total += MeasureRecursive(sub);
        }

        return total;
    }

    public static void DeleteTreeContents(
        string allowRoot,
        string root,
        CleanupProtectionContext ctx,
        ICollection<CleanupFailureRecord> failures,
        out long bytesFreed,
        out int filesDeleted,
        out int dirsRemoved,
        out int skipped)
    {
        bytesFreed = 0;
        filesDeleted = 0;
        dirsRemoved = 0;
        skipped = 0;

        if (!Directory.Exists(root))
            return;

        string normAllow;
        string normRoot;
        try
        {
            normAllow = CleanupPathSafety.NormalizePath(allowRoot);
            normRoot = CleanupPathSafety.NormalizePath(root);
        }
        catch
        {
            return;
        }

        if (CleanupPathSafety.IsReparsePoint(normRoot))
            return;

        DeleteRecursive(normAllow, normRoot, ctx, failures, ref bytesFreed, ref filesDeleted, ref dirsRemoved, ref skipped);
    }

    private static void DeleteRecursive(
        string allowRoot,
        string dir,
        CleanupProtectionContext ctx,
        ICollection<CleanupFailureRecord> failures,
        ref long bytesFreed,
        ref int filesDeleted,
        ref int dirsRemoved,
        ref int skipped)
    {
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            if (CleanupPathSafety.IsReparsePoint(file))
            {
                skipped++;
                continue;
            }

            if (!CleanupPathSafety.IsAllowedDeletionTarget(allowRoot, file, ctx, out var r))
            {
                failures.Add(new CleanupFailureRecord { Path = file, Reason = r ?? "거부" });
                skipped++;
                continue;
            }

            try
            {
                long len = 0;
                try
                {
                    len = new FileInfo(file).Length;
                }
                catch { /* ignore */ }

                File.Delete(file);
                bytesFreed += len;
                filesDeleted++;
            }
            catch (Exception ex)
            {
                failures.Add(new CleanupFailureRecord { Path = file, Reason = ex.Message });
                skipped++;
            }
        }

        foreach (var sub in Directory.EnumerateDirectories(dir))
        {
            if (CleanupPathSafety.IsReparsePoint(sub))
            {
                skipped++;
                continue;
            }

            if (!CleanupPathSafety.IsAllowedDeletionTarget(allowRoot, sub, ctx, out var r2))
            {
                failures.Add(new CleanupFailureRecord { Path = sub, Reason = r2 ?? "거부" });
                skipped++;
                continue;
            }

            DeleteRecursive(allowRoot, sub, ctx, failures, ref bytesFreed, ref filesDeleted, ref dirsRemoved, ref skipped);

            try
            {
                if (!CleanupPathSafety.IsAllowedDeletionTarget(allowRoot, sub, ctx, out _))
                    continue;
                if (Directory.Exists(sub) && Directory.EnumerateFileSystemEntries(sub).Any())
                    continue;
                Directory.Delete(sub, false);
                dirsRemoved++;
            }
            catch (Exception ex)
            {
                failures.Add(new CleanupFailureRecord { Path = sub, Reason = ex.Message });
                skipped++;
            }
        }
    }
}
