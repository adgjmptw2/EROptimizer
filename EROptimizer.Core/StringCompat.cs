namespace EROptimizer.Core;

internal static class StringCompat
{
    public static bool Contains(this string s, string value, StringComparison comparison) =>
        s.IndexOf(value, comparison) >= 0;
}
