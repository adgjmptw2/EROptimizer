using EROptimizer.Core.Models;

namespace EROptimizer.Core.Services;

public static class TempCleanupService
{
    public static StepResult Run(string workspaceRoot, string sessionId, ErLog log)
    {
        _ = workspaceRoot;
        _ = sessionId;
        const string name = "TEMP 정리";
        try
        {
            var temp = Path.GetTempPath();
            var top = new DirectoryInfo(temp).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).ToList();

            long freed = 0;
            var ok = 0;
            var skipped = 0;
            foreach (var item in top)
            {
                try
                {
                    if (item is FileInfo fi)
                    {
                        var len = fi.Length;
                        File.Delete(fi.FullName);
                        freed += len;
                        ok++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch
                {
                    skipped++;
                }
            }

            log.Info($"TEMP 삭제 성공 {ok}개, 건너뜀 {skipped}개, 확보 약 {freed} bytes");
            return new StepResult { Name = name, Success = true, Skipped = false, Message = $"삭제 {ok}, 건너뜀 {skipped}" };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }
}
