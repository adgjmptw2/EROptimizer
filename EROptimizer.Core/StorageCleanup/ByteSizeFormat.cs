namespace EROptimizer.Core.StorageCleanup;

public static class ByteSizeFormat
{
    public static string Format(long bytes)
    {
        if (bytes < 0) bytes = 0;
        const double kb = 1024;
        if (bytes < kb) return $"{bytes} B";
        double v = bytes;
        string[] u = ["KB", "MB", "GB", "TB"];
        var i = -1;
        do
        {
            v /= kb;
            i++;
        } while (v >= kb && i < u.Length - 1);

        return $"{v:0.##} {u[i]}";
    }
}
