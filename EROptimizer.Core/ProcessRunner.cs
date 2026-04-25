using System.Diagnostics;
using System.Text;

namespace EROptimizer.Core;

internal static class ProcessRunner
{
    public static (int ExitCode, string StdOut, string StdErr) Run(string fileName, string arguments, int timeoutMs = 120_000)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");

        var outBuf = new StringBuilder();
        var errBuf = new StringBuilder();
        var outDone = new TaskCompletionSource<bool>();
        var errDone = new TaskCompletionSource<bool>();

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) outDone.TrySetResult(true);
            else outBuf.AppendLine(e.Data);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) errDone.TrySetResult(true);
            else errBuf.AppendLine(e.Data);
        };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(); } catch { /* ignore */ }
            throw new TimeoutException(fileName);
        }

        Task.WaitAll(new[] { outDone.Task, errDone.Task }, TimeSpan.FromSeconds(60));
        return (p.ExitCode, outBuf.ToString(), errBuf.ToString());
    }

    public static int RunCmdSingleLine(string commandAfterSlashC, int timeoutMs = 300_000)
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c " + commandAfterSlashC)
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(); } catch { /* ignore */ }
            throw new TimeoutException("cmd.exe");
        }
        return p.ExitCode;
    }

    public static int RunNoCapture(string fileName, string arguments, int timeoutMs = 60_000)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(); } catch { /* ignore */ }
            throw new TimeoutException(fileName);
        }
        return p.ExitCode;
    }
}
