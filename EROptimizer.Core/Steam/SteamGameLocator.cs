using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace EROptimizer.Core;

public static class SteamGameLocator
{
    private static readonly Regex VdfPathRegex = new("\"path\"\\s+\"((?:\\\\\"|[^\"])*)\"", RegexOptions.Compiled);
    private static readonly Regex AcfInstallDirRegex = new("^\\s*\"installdir\"\\s+\"([^\"]*)\"\\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public static string? TryReadSteamPathFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        var v = key?.GetValue("SteamPath") as string;
        if (string.IsNullOrWhiteSpace(v)) return null;
        return v.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
    }

    public static IReadOnlyList<string> EnumerateSteamAppsRoots(string steamRoot)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var defaultApps = Path.Combine(steamRoot, "steamapps");
        if (Directory.Exists(defaultApps))
            set.Add(defaultApps);

        var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            var text = File.ReadAllText(vdf);
            foreach (Match m in VdfPathRegex.Matches(text))
            {
                var raw = m.Groups[1].Value.Replace(@"\\", @"\");
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var libRoot = raw.TrimEnd(Path.DirectorySeparatorChar);
                var apps = Path.Combine(libRoot, "steamapps");
                if (Directory.Exists(apps))
                    set.Add(apps);
            }
        }

        return set.ToList();
    }

    public static string? TryReadInstallDirFromAcf(string acfPath)
    {
        if (!File.Exists(acfPath)) return null;
        var text = File.ReadAllText(acfPath);
        var m = AcfInstallDirRegex.Match(text);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string? TryFindGameInstallDirectory()
    {
        var steam = TryReadSteamPathFromRegistry();
        if (string.IsNullOrEmpty(steam)) return null;

        var acfName = $"appmanifest_{ErConstants.SteamAppId}.acf";
        foreach (var apps in EnumerateSteamAppsRoots(steam))
        {
            var acf = Path.Combine(apps, acfName);
            var installdir = TryReadInstallDirFromAcf(acf);
            if (string.IsNullOrEmpty(installdir)) continue;
            var common = Path.Combine(apps, "common", installdir);
            if (Directory.Exists(common))
                return common;
        }

        return null;
    }

    public static string? TryResolveGameExe(string installDir)
    {
        var primary = Path.Combine(installDir, ErConstants.GameExePrimary);
        if (File.Exists(primary))
            return Path.GetFullPath(primary);

        string[] noise = { "UnityCrashHandler", "vc_redist", "uninstall", "DXSETUP", "Install" };
        var exes = Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
            .Where(p =>
            {
                var n = Path.GetFileNameWithoutExtension(p);
                return noise.All(x => n.IndexOf(x, StringComparison.OrdinalIgnoreCase) < 0);
            })
            .OrderByDescending(p => p.Contains("Eternal", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(p => p.Length)
            .ToList();

        return exes.Count > 0 ? Path.GetFullPath(exes[0]) : null;
    }

    public static string? TryAutoFindGameExe() =>
        TryFindGameInstallDirectory() is { } dir ? TryResolveGameExe(dir) : null;

    public static GameDiscoveryResult DiscoverDetailed()
    {
        var steam = TryReadSteamPathFromRegistry();
        var install = TryFindGameInstallDirectory();
        var exe = install != null ? TryResolveGameExe(install) : null;
        return new GameDiscoveryResult
        {
            SteamRoot = steam,
            InstallDirectory = install,
            GameExePath = exe
        };
    }
}
