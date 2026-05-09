using EROptimizer.Core;
using EROptimizer.Core.Backup;
using EROptimizer.Core.Diagnostics;
using EROptimizer.Core.Hardware;
using EROptimizer.Core.Models;
using EROptimizer.Core.Services;
using Newtonsoft.Json;

namespace EROptimizer.Cli;

internal static partial class Program
{
    private static bool EnsureGameExe(ref string? gameExe, GameDiscoveryResult discovery)
    {
        if (!string.IsNullOrEmpty(gameExe) && File.Exists(gameExe))
            return true;
        if (discovery.IsComplete)
        {
            gameExe = discovery.GameExePath;
            return true;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("실행 파일이 없습니다. [8]으로 먼저 지정하세요.");
        Console.ResetColor();
        return false;
    }

    private static void ResolveManualExe(ref string? gameExe)
    {
        Console.WriteLine();
        Console.WriteLine("1) 경로 붙여넣기  2) 파일 선택 창");
        var mode = MineConsoleUi.PromptLine("선택 : ")?.Trim();
        if (mode == "2")
            gameExe = ExePicker.PickExeInteractive();
        else
        {
            var p = MineConsoleUi.PromptLine("EternalReturn.exe 전체 경로 : ")?.Trim().Trim('"');
            gameExe = string.IsNullOrEmpty(p) ? null : p;
        }

        if (!string.IsNullOrEmpty(gameExe) && File.Exists(gameExe))
        {
            gameExe = Path.GetFullPath(gameExe);
            Console.Write("   - 실행 파일 확인 : ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(gameExe);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("   - 경로가 유효하지 않습니다.");
            Console.ResetColor();
            gameExe = null;
        }
    }

    private static bool RunFullPackage(string workspace, string exe, ErLog log)
    {
        var sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        log.Info($"패키지 세션 시작 — exe: {exe}, session={sessionId}");

        var hw = HardwareProbe.Probe();
        log.Info(hw.SummaryLine);

        var pre = SystemDiagnosisProbe.Collect(exe);
        MineConsoleUi.PrintSystemDiagnosis("현재 진단 결과", pre);
        PrintPackageSummary(exe, workspace, sessionId, hw);
        if (MineConsoleUi.PromptLine("적용하고 백업을 만듭니다. 계속? (Y/N) : ")?.Trim().ToUpperInvariant() != "Y")
        {
            log.Info("사용자 취소(패키지)");
            return false;
        }

        log.Info("boot.config: 패키지에 포함하여 병합");

        var backup = new BackupSession(workspace, sessionId);
        log.Info($"백업: {backup.SessionPath}");

        var results = new List<StepResult>
        {
            DnsService.Run(Path.Combine(workspace, "logs"), sessionId, log),
            GameBarService.Apply(backup, log),
            GpuPreferenceService.Apply(exe, backup, log),
            PowerPlanService.Apply(backup, log),
            TempCleanupService.Run(workspace, sessionId, log),
            NvidiaSafeProfileService.Export(exe, backup, hw, log),
            BootConfigService.Apply(exe, backup, log)
        };

        PrintResultsTable(results);
        SaveSummary(backup.SessionPath, sessionId, results);
        log.Info($"패키지 작업 완료 session={sessionId}");

        var post = SystemDiagnosisProbe.Collect(exe);
        MineConsoleUi.PrintSystemDiagnosis("적용 후 재진단 (변경·수동 점검)", post);

        Console.WriteLine();
        MineConsoleUi.PrintAdditionalDiagnosticsOffer(workspace);
        if (MineConsoleUi.PromptLine("추가 진단을 진행하시겠습니까? (Y/N) : ")?.Trim().ToUpperInvariant() == "Y")
        {
            var diagSid = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var rep = PostApplyDiagnosticService.Run(exe, sessionId,
                (msg, pct) => MineConsoleUi.WriteDiagnosticProgress(msg, pct));
            Console.WriteLine();
            MineConsoleUi.ClearDiagnosticProgressLine();
            MineConsoleUi.PrintPostApplyDiagnosticSummary(rep);
            var logsDir = Path.Combine(workspace, "logs");
            DiagnosticResultWriter.Save(logsDir, diagSid, rep);
            log.Info($"추가 진단 저장: diagnostic_{diagSid}.log, diagnostic_result_{diagSid}.json");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" 저장: {MineConsoleUi.Shorten(Path.Combine(logsDir, $"diagnostic_{diagSid}.log"), 68)}");
            Console.WriteLine($" JSON: {MineConsoleUi.Shorten(Path.Combine(logsDir, $"diagnostic_result_{diagSid}.json"), 68)}");
            Console.ResetColor();
        }

        return true;
    }

    private static void RunBootConfigOnly(string workspace, string exe, ErLog log)
    {
        Console.WriteLine();
        if (MineConsoleUi.PromptLine("boot.config만 권장 블록으로 병합합니다 (현재 파일은 세션 백업). 계속? (Y/N) : ")?.Trim().ToUpperInvariant() != "Y")
        {
            log.Info("사용자 취소(boot만)");
            return;
        }

        var sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        log.Info($"boot 전용 세션 — exe: {exe}, session={sessionId}");
        var backup = new BackupSession(workspace, sessionId);
        var results = new List<StepResult> { BootConfigService.Apply(exe, backup, log) };
        PrintResultsTable(results);
        SaveSummary(backup.SessionPath, sessionId, results);
        log.Info($"boot 전용 완료 session={sessionId}");
    }

    private static void RunFullRestoreFromBackup(string workspace, string exe, ErLog log)
    {
        var sessionPath = PromptPickBackupSession(workspace);
        if (sessionPath == null)
            return;

        Console.WriteLine();
        if (MineConsoleUi.PromptLine("선택한 세션의 레지·전원·boot.config를 게임/시스템에 되돌립니다. 계속? (Y/N) : ")?.Trim().ToUpperInvariant() != "Y")
        {
            log.Info("사용자 취소(백업 재적용)");
            return;
        }

        log.Info($"백업 재적용: {sessionPath}");
        var results = new List<StepResult>
        {
            BackupRestoreService.RestoreRegistryFromSession(sessionPath, log),
            BackupRestoreService.RestorePowerPlanFromSession(sessionPath, log)
        };

        var boots = BackupRestoreService.FindBootBackupsInSession(sessionPath);
        if (boots.Count == 0)
            results.Add(new StepResult { Name = "boot.config", Success = true, Skipped = true, Message = "세션에 boot 백업 없음" });
        else
            results.Add(BackupRestoreService.RestoreBootFile(exe, boots[0], log));

        PrintResultsTable(results);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("(요약본은 새 세션을 만들지 않았습니다. 로그에만 기록됩니다.)");
        Console.ResetColor();
        log.Info("백업 재적용 완료");
    }

    private static void RunBootRestoreFromBackup(string workspace, string exe, ErLog log)
    {
        var sessionPath = PromptPickBackupSession(workspace);
        if (sessionPath == null)
            return;

        var boots = BackupRestoreService.FindBootBackupsInSession(sessionPath);
        if (boots.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("해당 세션에 boot.config.bak_* 파일이 없습니다.");
            Console.ResetColor();
            return;
        }

        string bakPath;
        if (boots.Count == 1)
            bakPath = boots[0];
        else
        {
            Console.WriteLine();
            for (var i = 0; i < boots.Count; i++)
                Console.WriteLine($"[{i + 1}] {Path.GetFileName(boots[i])}");
            var line = MineConsoleUi.PromptLine("복원할 백업 번호 : ")?.Trim();
            if (!int.TryParse(line, out var n) || n < 1 || n > boots.Count)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("취소되었습니다.");
                Console.ResetColor();
                return;
            }
            bakPath = boots[n - 1];
        }

        Console.WriteLine();
        if (MineConsoleUi.PromptLine($"{Path.GetFileName(bakPath)} → 게임 boot.config 로 덮어씁니다. 계속? (Y/N) : ")?.Trim().ToUpperInvariant() != "Y")
        {
            log.Info("사용자 취소(boot 복원)");
            return;
        }

        var r = BackupRestoreService.RestoreBootFile(exe, bakPath, log);
        PrintResultsTable(new List<StepResult> { r });
        log.Info("boot.config 백업 불러오기 완료");
    }

    private static string? PromptPickBackupSession(string workspace)
    {
        var sessions = BackupRestoreService.ListSessionPathsDescending(workspace);
        if (sessions.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("EROptimizer_Backup 폴더에 세션이 없습니다.");
            Console.ResetColor();
            return null;
        }

        Console.WriteLine();
        for (var i = 0; i < sessions.Count; i++)
            Console.WriteLine($"[{i + 1}] {Path.GetFileName(sessions[i])}");

        var line = MineConsoleUi.PromptLine("세션 번호 (Enter=취소) : ")?.Trim();
        if (!int.TryParse(line, out var idx) || idx < 1 || idx > sessions.Count)
            return null;
        return sessions[idx - 1];
    }

    private static void PrintPackageSummary(string exe, string workspace, string sessionId, HardwareSnapshot hw)
    {
        var boot = BootConfigService.GetBootConfigPath(exe);
        var backupDir = Path.Combine(workspace, "EROptimizer_Backup", sessionId);
        var logPath = Path.Combine(workspace, "logs", $"optimizer_{sessionId}.log");

        Console.WriteLine();
        MineConsoleUi.PrintBar('=');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(" 기본 패키지 적용 요약");
        Console.ResetColor();
        MineConsoleUi.PrintBar('=');
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($" 게임 exe        : {MineConsoleUi.Shorten(exe, 72)}");
        Console.WriteLine($" CPU / GPU       : {MineConsoleUi.Shorten(hw.SummaryLine, 76)}");
        Console.WriteLine($" boot.config     : {MineConsoleUi.Shorten(boot, 72)}");
        Console.WriteLine(" 1. DNS          : displaydns → 로그 파일, 이어서 flush");
        Console.WriteLine(" 2. 게임바/DVR   : 레지 비활성화, 이전 값 비활성화");
        Console.WriteLine(" 3. GPU          : UserGpuPreferences = 2; (Windows 고성능)");
        Console.WriteLine(" 4. 전원         : 고성능 있으면 전환 / 없으면 스킵");
        Console.WriteLine(" 5. TEMP         : %TEMP% 직속 파일만 삭제, 폴더는 스킵");
        Console.WriteLine(" 6. NV           : 지포스 있을 때만 JSON 뽑음 → 백업/files에만 저장 (드라이버는 미적용)");
        Console.WriteLine("12. boot.config  : 옵션 블록 합치기 + .bak, job-worker는 CPU 기준");
        Console.WriteLine($" 백업 폴더       : {MineConsoleUi.Shorten(backupDir, 72)}");
        Console.WriteLine($" 로그            : {MineConsoleUi.Shorten(logPath, 72)}");
        Console.ResetColor();
        MineConsoleUi.PrintBar('=');
    }

    private static void PrintResultsTable(List<StepResult> results)
    {
        Console.WriteLine();
        foreach (var r in results)
        {
            var mark = r.Success ? (r.Skipped ? "- " : "+ ") : "x ";
            var msg = MineConsoleUi.Shorten(r.Message, 44);
            if (r.Success && !r.Skipped) Console.ForegroundColor = ConsoleColor.Cyan;
            else if (r.Success && r.Skipped) Console.ForegroundColor = ConsoleColor.DarkCyan;
            else Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(mark);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{r.Name,-12} {msg}");
            Console.ResetColor();
        }
    }

    private static void SaveSummary(string backupPath, string sessionId, List<StepResult> results)
    {
        var obj = new
        {
            Timestamp = DateTimeOffset.Now.ToString("o"),
            SessionId = sessionId,
            Results = results.Select(r => new { r.Name, r.Success, r.Skipped, r.Message }).ToList()
        };
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        var path = Path.Combine(backupPath, "summary.json");
        File.WriteAllText(path, json);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"요약본 저장: {MineConsoleUi.Shorten(path, 68)}");
        Console.ResetColor();
    }

    private static void WaitExit()
    {
        Console.WriteLine();
        MineConsoleUi.PromptLine("종료하려면 Enter… ");
    }
}
