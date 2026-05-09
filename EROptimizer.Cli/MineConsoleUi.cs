using System.IO;
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
        Console.WriteLine(" 설치 폴더를 찾는 중입니다...");
        Thread.Sleep(stepDelayMs);

        if (!string.IsNullOrEmpty(d.SteamRoot))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("   - 스팀 설치 경로 확인 : ");
            WriteGreenLine(Shorten(d.SteamRoot!, PathMaxConsole));
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
            WriteGreenLine(Shorten(d.InstallDirectory!, PathMaxConsole));
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
            Console.WriteLine("   - 실행 파일 확인 : (자동 탐색 실패 — 메뉴 [8]에서 수동 지정)");
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
        Console.WriteLine("[4] 모니터 주사율 점검  (읽기 전용)");
        Console.WriteLine("[5] 오버레이·녹화·런처 점검  (감지만)");
        Console.WriteLine("[6] boot.config만 백업 불러오기");
        Console.WriteLine("[7] 설치 경로 다시 스캔");
        Console.WriteLine("[8] 실행 파일 수동 지정 (경로 입력 / 파일 선택)");
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

    public static void ShowOptimizationCompleteExitPrompt()
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" 최적화가 완료되었습니다. 즐거운 루미아섬 여행 되세요! ! !");
        Console.ResetColor();
        PrintBar('-');
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" 아무 키나 누르면 종료됩니다.");
        Console.ResetColor();
        try
        {
            if (Console.IsInputRedirected)
                PromptLine("종료하려면 Enter… ");
            else
                Console.ReadKey(intercept: true);
        }
        catch (InvalidOperationException)
        {
            PromptLine("종료하려면 Enter… ");
        }
    }

    public static void PrintAdditionalDiagnosticsOffer(string workspace)
    {
        var logsDir = Path.Combine(workspace, "logs");
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" [ 추가 진단 ]  Y 를 누르면 아래를 한 번 더 읽습니다.");
        Console.ResetColor();
        PrintBar('-');
        Console.WriteLine("  - 그래픽 드라이버(WMI): 어댑터 이름·설치 버전·날짜, DriverDate 참고 문구");
        Console.WriteLine("  - 기본 디스플레이 현재 주사율 / 가능한 주사율 후보 (읽기 전용)");
        Console.WriteLine("  - Discord, Steam, Xbox Game Bar, NVIDIA 공유 등 점검 대상 프로세스");
        Console.WriteLine("    (실행 여부, CPU 약 1초 샘플, RAM Working Set 합산)");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  - 저장: {Shorten(logsDir + Path.DirectorySeparatorChar, 52)}diagnostic_<시간>.log");
        Console.WriteLine("          + diagnostic_result_<시간>.json");
        Console.ResetColor();
        PrintBar('-');
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

    public static void WriteDiagnosticProgress(string message, int percent)
    {
        if (percent < 0) percent = 0;
        else if (percent > 100) percent = 100;
        const int w = 10;
        var f = (int)Math.Round(w * (percent / 100.0));
        if (f > w) f = w;
        var bar = new string('#', f) + new string('-', w - f);
        var line = $"{message} [{bar}] {percent}%";
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("\r" + line);
        try
        {
            var cw = Console.WindowWidth;
            if (cw > 1 && line.Length < cw - 1)
                Console.Write(new string(' ', Math.Min(24, cw - 1 - line.Length)));
        }
        catch
        {
            /* */
        }

        Console.ResetColor();
    }

    public static void ClearDiagnosticProgressLine()
    {
        try
        {
            var w = Console.WindowWidth;
            if (w > 1)
                Console.Write("\r" + new string(' ', w - 1) + "\r");
            else
                Console.Write("\r" + new string(' ', 96) + "\r");
        }
        catch
        {
            Console.Write("\r" + new string(' ', 96) + "\r");
        }
    }

    private static void WriteMonitorCurrentHzLine(int currentHz)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("    - 현재 주사율 : ");
        Console.ResetColor();
        if (currentHz > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(currentHz);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" Hz");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("확인 불가");
            Console.ResetColor();
        }
    }

    private static void WriteMonitorCandidatesHzLine(int currentHz, IReadOnlyList<int> candidates, string plainFallback)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("    - 감지 후보   : ");
        Console.ResetColor();
        if (candidates == null || candidates.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(string.IsNullOrEmpty(plainFallback) ? "확인 불가" : plainFallback);
            Console.ResetColor();
            return;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var hz = candidates[i];
            if (i > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" / ");
                Console.ResetColor();
            }

            var isActive = currentHz > 0 && hz == currentHz;
            Console.ForegroundColor = isActive ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.Write($"{hz}Hz");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public static void PrintPostApplyDiagnosticSummary(PostApplyDiagnosticReport r)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" [ 추가 진단 요약 ]");
        Console.ResetColor();
        PrintBar('-');
        Console.WriteLine();
        PrintGraphicsDriverCheck(r.Gpus);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" >> 모니터 (기본 디스플레이, 자동 변경 없음)");
        Console.ResetColor();
        WriteMonitorCurrentHzLine(r.MonitorCurrentHz);
        WriteMonitorCandidatesHzLine(r.MonitorCurrentHz, r.MonitorCandidateHz, r.MonitorCandidatesText);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("    - 판단        : " + r.MonitorJudgment);
        Console.ResetColor();

        PrintOverlayInspectionReport(r.Overlays);
    }

    public static void PrintGraphicsDriverCheck(IReadOnlyList<GpuDiagnosticRow> gpus)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 그래픽 드라이버 점검");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" 인터넷으로 최신 버전을 확인하지 않고, 현재 설치된 드라이버 날짜만 확인합니다.");
        Console.ResetColor();
        if (gpus.Count == 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" (표시할 어댑터 없음)");
            Console.ResetColor();
            PrintBar('-');
            return;
        }

        foreach (var g in gpus)
        {
            var dt = DriverAgeEvaluator.TryParseDriverDateYyyyMmDd(g.DriverDate);
            var guide = DriverAgePresentation.GuidanceText(dt, g.DriverAgeReference);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("- " + g.Name);
            Console.ResetColor();
            Console.WriteLine("  드라이버: " + (string.IsNullOrEmpty(g.DriverVersion) ? "?" : g.DriverVersion));
            Console.WriteLine("  설치 날짜: " + (string.IsNullOrEmpty(g.DriverDate) ? "확인 불가" : g.DriverDate));
            Console.WriteLine("  상태: " + g.DriverAgeReference);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  안내: " + guide);
            Console.ResetColor();
        }

        PrintBar('-');
    }

    public static void PrintOverlayInspectionReport(IReadOnlyList<OverlayAppSnapshot> rows)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 오버레이 / 녹화 / 런처 점검");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" 자동 종료하지 않습니다. 실행 여부와 사용량만 확인합니다.");
        Console.ResetColor();

        var running = rows.Where(static x => x.Running).ToList();
        var notRunning = rows.Where(static x => !x.Running).Select(static x => x.Label).ToList();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" 실행 중");
        Console.ResetColor();
        if (running.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" (없음)");
            Console.ResetColor();
        }
        else
        {
            foreach (var o in running)
            {
                var mb = (int)(o.RamBytes / (1024 * 1024));
                var hint = OverlayHintForLabel(o.Label);
                Console.WriteLine(
                    $"- {o.Label,-14} CPU {o.CpuPercentApprox}%   RAM {mb} MB   {hint}");
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" 실행 안 함");
        Console.ResetColor();
        if (notRunning.Count == 0)
            Console.WriteLine(" (없음)");
        else
            Console.WriteLine("- " + string.Join(", ", notRunning));

        var totalRamGb = running.Sum(static o => o.RamBytes) / (1024.0 * 1024.0 * 1024.0);
        var totalCpu = running.Sum(static o => o.CpuPercentApprox);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            $" 요약: 실행 중 {running.Count}개 / 총 RAM {totalRamGb.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} GB / 총 CPU {totalCpu.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}%");
        Console.ResetColor();
        PrintBar('-');
    }

    private static string OverlayHintForLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return "";
        if (label.StartsWith("Discord", StringComparison.OrdinalIgnoreCase))
            return "음성/오버레이 사용 시 영향 가능";
        if (label.StartsWith("Steam", StringComparison.OrdinalIgnoreCase))
            return "Steam 오버레이 확인 권장";
        if (label.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0)
            return "게임 바·캡처가 켜져 있으면 확인";
        if (label.StartsWith("NVIDIA", StringComparison.OrdinalIgnoreCase))
            return "녹화/오버레이 확인 권장";
        if (label.StartsWith("OBS", StringComparison.OrdinalIgnoreCase))
            return "방송/녹화 시 부하 가능";
        if (label.StartsWith("Overwolf", StringComparison.OrdinalIgnoreCase))
            return "게임 오버레이 앱";
        if (label.StartsWith("Medal", StringComparison.OrdinalIgnoreCase))
            return "클립/녹화 앱";
        if (label.StartsWith("Razer", StringComparison.OrdinalIgnoreCase))
            return "장치 소프트웨어";
        if (label.StartsWith("Logitech", StringComparison.OrdinalIgnoreCase))
            return "장치 소프트웨어";
        if (label.StartsWith("SteelSeries", StringComparison.OrdinalIgnoreCase))
            return "장치 소프트웨어";
        return "";
    }

    public static void PrintMonitorRefreshCheck(DisplayRefreshInfo d)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 모니터 주사율 점검");
        Console.ResetColor();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" 기본 디스플레이 · 읽기 전용 · 자동 변경 없음");
        Console.ResetColor();
        WriteMonitorCurrentHzLine(d.CurrentHz);
        WriteMonitorCandidatesHzLine(d.CurrentHz, d.CandidateHz,
            d.CandidateHz.Count > 0 ? "" : "확인 불가");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("    - 판단        : " + d.DetailNote);
        Console.ResetColor();
        PrintBar('-');
    }
}
