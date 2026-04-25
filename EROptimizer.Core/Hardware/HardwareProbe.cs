using System.Management;

namespace EROptimizer.Core.Hardware;

public static class HardwareProbe
{
    public static HardwareSnapshot Probe()
    {
        var cpu = CpuVendor.Unknown;
        var adapters = new List<string>();

        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer FROM Win32_Processor"))
            {
                foreach (var o in searcher.Get())
                {
                    using var mo = (ManagementObject)o;
                    var m = mo["Manufacturer"]?.ToString() ?? mo["Name"]?.ToString() ?? "";
                    if (m.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                        cpu = CpuVendor.Intel;
                    else if (m.Contains("AuthenticAMD", StringComparison.OrdinalIgnoreCase) ||
                             m.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase) ||
                             m.Contains("AMD", StringComparison.OrdinalIgnoreCase))
                        cpu = CpuVendor.Amd;
                    break;
                }
            }
        }
        catch
        {
            /* WMI 실패 시 Unknown */
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (var o in searcher.Get())
            {
                using var mo = (ManagementObject)o;
                var n = mo["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(n))
                    adapters.Add(n.Trim());
            }
        }
        catch
        {
            /* */
        }

        var primary = ClassifyPrimaryDiscrete(adapters);
        return new HardwareSnapshot
        {
            Cpu = cpu,
            PrimaryDiscrete = primary,
            AdapterNames = adapters
        };
    }

    private static GpuVendor ClassifyPrimaryDiscrete(List<string> adapters)
    {
        if (adapters.Count == 0)
            return GpuVendor.Unknown;
        if (adapters.Any(a => a.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)))
            return GpuVendor.Nvidia;
        if (adapters.Any(a =>
                a.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                a.Contains("Radeon", StringComparison.OrdinalIgnoreCase)))
            return GpuVendor.Amd;
        if (adapters.Any(a => a.Contains("Intel", StringComparison.OrdinalIgnoreCase) && a.Contains("Arc", StringComparison.OrdinalIgnoreCase)))
            return GpuVendor.Intel;
        return GpuVendor.Unknown;
    }
}
