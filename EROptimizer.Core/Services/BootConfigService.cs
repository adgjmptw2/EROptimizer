using EROptimizer.Core.Backup;
using EROptimizer.Core.Models;

namespace EROptimizer.Core.Services;

public static class BootConfigService
{
    public static string GetBootConfigPath(string gameExePath)
    {
        var gameDir = Path.GetDirectoryName(gameExePath)!;
        return Path.Combine(gameDir, ErConstants.GameDataFolder, ErConstants.BootConfigFileName);
    }

    public static StepResult Apply(string gameExePath, BackupSession backup, ErLog log)
    {
        const string name = "boot.config";
        try
        {
            var path = GetBootConfigPath(gameExePath);
            if (!File.Exists(path))
                return new StepResult { Name = name, Success = false, Skipped = true, Message = "파일 없음" };

            var ts = backup.SessionId;
            var bakName = $"boot.config.bak_{ts}";
            backup.BackupFile(path, bakName);
            log.Info($"boot.config 백업: {Path.Combine(backup.FilesPath, bakName)}");

            var raw = File.ReadAllText(path);
            var blockWithJw = BootConfigPackage.BuildBlockText();
            log.Info($"boot.config job-worker-count={BootConfigPackage.RecommendedJobWorkerCount()} (논리 프로세서={Environment.ProcessorCount})");
            var (ok, msg, merged) = BootConfigMerger.Merge(raw, blockWithJw);
            if (!ok || merged == null)
                return new StepResult { Name = name, Success = false, Skipped = false, Message = msg };

            File.WriteAllText(path, merged, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            log.Info($"boot.config 저장: {path} (무결성 검사 시 원복 가능 — 백업 .bak_* 참고)");
            return new StepResult { Name = name, Success = true, Skipped = false, Message = msg };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }
}
