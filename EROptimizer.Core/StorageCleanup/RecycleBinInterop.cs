using System.Runtime.InteropServices;

namespace EROptimizer.Core.StorageCleanup;

internal static class RecycleBinInterop
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    public static bool TryQueryRecycleBinForDrive(string driveRoot, out long sizeBytes)
    {
        sizeBytes = 0;
        var root = driveRoot.TrimEnd('\\') + "\\";
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO)) };
        var hr = SHQueryRecycleBin(root, ref info);
        if (hr != 0)
            return false;
        sizeBytes = Math.Max(0, info.i64Size);
        return true;
    }
}
