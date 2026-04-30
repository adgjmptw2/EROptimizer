using System.Management;

namespace EROptimizer.Core.Diagnostics;

public static class ChassisHelper
{
    public static bool IsNotebookChassis()
    {
        try
        {
            using var s = new ManagementObjectSearcher("SELECT PCSystemType FROM Win32_ComputerSystem");
            foreach (var o in s.Get())
            {
                using var m = (ManagementObject)o;
                var t = m["PCSystemType"];
                if (t is ushort u) return u == 2;
                if (t is uint ui) return ui == 2;
                if (t is int i) return i == 2;
            }
        }
        catch
        {
            /* */
        }
        return false;
    }
}
