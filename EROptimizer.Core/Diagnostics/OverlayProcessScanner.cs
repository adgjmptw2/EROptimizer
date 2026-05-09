using System.Diagnostics;

namespace EROptimizer.Core.Diagnostics;

public sealed class OverlayAppSnapshot
{
    public required string Label { get; init; }
    public bool Running { get; init; }
    public double CpuPercentApprox { get; init; }
    public long RamBytes { get; init; }
}

public static class OverlayProcessScanner
{
    private static readonly (string Label, string[] ProcessNames)[] Groups =
    [
        ("Discord", ["Discord"]),
        ("Steam", ["steam", "steamwebhelper", "GameOverlayUI"]),
        ("Xbox Game Bar", ["GameBar", "GameBarFTServer", "XboxGameBarWidgets"]),
        ("NVIDIA", ["NVIDIA Share", "NVIDIA Overlay", "nvcontainer", "NVIDIA App"]),
        ("OBS", ["obs64", "obs32"]),
        ("Overwolf", ["Overwolf"]),
        ("Medal", ["Medal"]),
        ("Razer", ["RazerApp", "RazerCentral", "Razer Synapse", "Razer Synapse 3"]),
        ("Logitech", ["lghub", "lghub_agent"]),
        ("SteelSeries", ["SteelSeriesGG"])
    ];

    public static IReadOnlyList<OverlayAppSnapshot> Scan(int sampleMs = 1000)
    {
        sampleMs = sampleMs < 200 ? 200 : sampleMs > 3000 ? 3000 : sampleMs;
        var targets = BuildTargetSet();
        var t0 = SampleCpu(targets);
        Thread.Sleep(sampleMs);
        var t1 = SampleCpu(targets);

        var procs = EnumerateMatchingProcesses(targets);
        var ramByLabel = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in Groups)
            ramByLabel[g.Label] = 0;

        foreach (var p in procs)
        {
            try
            {
                var label = ResolveLabel(p.ProcessName, targets);
                if (label == null)
                    continue;
                if (ramByLabel.TryGetValue(label, out var prev))
                    ramByLabel[label] = prev + p.WorkingSet64;
                else
                    ramByLabel[label] = p.WorkingSet64;
            }
            catch
            {
                /* */
            }
            finally
            {
                try { p.Dispose(); } catch { /* */ }
            }
        }

        var list = new List<OverlayAppSnapshot>();
        foreach (var g in Groups)
        {
            var cpu = 0.0;
            foreach (var name in g.ProcessNames)
            {
                var key = name.ToLowerInvariant();
                if (!t0.TryGetValue(key, out var a))
                    continue;
                if (!t1.TryGetValue(key, out var b))
                    continue;
                var deltaMs = (b - a).TotalMilliseconds;
                if (deltaMs < 0)
                    deltaMs = 0;
                var cores = Environment.ProcessorCount;
                if (cores < 1)
                    cores = 1;
                cpu += deltaMs / (cores * sampleMs) * 100.0;
            }

            var ram = ramByLabel.TryGetValue(g.Label, out var rb) ? rb : 0;
            list.Add(new OverlayAppSnapshot
            {
                Label = g.Label,
                Running = ram > 0 || cpu > 0.05,
                CpuPercentApprox = Math.Round(cpu, 1),
                RamBytes = ram
            });
        }

        return list;
    }

    private static HashSet<string> BuildTargetSet()
    {
        var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in Groups)
        {
            foreach (var n in g.ProcessNames)
                s.Add(n);
        }

        return s;
    }

    private static Dictionary<string, TimeSpan> SampleCpu(HashSet<string> targets)
    {
        var map = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var name = p.ProcessName;
                if (!targets.Contains(name))
                    continue;
                var key = name.ToLowerInvariant();
                if (map.TryGetValue(key, out var prev))
                    map[key] = prev + p.TotalProcessorTime;
                else
                    map[key] = p.TotalProcessorTime;
            }
            catch
            {
                /* */
            }
            finally
            {
                try { p.Dispose(); } catch { /* */ }
            }
        }

        return map;
    }

    private static List<Process> EnumerateMatchingProcesses(HashSet<string> targets)
    {
        var list = new List<Process>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (targets.Contains(p.ProcessName))
                    list.Add(p);
                else
                    p.Dispose();
            }
            catch
            {
                try { p.Dispose(); } catch { /* */ }
            }
        }

        return list;
    }

    private static string? ResolveLabel(string processName, HashSet<string> targets)
    {
        if (!targets.Contains(processName))
            return null;
        var key = processName.ToLowerInvariant();
        foreach (var g in Groups)
        {
            foreach (var n in g.ProcessNames)
            {
                if (string.Equals(n, processName, StringComparison.OrdinalIgnoreCase))
                    return g.Label;
            }
        }

        return null;
    }
}
