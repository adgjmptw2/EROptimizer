namespace EROptimizer.Core.Diagnostics;

public static class NvidiaDriverVersionCompare
{
    public static int Compare(string? a, string? b)
    {
        var pa = Split(a);
        var pb = Split(b);
        if (pa.Count == 0 || pb.Count == 0) return 0;
        var n = Math.Max(pa.Count, pb.Count);
        for (var i = 0; i < n; i++)
        {
            var va = i < pa.Count ? pa[i] : 0;
            var vb = i < pb.Count ? pb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }

    private static List<int> Split(string? s)
    {
        var r = new List<int>();
        if (string.IsNullOrWhiteSpace(s)) return r;
        foreach (var p in s!.Trim().Split('.'))
        {
            var t = p.Trim();
            if (int.TryParse(t, out var n))
                r.Add(n);
        }
        return r;
    }
}
