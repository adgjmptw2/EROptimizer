using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EROptimizer.Core.StorageCleanup;

public static class StorageCleanupJson
{
    public static void SaveCleanupPreviewJson(
        string path,
        string sessionId,
        StorageAnalysisReport analysis,
        IReadOnlyList<CleanupPreviewRow> rows,
        IReadOnlyList<string> selectedIds)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var obj = new
        {
            sessionId,
            generatedUtc = DateTime.UtcNow,
            selectedIds,
            analysis,
            previewRows = rows
        };
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        });
        File.WriteAllText(path, json);
    }

    public static void SaveCleanupResultJson(string path, string sessionId, CleanupExecutionReport report)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var obj = new
        {
            sessionId,
            report
        };
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
