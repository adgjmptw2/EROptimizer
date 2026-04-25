using System.Reflection;
using EROptimizer.Core.Backup;
using EROptimizer.Core.Hardware;
using EROptimizer.Core.Models;
using EROptimizer.Core.Profiles;
using Newtonsoft.Json;

namespace EROptimizer.Core.Services;

public static class NvidiaSafeProfileService
{
    private const string ExportFileName = "er_profile_safe_export.json";
    private const string EmbeddedLogicalName = "EROptimizer.Core.er_profile_backup.json";
    private static readonly JsonSerializerSettings JsonRead = new() { MissingMemberHandling = MissingMemberHandling.Ignore };
    private static readonly JsonSerializerSettings JsonWrite = new() { Formatting = Formatting.Indented };

    public static StepResult Export(string gameExePath, BackupSession backup, HardwareSnapshot hw, ErLog log)
    {
        const string name = "NV 안전프로필";
        try
        {
            if (!hw.ShouldExportNvidiaSafeProfile)
            {
                const string why = "NVIDIA 없음, NV 프로필 JSON 스킵";
                log.Info(why);
                return new StepResult { Name = name, Success = true, Skipped = true, Message = why };
            }

            var embedded = LoadEmbeddedBackup();
            if (embedded == null)
                return new StepResult { Name = name, Success = false, Skipped = false, Message = "내장 er_profile_backup.json 없음" };

            var exeName = Path.GetFileName(gameExePath);
            var safe = NvidiaProfileSanitizer.Sanitize(embedded, exeName);
            var json = JsonConvert.SerializeObject(safe, JsonWrite);
            var outPath = Path.Combine(backup.FilesPath, ExportFileName);
            File.WriteAllText(outPath, json, new System.Text.UTF8Encoding(false));
            log.Info($"NV 프로필 JSON 저장: {outPath} (동기화·VRR·G-SYNC·주사율·FRTC·DLSS 강제 등 빼고 잘라냄)");
            log.Info("쓰려면 NVIDIA 제어판이나 Profile Inspector에서 이 JSON import");
            return new StepResult
            {
                Name = name,
                Success = true,
                Skipped = false,
                Message = $"저장 {ExportFileName} ({safe.Settings.Count}항목)"
            };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }

    private static ErNvidiaProfileDoc? LoadEmbeddedBackup()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(EmbeddedLogicalName)
            ?? asm.GetManifestResourceNames()
                .Where(n => n.EndsWith("er_profile_backup.json", StringComparison.Ordinal))
                .Select(asm.GetManifestResourceStream)
                .OfType<Stream>()
                .FirstOrDefault();
        if (stream == null)
            return null;
        using (stream)
        using (var reader = new StreamReader(stream))
        {
            var text = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<ErNvidiaProfileDoc>(text, JsonRead);
        }
    }
}
