namespace EROptimizer.Core.Diagnostics;

public static class GpuVendorDetector
{
    public static GpuVendor Detect(string? pnpDeviceId, string? adapterName)
    {
        var pnp = pnpDeviceId ?? "";
        if (pnp.Contains("VEN_10DE", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Nvidia;
        if (pnp.Contains("VEN_1002", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Amd;
        if (pnp.Contains("VEN_8086", StringComparison.OrdinalIgnoreCase))
            return GpuVendor.Intel;

        var n = adapterName ?? "";
        if (ContainsAny(n, "NVIDIA", "GeForce", "RTX", "GTX", "Quadro"))
            return GpuVendor.Nvidia;
        if (ContainsAny(n, "AMD", "Radeon"))
            return GpuVendor.Amd;
        if (ContainsAny(n, "Intel", "Iris", "UHD", "Arc"))
            return GpuVendor.Intel;

        return GpuVendor.Unknown;
    }

    private static bool ContainsAny(string hay, params string[] needles)
    {
        foreach (var x in needles)
        {
            if (hay.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
