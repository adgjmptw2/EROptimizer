using System.Threading;
using EROptimizer.Core;
using EROptimizer.Core.Diagnostics;

namespace EROptimizer.Cli;

internal static class MineConsoleUi
{
    private static readonly string[] MineBlockAscii =
    [
        "███╗   ███╗██╗███╗   ██╗███╗   ██╗███████╗",
        "████╗ ████║██║████╗  ██║████╗  ██║██╔════╝",
        "██╔████╔██║██║██╔██╗ ██║██╔██╗ ██║█████╗ ",
        "██║╚██╔╝██║██║██║╚██╗██║██║╚██╗██║██╔══╝ ",
        "██║ ╚═╝ ██║██║██║ ╚████║██║ ╚████║███████╗",
        "╚═╝     ╚═╝╚═╝╚═╝  ╚═══╝╚═╝  ╚═══╝╚══════╝"
    ];

    private static readonly int BarWidth = Math.Max(46, MineBlockAscii.Max(static s => s.Length) + 2);

    public static void PrintBanner()
    {
        Console.Clear();
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        PrintBar('=');

        var artW = MineBlockAscii.Max(static s => s.Length);
        foreach (var line in MineBlockAscii)
        {
            var pad = Math.Max(0, (BarWidth - artW) / 2);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(new string(' ', pad));
            Console.WriteLine(line);
        }

        Console.WriteLine();
        const string sub = "Eternal Return  ·  System Optimizer";
        var subPad = Math.Max(0, (BarWidth - sub.Length) / 2);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(new string(' ', subPad) + sub);

        Console.WriteLine();
        PrintBar('=');
        Console.ResetColor();
    }

    public static void PrintBar(char ch = '=')
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string(ch, BarWidth));
        Console.ResetColor();
    }

    public static void PrintThinBar()
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(new string('-', BarWidth));
        Console.ResetColor();
    }

    public static void PrintMenuTitle()
    {
        PrintBar('=');
        WriteCyanLine(CenterPad("이터널리턴 최적화 도우미", BarWidth));
        PrintBar('=');
    }

    public static void PrintDiscoveryFlow(GameDiscoveryResult d, int stepDelayMs = 200)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("컴퓨터에서 ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("이터널 리턴");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" 설치 폴더를 찾는 중입니다 . . .");
        Thread.Sleep(stepDelayMs);

        if (!string.IsNullOrEmpty(d.SteamRoot))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("   - 스팀 설치 경로 확인 : ");
            WriteGreenLine(Shorten(d.SteamRoot, PathMaxConsole));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   - 스팀 설치 경로 확인 : (레지스트리에서 찾지 못함)");
            Console.ResetColor();
        }

        Thread.Sleep(stepDelayMs);

        if (!string.IsNullOrEmpty(d.InstallDirectory))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("   - 이터널 리턴 설치 폴더 확인 : ");
            WriteGreenLine(Shorten(d.InstallDirectory, PathMaxConsole));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   - 이터널 리턴 설치 폴더 확인 : (appmanifest 또는 폴더 없음)");
            Console.ResetColor();
        }

        Thread.Sleep(stepDelayMs);

        if (d.IsComplete)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("   - 실행 파일 확인 : ");
            WriteGreenLine(Shorten(d.GameExePath!, PathMaxConsole));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   - 실행 파일 확인 : (자동 탐색 실패 — 메뉴 [6]에서 수동 지정)");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public static void PrintMainMenu()
    {
        PrintThinBar();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[1] 기본 패키지 적용  (DNS, 게임바, GPU, 전원, TEMP, NV JSON, boot.config)");
        Console.WriteLine("[2] boot.config만 패치");
        Console.WriteLine("[3] 백업 버전 재적용  (레지·전원·boot.config)");
        Console.WriteLine("[4] boot.config만 백업 불러오기");
        Console.WriteLine("[5] 설치 경로 다시 스캔");
        Console.WriteLine("[6] 실행 파일 수동 지정 (경로 입력 / 파일 선택)");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("[Q] 종료");
        Console.ResetColor();
    }

    public static string? PromptLine(string label)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(label);
        Console.ResetColor();
        return Console.ReadLine();
    }

    private const int PathMaxConsole = 68;

    public static string Shorten(string path, int max)
    {
        if (string.IsNullOrEmpty(path) || path.Length <= max) return path;
        var take = max - 1;
        if (take < 8) take = 8;
        return "…" + path[^take..];
    }

    private static string CenterPad(string text, int width)
    {
        if (text.Length >= width) return text;
        var pad = (width - text.Length) / 2;
        return new string(' ', pad) + text;
    }

    private static void WriteGreenLine(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static void WriteCyanLine(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void PrintSystemDiagnosis(string title, SystemDiagnosisResult d)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" " + title);
        Console.ResetColor();
        PrintBar('-');
        DiagnosisLine("Game Bar", d.GameBarOk, d.GameBarDetail);
        DiagnosisLine("Game DVR", d.GameDvrOk, d.GameDvrDetail);
        DiagnosisLine("전원 계획", d.PowerHighPerformance, d.PowerDetail);
        DiagnosisLine("Eternal Return GPU 설정", d.GameGpuHighPerformance, d.GameGpuDetail);
        DiagnosisLine("boot.config", d.BootConfigOk, d.BootConfigDetail);
        DiagnosisLine("TEMP 용량", d.TempDriveEnoughSpace, d.TempSpaceDetail);
        PrintBar('-');
    }

    private static void DiagnosisLine(string label, bool ok, string detail)
    {
        WritePassFailIcon(ok);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        var tail = string.IsNullOrEmpty(detail) ? "" : ": " + detail;
        Console.WriteLine(" " + label + tail);
        Console.ResetColor();
    }

    private static void WritePassFailIcon(bool ok)
    {
        if (ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✔ ");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("❌ ");
        }
        Console.ResetColor();
    }

    public static void PrintAdapterDriversAndNotes(SystemDiagnosisResult d, NvidiaGfeLatestInfo? gfe)
    {
        Console.WriteLine();
        PrintBar('=');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 그래픽 드라이버");
        Console.ResetColor();
        PrintBar('=');
        if (d.Adapters.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" 표시 어댑터 없음(WMI).");
            Console.ResetColor();
        }
        else
        {
            for (var i = 0; i < d.Adapters.Count; i++)
            {
                var a = d.Adapters[i];
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(" [" + (i + 1) + "] " + Shorten(a.Name, 72));
                var ver = string.IsNullOrEmpty(a.DriverVersion) ? "?" : a.DriverVersion;
                var tail = string.IsNullOrEmpty(a.DriverDate) ? "" : "  (" + a.DriverDate + ")";
                Console.WriteLine("     드라이버: " + ver + tail);
            }
        }

        var nvidia = d.Adapters.Any(x => x.Name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0);
        var nv = d.Adapters.FirstOrDefault(x => x.Name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0);
        if (nvidia && nv != null)
        {
            if (gfe is { Version: { Length: > 0 } v })
            {
                var cmp = NvidiaDriverVersionCompare.Compare(v, nv.DriverVersion);
                var gfeTail = string.IsNullOrEmpty(gfe.ReleaseDate) ? "" : "  (" + gfe.ReleaseDate + ")";
                if (cmp < 0)
                {
                    Console.WriteLine("     GFE 조회: " + v + gfeTail + " — 설치(" + nv.DriverVersion + ")보다 낮음 (조회·카드 ID 불일치 가능). 최신 GRD는 nvidia.com/drivers");
                }
                else
                    Console.WriteLine("     GFE 최신: " + v + gfeTail);
            }
            else
                Console.WriteLine("     GFE 최신: (조회 실패 — nvidia.com 또는 GeForce Experience)");
        }
        else
            Console.WriteLine(" 최신 드라이버: AMD / Intel 각각 공식 지원 페이지에서 OS에 맞게 설치.");

        if (d.TempDriveEnoughSpace is false)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" 참고: TEMP 드라이브 여유가 부족합니다. 위 진단의 TEMP 항목을 확인하세요.");
            Console.ResetColor();
        }
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" (설치 버전=nvidia-smi / GFE는 비공식 API·공식 사이트·배포 채널과 어긋날 수 있음)");
        Console.ResetColor();
        PrintBar('=');
    }
}
