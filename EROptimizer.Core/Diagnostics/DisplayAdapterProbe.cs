using System.Management;

namespace EROptimizer.Core.Diagnostics;

public static class DisplayAdapterProbe
{
    public static IReadOnlyList<DisplayAdapterInfo> GetAdapters()
    {
        var list = new List<DisplayAdapterInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, DriverVersion, DriverDate, PNPDeviceID FROM Win32_VideoController");
            foreach (var o in searcher.Get())
            {
                using var mo = (ManagementObject)o;
                var name = mo["Name"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (name!.Contains("Basic Render Driver", StringComparison.OrdinalIgnoreCase))
                    continue;
                var dv = mo["DriverVersion"]?.ToString() ?? "";
                var rawDd = mo["DriverDate"]?.ToString();
                var dd = TryFormatDriverDate(rawDd);
                var pnp = mo["PNPDeviceID"]?.ToString() ?? "";
                list.Add(new DisplayAdapterInfo
                {
                    Name = name!,
                    DriverVersion = dv,
                    DriverDate = string.IsNullOrEmpty(dd) ? null : dd,
                    PnpDeviceId = pnp
                });
            }
        }
        catch
        {
            /* */
        }

        var smiVer = NvidiaSmiDriverVersion.TryQueryDriverVersion();
        if (!string.IsNullOrEmpty(smiVer))
        {
            for (var i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a.Name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                list[i] = new DisplayAdapterInfo
                {
                    Name = a.Name,
                    DriverVersion = smiVer!,
                    DriverDate = a.DriverDate,
                    PnpDeviceId = a.PnpDeviceId
                };
            }
        }

        return list;
    }

    private static string? TryFormatDriverDate(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            return ManagementDateTimeConverter.ToDateTime(raw).ToString("yyyy-MM-dd");
        }
        catch
        {
            return raw;
        }
    }
}
