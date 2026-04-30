using System.Threading.Tasks;
using EROptimizer.Core;
using EROptimizer.Core.Diagnostics;
using EROptimizer.Core.StorageCleanup;

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

    private static readonly string[] WaitSpinnerFrames =
    [
        "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"
    ];

    public static T RunWithWait<T>(string statusLine, Func<T> work)
    {
        var task = Task.Run(work);
        var i = 0;
        while (!task.IsCompleted)
        {
            var frame = WaitSpinnerFrames[i++ % WaitSpinnerFrames.Length];
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            var line = $"  {frame}  {statusLine}";
            Console.Write("\r" + line);
            var pad = Math.Max(0, 96 - line.Length);
            Console.Write(new string(' ', pad));
            Console.ResetColor();
            task.Wait(90);
        }

        ClearWaitLine();
        return task.GetAwaiter().GetResult();
    }

    public static void RunWithWait(string statusLine, Action work) =>
        RunWithWait<object?>(statusLine, () =>
        {
            work();
            return null;
        });

    private static void ClearWaitLine()
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

    private static string MenuKey(int n) => $"[{n}]".PadRight(5);

    private static void WriteMenuRow(int index, string primary, string? dimSecondary = null)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(MenuKey(index));
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(primary);
        if (!string.IsNullOrEmpty(dimSecondary))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  —  " + dimSecondary);
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    private static void WriteSectionLabel(string title)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(" " + title);
        Console.ResetColor();
    }

    public static void WriteNumberedRow(int n, string label, string detail, ConsoleColor detailColor = ConsoleColor.DarkGray)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"    [{n}]  ");
        Console.Write($"{label,-24}");
        Console.ForegroundColor = detailColor;
        Console.WriteLine(detail);
        Console.ResetColor();
    }

    public static void PrintMutedRunningLine(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  ▸  " + message);
        Console.ResetColor();
    }

    public static void WriteProgressPercent(int percent, string? status = null)
    {
        if (percent < 0) percent = 0;
        else if (percent > 100) percent = 100;
        const int barChars = 28;
        var filled = (int)Math.Round(barChars * (percent / 100.0));
        if (filled > barChars) filled = barChars;
        var bar = new string('█', filled) + new string('░', barChars - filled);
        var tail = string.IsNullOrEmpty(status) ? "" : "  " + status;
        var line = $"  [{bar}]  {percent,3}%{tail}";
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("\r" + line);
        try
        {
            var w = Console.WindowWidth;
            if (w > 1 && line.Length < w - 1)
                Console.Write(new string(' ', Math.Min(48, w - 1 - line.Length)));
        }
        catch
        {
            if (line.Length < 96)
                Console.Write(new string(' ', Math.Min(48, 96 - line.Length)));
        }

        Console.ResetColor();
    }

    public static void ClearProgressLine() => ClearWaitLine();

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
        _ = stepDelayMs;
        Console.WriteLine();
        PrintThinBar();
        WriteSectionLabel("설치 경로 탐색 결과");
        WriteNumberedRow(1, "스팀 설치 경로",
            string.IsNullOrEmpty(d.SteamRoot)
                ? "(레지스트리에서 찾지 못함)"
                : Shorten(d.SteamRoot ?? "", PathMaxConsole),
            string.IsNullOrEmpty(d.SteamRoot) ? ConsoleColor.DarkYellow : ConsoleColor.Green);
        WriteNumberedRow(2, "게임 설치 폴더",
            string.IsNullOrEmpty(d.InstallDirectory)
                ? "(appmanifest 또는 폴더 없음)"
                : Shorten(d.InstallDirectory ?? "", PathMaxConsole),
            string.IsNullOrEmpty(d.InstallDirectory) ? ConsoleColor.DarkYellow : ConsoleColor.Green);
        var exeOk = d.IsComplete && !string.IsNullOrEmpty(d.GameExePath);
        WriteNumberedRow(3, "실행 파일",
            exeOk ? Shorten(d.GameExePath!, PathMaxConsole) : "(실패 — 메뉴 [7]에서 수동 지정)",
            exeOk ? ConsoleColor.Green : ConsoleColor.DarkYellow);
        Console.WriteLine();
    }

    public static void PrintMainMenu()
    {
        PrintThinBar();
        WriteMenuRow(1, "기본 패키지 적용", "DNS, 게임바, GPU, 전원, TEMP, NV JSON, boot.config");
        WriteMenuRow(2, "PC 저장소 청소", "저장소 분석 · 임시 · 캐시");
        WriteMenuRow(3, "boot.config만 패치");
        WriteMenuRow(4, "백업 버전 재적용", "레지 · 전원 · boot.config");
        WriteMenuRow(5, "boot.config만 백업 불러오기");
        WriteMenuRow(6, "설치 경로 다시 스캔");
        WriteMenuRow(7, "실행 파일 수동 지정", "경로 입력 또는 파일 선택");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("[Q] ");
        Console.WriteLine("종료");
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
                Console.Write($"    [{i + 1}]  ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(Shorten(a.Name, 74));
                var ver = string.IsNullOrEmpty(a.DriverVersion) ? "?" : a.DriverVersion;
                var tail = string.IsNullOrEmpty(a.DriverDate) ? "" : "  (" + a.DriverDate + ")";
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"              드라이버  {ver}{tail}");
                Console.ResetColor();
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
                    Console.WriteLine(
                        "              GFE 조회: " + v + gfeTail + " — 설치(" + nv.DriverVersion + ")보다 낮음 (조회·카드 ID 불일치 가능). 최신 GRD는 nvidia.com/drivers");
                }
                else
                    Console.WriteLine("              GFE 최신: " + v + gfeTail);
            }
            else
                Console.WriteLine("              GFE 최신: (조회 실패 — nvidia.com 또는 GeForce Experience)");
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

    public static void PrintStorageCleanupMenu()
    {
        Console.WriteLine();
        PrintBar('=');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(CenterPad("PC 저장소 청소", BarWidth));
        Console.ResetColor();
        PrintBar('=');
        PrintThinBar();
        WriteMenuRow(1, "저장소 용량 분석");
        WriteMenuRow(2, "저장소 정리 미리보기", "한 줄 요약 + cleanup_preview JSON");
        WriteMenuRow(3, "선택 항목 정리 실행");
        WriteMenuRow(4, "정리 결과 보기");
        WriteMenuRow(5, "로그 폴더 열기");
        WriteMenuRow(6, "뒤로", "메인 메뉴");
    }

    public static void PrintStorageAnalysis(StorageAnalysisReport a)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 저장소 분석");
        Console.ResetColor();
        PrintBar('-');

        var n = 1;
        WriteSectionLabel("로컬 디스크");
        foreach (var drive in a.Drives)
        {
            var total = ByteSizeFormat.Format(drive.TotalBytes);
            var free = ByteSizeFormat.Format(drive.FreeBytes);
            var used = ByteSizeFormat.Format(Math.Max(0, drive.TotalBytes - drive.FreeBytes));
            WriteNumberedRow(n++, drive.Name.Trim(), $"전체 {total}  ·  사용 약 {used}  ·  여유 {free}", ConsoleColor.DarkGray);
        }

        if (a.DriveCLowSpace && !string.IsNullOrEmpty(a.DriveCLowSpaceNote))
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   ※  " + a.DriveCLowSpaceNote);
            Console.ResetColor();
        }

        WriteSectionLabel("임시 · 캐시 · 보고 (용량 참고)");
        WriteNumberedRow(n++, "사용자 TEMP",
            $"약 {ByteSizeFormat.Format(a.UserTempBytes)}  ·  {Shorten(a.UserTempPath, 44)}", ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "Windows TEMP",
            $"약 {ByteSizeFormat.Format(a.WindowsTempBytes)}  ·  {Shorten(a.WindowsTempPath, 44)}", ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "휴지통 (C:)",
            a.RecycleBinKnown
                ? $"약 {ByteSizeFormat.Format(a.RecycleBinEstimatedBytes)}"
                : "조회 실패 — 생략",
            ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "업데이트 잔여·덤프",
            a.WindowsOldExists
                ? $"약 {ByteSizeFormat.Format(a.WindowsOldBytes)}  ·  {Shorten(a.WindowsOldPath, 36)} (Windows.old)"
                : "없음",
            ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "C:\\NVIDIA",
            !a.NvidiaRootExists
                ? "없음"
                : a.NvidiaRootIsReparse
                    ? "재분석 지점 — 정리 제외"
                    : $"약 {ByteSizeFormat.Format(a.NvidiaRootBytes)}",
            ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "C:\\AMD",
            !a.AmdRootExists
                ? "없음"
                : a.AmdRootIsReparse
                    ? "재분석 지점 — 정리 제외"
                    : $"약 {ByteSizeFormat.Format(a.AmdRootBytes)}",
            ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "배달 최적화 캐시",
            $"약 {ByteSizeFormat.Format(a.DeliveryOptimizationCacheBytes)}  ·  {Shorten(a.DeliveryOptimizationCachePath, 36)}",
            ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "DirectX D3DSCache",
            $"약 {ByteSizeFormat.Format(a.DirectXD3DCacheBytes)}  ·  {Shorten(a.DirectXD3DCachePath, 36)}", ConsoleColor.DarkGray);
        WriteNumberedRow(n++, "WER · 로컬 덤프",
            $"약 {ByteSizeFormat.Format(a.WerQueuedBytes + a.WerArchiveBytes + a.LocalCrashDumpBytes)}",
            ConsoleColor.DarkGray);

        PrintBar('-');
    }

    public static void PrintCleanupPreviewTable(IReadOnlyList<CleanupPreviewRow> rows)
    {
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 정리 미리보기 (삭제 전 확인)");
        Console.ResetColor();
        PrintBar('-');

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var bucket = r.Id == StorageCleanupIds.WindowsOldInfo ? "[선택]" : "[권장]";
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"    {bucket}  ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{i + 1}]  ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{r.Title}  ·  용량 {ByteSizeFormat.Format(r.EstimatedBytes)}");
            Console.ResetColor();
        }

        PrintBar('-');
    }

    public static void PrintCleanupConfirmSummary(IReadOnlyList<string> chosenIds,
        IReadOnlyList<CleanupPreviewRow> preview, long estimatedBytes)
    {
        var width = Math.Min(Math.Max(BarWidth - 8, 32), 56);
        var dash = new string('·', width);

        static string ShortTitle(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s[..(max - 1)] + "…";
        }

        var labels = string.Join(" · ",
            chosenIds.Select(id =>
            {
                var row = preview.FirstOrDefault(x => x.Id == id);
                return row != null ? ShortTitle(row.Title, 52) : id;
            }));

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ·  {dash}");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("  │  실행 전 확인");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  │    ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("선택 항목   ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(labels);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  │    ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("예상 확보   ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            $"약 {ByteSizeFormat.Format(estimatedBytes)}  —  참고치이며 실제와 다를 수 있습니다.");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  │    ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("참고        ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("삭제형 정리는 되돌리기 어렵습니다. 미리보기 JSON과 로그만 남습니다.");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ·  {dash}");
        Console.ResetColor();
    }

    public static void PrintCleanupExecutionReport(CleanupExecutionReport? r)
    {
        if (r == null) return;
        Console.WriteLine();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 정리 실행 결과");
        Console.ResetColor();
        PrintBar('-');
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            $"  합계 확보(참고) {ByteSizeFormat.Format(r.TotalBytesReclaimed)}  ·  UTC {r.StartedUtc:HH:mm:ss} → {r.FinishedUtc:HH:mm:ss}");
        Console.ResetColor();
        var itemNo = 1;
        foreach (var it in r.Items)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"    [{itemNo++}]  ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(it.Title);
            Console.ResetColor();
            if (!it.Ran)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("              (실행 안 함)");
                Console.ResetColor();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(
                $"              확보 {ByteSizeFormat.Format(it.BytesReclaimed)}  ·  파일 {it.FilesDeleted}  ·  폴더 {it.DirectoriesRemoved}  ·  건너뜀 {it.Skipped}");
            if (it.Failures.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                foreach (var (label, cnt) in CleanupFailureLog.SummarizeFailures(it.Failures))
                    Console.WriteLine($"              삭제 실패 : {label} [{cnt}건] - SKIP.");
                Console.ResetColor();
            }
        }

        PrintBar('-');
    }
}
