namespace EROptimizer.Core.Diagnostics;

public static class NvidiaSmiDriverVersion
{
    private static readonly string[] CandidateExes =
    [
        "nvidia-smi",
        @"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe"
    ];

    public static string? TryQueryDriverVersion()
    {
        foreach (var exe in CandidateExes)
        {
            try
            {
                var (ec, stdout, _) = ProcessRunner.Run(exe, "--query-gpu=driver_version --format=csv,noheader", 8_000);
                if (ec != 0 || string.IsNullOrWhiteSpace(stdout))
                    continue;
                var line = stdout.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(line))
                    return line.Trim();
            }
            catch
            {
                /* */
            }
        }
        return null;
    }
}
