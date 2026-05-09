using System.Text.RegularExpressions;
using EROptimizer.Core.Backup;
using EROptimizer.Core.Models;

namespace EROptimizer.Core.Services;

public static class PowerPlanService
{
    private static readonly Regex GuidRegex = new(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}", RegexOptions.Compiled);

    public static StepResult Apply(BackupSession backup, ErLog log)
    {
        const string name = "전원 고성능";

        try
        {
            var listOut = ProcessRunner.Run("powercfg.exe", "/list").StdOut;
            log.Info("powercfg /list:\n" + listOut);

            var active = ProcessRunner.Run("powercfg.exe", "/getactivescheme").StdOut;
            var activeGuid = GuidRegex.Matches(active).Cast<Match>().FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(activeGuid))
            {
                backup.WritePowerPlanBackup(activeGuid!);
                log.Info($"활성 전원 GUID 백업: {activeGuid}");
            }

            var hp = ErConstants.HighPerformancePowerSchemeGuid;
            if (listOut.IndexOf(hp, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var (ec, _, err) = ProcessRunner.Run("powercfg.exe", $"/setactive {hp}");
                log.Info($"powercfg /setactive {hp} exit={ec} {err}");
                return new StepResult { Name = name, Success = ec == 0, Skipped = false, Message = "고성능 적용" };
            }

            log.Info("고성능 전원 계획이 목록에 없음 — duplicatescheme 생략(스킵)");
            return new StepResult { Name = name, Success = true, Skipped = true, Message = "고성능 없음·생략" };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }
}
