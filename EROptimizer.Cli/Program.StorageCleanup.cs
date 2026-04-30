using System.Diagnostics;
using EROptimizer.Core;
using EROptimizer.Core.StorageCleanup;

namespace EROptimizer.Cli;

internal static partial class Program
{
    private static StorageAnalysisReport? _storageLastAnalysis;
    private static List<CleanupPreviewRow>? _storageLastPreview;
    private static CleanupExecutionReport? _storageLastExecution;
    private static IReadOnlyList<string>? _storageLastSelectedIds;

    private static void RunStorageCleanupMenu(string workspace, GameDiscoveryResult discovery, ErLog log, string appSession)
    {
        var protection = CleanupProtectionContext.FromDiscovery(discovery);

        while (true)
        {
            MineConsoleUi.PrintStorageCleanupMenu();
            MineConsoleUi.PrintThinBar();
            var sel = MineConsoleUi.PromptLine("번호 : ")?.Trim();

            switch (sel)
            {
                case "1":
                    _storageLastAnalysis = MineConsoleUi.RunWithWait(
                        "저장소 용량 계산 중",
                        () => StorageCleanupScanner.ScanStorageCleanupTargets(protection));
                    MineConsoleUi.PrintStorageAnalysis(_storageLastAnalysis);
                    log.Info("저장소 분석 완료");
                    break;
                case "2":
                    EnsurePreview(workspace, protection, log, appSession);
                    break;
                case "3":
                    RunCleanupExecute(workspace, protection, log, appSession);
                    break;
                case "4":
                    if (_storageLastExecution != null)
                        MineConsoleUi.PrintCleanupExecutionReport(_storageLastExecution);
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("아직 정리 실행 결과가 없습니다. [3]으로 실행하세요.");
                        Console.ResetColor();
                    }

                    break;
                case "5":
                    OpenLogsFolder(workspace);
                    break;
                case "6":
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("1~6 을 입력하세요.");
                    Console.ResetColor();
                    break;
            }
        }
    }

    private static void EnsurePreview(string workspace, CleanupProtectionContext protection, ErLog log, string appSession)
    {
        if (_storageLastAnalysis == null)
            _storageLastAnalysis = MineConsoleUi.RunWithWait(
                "저장소 용량 계산 중",
                () => StorageCleanupScanner.ScanStorageCleanupTargets(protection));

        _storageLastPreview = StorageCleanupScanner.BuildCleanupPreview(_storageLastAnalysis);
        MineConsoleUi.PrintCleanupPreviewTable(_storageLastPreview);

        var previewPath = Path.Combine(workspace, "logs", $"cleanup_preview_{appSession}.json");
        var defaultIds = _storageLastPreview
            .Where(r => r.CanExecute && r.SelectedByDefault)
            .Select(r => r.Id)
            .ToList();
        StorageCleanupJson.SaveCleanupPreviewJson(
            previewPath,
            appSession,
            _storageLastAnalysis,
            _storageLastPreview,
            defaultIds);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"미리보기를 저장했습니다: {previewPath}");
        Console.ResetColor();
        log.Info($"cleanup_preview 저장: {previewPath}");
    }

    private static void RunCleanupExecute(string workspace, CleanupProtectionContext protection, ErLog log, string appSession)
    {
        if (_storageLastPreview == null || _storageLastAnalysis == null)
        {
            EnsurePreview(workspace, protection, log, appSession);
        }

        var preview = _storageLastPreview!;
        var executable = preview.Where(p => p.CanExecute).ToList();
        if (executable.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("실행 가능한 정리 항목이 없습니다.");
            Console.ResetColor();
            return;
        }

        MineConsoleUi.PrintCleanupPreviewTable(preview);

        var defaultIds = executable
            .Where(r => r.SelectedByDefault)
            .Select(r => r.Id)
            .ToList();
        Console.WriteLine();
        Console.WriteLine(
            "기본 선택(초기 체크된 항목)으로 실행하려면 Y, 번호를 직접 고르려면 N");
        var mode = MineConsoleUi.PromptLine("Y/N : ")?.Trim().ToUpperInvariant();
        List<string> chosen;
        if (mode == "N")
        {
            Console.WriteLine("항목 번호는 미리보기 표 위에서 확인합니다. 쉼표로 구분 (예: 1,2,4)");
            var line = MineConsoleUi.PromptLine("번호 : ")?.Trim();
            chosen = ParseIndices(line, preview);
            if (chosen.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("선택이 비어 있어 취소했습니다.");
                Console.ResetColor();
                return;
            }
        }
        else
        {
            chosen = defaultIds;
            if (chosen.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("기본 선택 항목이 없습니다. N으로 번호를 지정하세요.");
                Console.ResetColor();
                return;
            }
        }

        var est = chosen
            .Select(id => preview.FirstOrDefault(r => r.Id == id))
            .Where(r => r != null)
            .Sum(r => r!.EstimatedBytes);

        MineConsoleUi.PrintCleanupConfirmSummary(chosen, preview, est);
        if (MineConsoleUi.PromptLine("정말 실행할까요? (Y/N) : ")?.Trim().ToUpperInvariant() != "Y")
        {
            log.Info("사용자가 정리 실행을 취소함");
            return;
        }

        Console.WriteLine();
        var progress = new Progress<int>(p => MineConsoleUi.WriteProgressPercent(p, "선택 항목 정리 중"));

        _storageLastSelectedIds = chosen;
        var previewPath = Path.Combine(workspace, "logs", $"cleanup_preview_{appSession}.json");
        StorageCleanupJson.SaveCleanupPreviewJson(
            previewPath,
            appSession,
            _storageLastAnalysis!,
            preview,
            chosen);

        var report = StorageCleanupExecutor.ExecuteCleanupPlan(chosen, protection, log, progress);
        MineConsoleUi.ClearProgressLine();
        _storageLastExecution = report;

        var resultPath = Path.Combine(workspace, "logs", $"cleanup_result_{appSession}.json");
        StorageCleanupJson.SaveCleanupResultJson(resultPath, appSession, report);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"결과 저장: {resultPath}");
        Console.ResetColor();
        log.Info($"cleanup_result 저장: {resultPath}");

        MineConsoleUi.PrintCleanupExecutionReport(report);
    }

    private static List<string> ParseIndices(string? line, IReadOnlyList<CleanupPreviewRow> rows)
    {
        var result = new List<string>();
        if (line == null || string.IsNullOrWhiteSpace(line))
            return result;

        foreach (var part in line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (!int.TryParse(t, out var idx) || idx < 1 || idx > rows.Count) continue;
            var row = rows[idx - 1];
            if (!row.CanExecute) continue;
            if (!result.Contains(row.Id)) result.Add(row.Id);
        }

        return result;
    }

    private static void OpenLogsFolder(string workspace)
    {
        var logs = Path.Combine(workspace, "logs");
        Directory.CreateDirectory(logs);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = logs,
            UseShellExecute = true
        });
    }
}
