using System.Security;

namespace EROptimizer.Core.StorageCleanup;

public static class CleanupPathSafety
{
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("경로가 비어 있습니다.", nameof(path));
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
        return Path.GetFullPath(expanded);
    }

    public static bool PathExists(string path) =>
        File.Exists(path) || Directory.Exists(path);

    public static bool IsReparsePoint(string path)
    {
        if (!PathExists(path))
            return false;
        try
        {
            var attr = File.GetAttributes(path);
            return (attr & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return true;
        }
    }

    public static bool IsStrictChildOrEqual(string root, string path)
    {
        root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(root, path, StringComparison.OrdinalIgnoreCase))
            return true;
        return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || path.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProtectedSystemPath(string normalizedPath, CleanupProtectionContext ctx)
    {
        var p = normalizedPath;

        if (string.IsNullOrEmpty(p) || p.Length < 3)
            return true;

        foreach (var steamRoot in ctx.SteamAppsRoots)
        {
            if (IsStrictChildOrEqual(steamRoot, p))
                return true;
        }

        if (!string.IsNullOrEmpty(ctx.EternalReturnInstallDirectory)
            && IsStrictChildOrEqual(ctx.EternalReturnInstallDirectory, p))
            return true;

        var pf86 = GetProgramFilesX86();
        var pf = GetProgramFiles();
        if (IsStrictChildOrEqual(pf, p) || IsStrictChildOrEqual(pf86, p))
            return true;

        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrEmpty(win))
        {
            var winNorm = NormalizePath(win);
            var winSxS = Path.Combine(winNorm, "WinSxS");
            var driverStore = Path.Combine(winNorm, "System32", "DriverStore");
            var sys32 = Path.Combine(winNorm, "System32");
            if (IsStrictChildOrEqual(winSxS, p) || IsStrictChildOrEqual(driverStore, p))
                return true;
            // Entire System32 (except we never target it for cleanup)
            if (IsStrictChildOrEqual(sys32, p))
                return true;

            // C:\Windows except \Temp — block direct deletes under Windows root for generic paths
            if (IsStrictChildOrEqual(winNorm, p))
            {
                var winTemp = Path.Combine(winNorm, "Temp");
                if (!IsStrictChildOrEqual(winTemp, p))
                    return true;
            }
        }

        // Under %UserProfile%: only explicit allow paths are permitted elsewhere; everything else blocked.
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            var up = NormalizePath(userProfile);
            if (IsStrictChildOrEqual(up, p))
            {
                var tempOk = NormalizePath(Path.GetTempPath());
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var crashOk = NormalizePath(Path.Combine(local, "CrashDumps"));
                var d3dOk = NormalizePath(Path.Combine(local, "D3DSCache"));

                if (IsStrictChildOrEqual(tempOk, p)) return false;
                if (IsStrictChildOrEqual(crashOk, p)) return false;
                if (IsStrictChildOrEqual(d3dOk, p)) return false;

                return true;
            }
        }

        return false;
    }

    private static string GetProgramFiles() =>
        NormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

    private static string GetProgramFilesX86() =>
        NormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

    public static bool IsAllowedDeletionTarget(string allowRoot, string candidate, CleanupProtectionContext ctx, out string? reason)
    {
        reason = null;
        try
        {
            string root;
            string cand;
            try
            {
                root = NormalizePath(allowRoot);
                cand = NormalizePath(candidate);
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }

            if (IsReparsePoint(root))
            {
                reason = "허용 루트가 재분석 지점(junction/symlink)입니다.";
                return false;
            }

            if (IsReparsePoint(cand))
            {
                reason = "대상이 재분석 지점(junction/symlink)입니다.";
                return false;
            }

            if (!IsStrictChildOrEqual(root, cand))
            {
                reason = "허용 루트 밖입니다.";
                return false;
            }

            if (IsProtectedSystemPath(cand, ctx))
            {
                reason = "보호 경로 규칙에 해당합니다.";
                return false;
            }

            return true;
        }
        catch (SecurityException ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    public static string GetCanonicalAllowRoot(CleanupAllowKind kind)
    {
        return kind switch
        {
            CleanupAllowKind.UserTemp => NormalizePath(Path.GetTempPath()),
            CleanupAllowKind.WindowsTemp => NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")),
            CleanupAllowKind.NvidiaRootCache => NormalizePath(Path.Combine("C:", "NVIDIA")),
            CleanupAllowKind.AmdRootCache => NormalizePath(Path.Combine("C:", "AMD")),
            CleanupAllowKind.WerReportQueue => NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "Windows", "WER", "ReportQueue")),
            CleanupAllowKind.WerReportArchive => NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "Windows", "WER", "ReportArchive")),
            CleanupAllowKind.LocalCrashDumps => NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps")),
            CleanupAllowKind.DeliveryOptimizationCache => NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "Windows", "DeliveryOptimization", "Cache")),
            CleanupAllowKind.DirectXD3DCache => NormalizePath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache")),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}

public enum CleanupAllowKind
{
    UserTemp,
    WindowsTemp,
    NvidiaRootCache,
    AmdRootCache,
    WerReportQueue,
    WerReportArchive,
    LocalCrashDumps,
    DeliveryOptimizationCache,
    DirectXD3DCache
}
