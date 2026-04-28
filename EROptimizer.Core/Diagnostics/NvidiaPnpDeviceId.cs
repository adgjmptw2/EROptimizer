using System.Text.RegularExpressions;

namespace EROptimizer.Core.Diagnostics;

internal static class NvidiaPnpDeviceId
{
    private static readonly Regex DevRe = new(@"DEV_([0-9A-Fa-f]{4})", RegexOptions.Compiled);
    private static readonly Regex SubsysRe = new(@"SUBSYS_([0-9A-Fa-f]{8})", RegexOptions.Compiled);

    public static bool TryBuildGfeDeviceId(string? pnpDeviceId, out string deviceIdString)
    {
        deviceIdString = "";
        if (string.IsNullOrWhiteSpace(pnpDeviceId))
            return false;
        var u = pnpDeviceId!.ToUpperInvariant();
        if (!u.Contains("VEN_10DE", StringComparison.Ordinal))
            return false;
        var dm = DevRe.Match(u);
        if (!dm.Success)
            return false;
        var dev = dm.Groups[1].Value.ToUpperInvariant();
        const string ven = "10DE";
        var sm = SubsysRe.Match(u);
        if (!sm.Success)
        {
            deviceIdString = $"{dev}_{ven}_0000_0000";
            return true;
        }
        var sub = sm.Groups[1].Value.ToUpperInvariant();
        deviceIdString = $"{dev}_{ven}_{sub[..4]}_{sub.Substring(4, 4)}";
        return true;
    }
}
