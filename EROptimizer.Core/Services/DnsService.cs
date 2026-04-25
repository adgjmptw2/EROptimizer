using EROptimizer.Core.Models;

namespace EROptimizer.Core.Services;

public static class DnsService
{
    public static StepResult Run(string logsDirectory, string sessionId, ErLog log)
    {
        const string name = "DNS 정리";
        try
        {
            Directory.CreateDirectory(logsDirectory);
            var dumpPath = Path.GetFullPath(Path.Combine(logsDirectory, $"dns_before_{sessionId}.txt"));
            var cmdLine = "ipconfig /displaydns > \"" + dumpPath.Replace("\"", "\"\"") + "\"";
            var ec1 = ProcessRunner.RunCmdSingleLine(cmdLine);
            var size = File.Exists(dumpPath) ? new FileInfo(dumpPath).Length : 0;
            log.Info($"displaydns 저장: {dumpPath} (cmd exit {ec1}, {size} bytes)");

            var ec2 = ProcessRunner.RunNoCapture("ipconfig.exe", "/flushdns");
            log.Info($"flushdns 완료 (exit {ec2})");
            return new StepResult { Name = name, Success = true, Skipped = false, Message = $"덤프 OK, flush exit={ec2}" };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }
}
