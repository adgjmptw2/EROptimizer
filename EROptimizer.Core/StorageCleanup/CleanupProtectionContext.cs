using EROptimizer.Core;

namespace EROptimizer.Core.StorageCleanup;

public sealed class CleanupProtectionContext
{
    public CleanupProtectionContext(IReadOnlyList<string>? steamAppsRoots = null, string? eternalReturnInstallDirectory = null)
    {
        SteamAppsRoots = steamAppsRoots ?? Array.Empty<string>();
        EternalReturnInstallDirectory = eternalReturnInstallDirectory;
    }

    public IReadOnlyList<string> SteamAppsRoots { get; }
    public string? EternalReturnInstallDirectory { get; }

    public static CleanupProtectionContext FromDiscovery(GameDiscoveryResult d)
    {
        List<string> steamApps = [];
        if (!string.IsNullOrEmpty(d.SteamRoot))
        {
            foreach (var root in SteamGameLocator.EnumerateSteamAppsRoots(d.SteamRoot))
                steamApps.Add(CleanupPathSafety.NormalizePath(root));
        }

        string? er = string.IsNullOrEmpty(d.InstallDirectory)
            ? null
            : CleanupPathSafety.NormalizePath(d.InstallDirectory);
        return new CleanupProtectionContext(steamApps, er);
    }
}
