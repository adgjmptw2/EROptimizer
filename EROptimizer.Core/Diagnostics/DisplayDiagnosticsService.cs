using System.Runtime.InteropServices;

namespace EROptimizer.Core.Diagnostics;

public sealed class DisplayRefreshInfo
{
    public int CurrentHz { get; init; }
    public IReadOnlyList<int> CandidateHz { get; init; } = Array.Empty<int>();
    public string StatusLine { get; init; } = "";
    public string DetailNote { get; init; } = "";
}

public static class DisplayDiagnosticsService
{
    private const int EnumCurrentSettings = -1;

    public static DisplayRefreshInfo GetPrimaryDisplayRefresh()
    {
        var dm = NewDevMode();
        if (!EnumDisplaySettings(null, EnumCurrentSettings, ref dm))
            return Fail("디스플레이 설정을 읽지 못했습니다.");

        var curW = (int)dm.dmPelsWidth;
        var curH = (int)dm.dmPelsHeight;
        var curHz = (int)dm.dmDisplayFrequency;
        if (curHz <= 0)
            curHz = 0;

        var set = new HashSet<int>();
        for (var i = 0; i < 512; i++)
        {
            var m = NewDevMode();
            if (!EnumDisplaySettings(null, i, ref m))
                break;
            if ((int)m.dmPelsWidth == curW && (int)m.dmPelsHeight == curH)
            {
                var hz = (int)m.dmDisplayFrequency;
                if (hz > 0)
                    set.Add(hz);
            }
        }

        var list = set.OrderBy(x => x).ToList();
        if (list.Count == 0 && curHz > 0)
            list.Add(curHz);

        var maxHz = list.Count > 0 ? list.Max() : curHz;
        var candidates = string.Join(" / ", list.Select(h => h + "Hz"));
        if (string.IsNullOrEmpty(candidates))
            candidates = "확인 불가";

        string status;
        string note;
        if (curHz <= 0)
        {
            status = "현재 주사율: 확인 불가";
            note = "EnumDisplaySettings 값이 비어 있습니다.";
        }
        else
        {
            status = $"현재 주사율: {curHz}Hz";
            if (maxHz > curHz && curHz <= 60)
            {
                note = "더 높은 주사율 후보가 있습니다. Windows 디스플레이 설정에서 확인하세요.";
            }
            else if (maxHz > curHz)
            {
                note = "모드 목록에 현재보다 높은 주사율이 있습니다. Windows 설정을 확인하세요.";
            }
            else
            {
                note = "자동으로 주사율을 바꾸지 않습니다.";
            }
        }

        return new DisplayRefreshInfo
        {
            CurrentHz = curHz,
            CandidateHz = list,
            StatusLine = status,
            DetailNote = note
        };
    }

    private static DisplayRefreshInfo Fail(string msg) =>
        new()
        {
            CurrentHz = 0,
            CandidateHz = Array.Empty<int>(),
            StatusLine = "현재 주사율: 확인 불가",
            DetailNote = msg
        };

    private static DevMode NewDevMode()
    {
        var dm = new DevMode();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DevMode));
        return dm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DevMode lpDevMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}
