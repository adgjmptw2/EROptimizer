using System.Threading;
using EROptimizer.Core;

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
}
