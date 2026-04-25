namespace EROptimizer.Core.Services;

public static class BootConfigMerger
{
    public static (bool Ok, string Message, string? NewText) Merge(string originalText, string blockText)
    {
        var presetLines = blockText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal))
            .ToList();

        var presetOrder = new List<string>();
        var presetVals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ln in presetLines)
        {
            var eq = ln.IndexOf('=');
            if (eq < 1) continue;
            var k = ln.Substring(0, eq).Trim();
            var v = ln.Substring(eq + 1).Trim();
            if (!presetVals.ContainsKey(k))
                presetOrder.Add(k);
            presetVals[k] = v;
        }

        var lines = originalText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        string? buildLine = null;
        var buildIndex = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("build-guid", StringComparison.OrdinalIgnoreCase))
            {
                buildLine = lines[i];
                buildIndex = i;
                break;
            }
        }

        if (string.IsNullOrEmpty(buildLine))
            return (false, "build-guid 줄을 찾을 수 없습니다.", null);

        var before = lines.Take(buildIndex).ToList();
        var origOrder = new List<string>();
        var origVals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ln in before)
        {
            var t = ln.Trim();
            if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal)) continue;
            var eq = t.IndexOf('=');
            if (eq < 1) continue;
            var k = t.Substring(0, eq).Trim();
            var v = t.Substring(eq + 1).Trim();
            if (k.Equals("build-guid", StringComparison.OrdinalIgnoreCase)) continue;
            if (!origVals.ContainsKey(k))
                origOrder.Add(k);
            origVals[k] = v;
        }

        var finalVals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in origOrder)
            finalVals[k] = origVals[k];
        foreach (var k in presetOrder)
            finalVals[k] = presetVals[k];

        var presetKeySet = new HashSet<string>(presetOrder, StringComparer.OrdinalIgnoreCase);
        var outLines = new List<string>();
        foreach (var k in presetOrder)
        {
            if (finalVals.TryGetValue(k, out var val))
                outLines.Add($"{k}={val}");
        }
        foreach (var k in origOrder)
        {
            if (!presetKeySet.Contains(k) && finalVals.TryGetValue(k, out var val))
                outLines.Add($"{k}={val}");
        }
        outLines.Add(buildLine.TrimEnd(new[] { '\r' }));
        var text = string.Join("\n", outLines) + "\n";
        return (true, "병합 완료", text);
    }
}
