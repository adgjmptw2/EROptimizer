using EROptimizer.Core.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EROptimizer.Core.Services;

public static class BackupRestoreService
{
    private static readonly JsonSerializerSettings JsonRead = new()
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static IReadOnlyList<string> ListSessionPathsDescending(string workspaceRoot)
    {
        var root = Path.Combine(workspaceRoot, "EROptimizer_Backup");
        if (!Directory.Exists(root))
            return Array.Empty<string>();
        return Directory.GetDirectories(root)
            .OrderByDescending(static d => Path.GetFileName(d), StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<string> FindBootBackupsInSession(string sessionPath)
    {
        var files = Path.Combine(sessionPath, "files");
        if (!Directory.Exists(files))
            return Array.Empty<string>();
        return Directory.GetFiles(files, "boot.config.bak_*")
            .OrderBy(static f => f, StringComparer.Ordinal)
            .ToList();
    }

    public static StepResult RestoreRegistryFromSession(string sessionPath, ErLog log)
    {
        const string name = "레지 복원";
        var jsonPath = Path.Combine(sessionPath, "registry_backup.json");
        if (!File.Exists(jsonPath))
            return new StepResult { Name = name, Success = false, Skipped = true, Message = "registry_backup.json 없음" };

        List<RegistryBackupEntry>? entries;
        try
        {
            var text = File.ReadAllText(jsonPath);
            entries = JsonConvert.DeserializeObject<List<RegistryBackupEntry>>(text, JsonRead);
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }

        if (entries == null || entries.Count == 0)
            return new StepResult { Name = name, Success = true, Skipped = true, Message = "항목 없음" };

        var errors = 0;
        foreach (var e in entries)
        {
            try
            {
                if (!TryParseHivePath(e.Path, out var hive, out var subKey))
                {
                    errors++;
                    log.Warn($"경로 파싱 실패: {e.Path}");
                    continue;
                }

                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(subKey, writable: true);
                if (key == null)
                {
                    log.Warn($"키 없음(건너뜀): {e.Path}");
                    continue;
                }

                if (IsAbsentSentinel(e.ValueBefore))
                {
                    try { key.DeleteValue(e.Name, throwOnMissingValue: false); }
                    catch (Exception ex) { errors++; log.Warn($"삭제 실패 {e.Path}\\{e.Name}: {ex.Message}"); }
                    continue;
                }

                if (string.Equals(e.Kind, "String", StringComparison.OrdinalIgnoreCase))
                {
                    var s = CoerceString(e.ValueBefore);
                    if (s == null) { errors++; continue; }
                    key.SetValue(e.Name, s, RegistryValueKind.String);
                }
                else if (string.Equals(e.Kind, "DWord", StringComparison.OrdinalIgnoreCase))
                {
                    var dw = CoerceInt(e.ValueBefore);
                    if (dw == null) { errors++; continue; }
                    key.SetValue(e.Name, dw.Value, RegistryValueKind.DWord);
                }
                else
                    log.Warn($"알 수 없는 Kind: {e.Kind} @ {e.Name}");
            }
            catch (Exception ex)
            {
                errors++;
                log.Warn($"{e.Path}\\{e.Name}: {ex.Message}");
            }
        }

        log.Info($"레지 복원 완료: {entries.Count}건, 오류 {errors}");
        return new StepResult { Name = name, Success = errors == 0, Skipped = false, Message = errors == 0 ? "완료" : $"오류 {errors}건" };
    }

    public static StepResult RestorePowerPlanFromSession(string sessionPath, ErLog log)
    {
        const string name = "전원 복원";
        var txt = Path.Combine(sessionPath, "power_plan_backup.txt");
        if (!File.Exists(txt))
            return new StepResult { Name = name, Success = false, Skipped = true, Message = "power_plan_backup.txt 없음" };

        var guid = File.ReadAllText(txt).Trim();
        if (string.IsNullOrWhiteSpace(guid))
            return new StepResult { Name = name, Success = false, Skipped = true, Message = "GUID 비어 있음" };

        var (ec, _, err) = ProcessRunner.Run("powercfg.exe", $"/setactive {guid}");
        log.Info($"powercfg /setactive {guid} exit={ec} {err}");
        return new StepResult { Name = name, Success = ec == 0, Skipped = false, Message = ec == 0 ? guid : err };
    }

    public static StepResult RestoreBootFile(string gameExePath, string bakFullPath, ErLog log)
    {
        const string name = "boot.config";
        try
        {
            if (!File.Exists(bakFullPath))
                return new StepResult { Name = name, Success = false, Skipped = true, Message = "백업 파일 없음" };

            var dest = BootConfigService.GetBootConfigPath(gameExePath);
            var dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(bakFullPath, dest, overwrite: true);
            log.Info($"boot.config 복원: {bakFullPath} → {dest}");
            return new StepResult { Name = name, Success = true, Skipped = false, Message = "백업에서 복원" };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }

    private static bool TryParseHivePath(string path, out RegistryHive hive, out string subKey)
    {
        hive = RegistryHive.CurrentUser;
        subKey = "";
        var p = path.Trim().Replace('/', '\\');
        if (p.StartsWith("HKCU:", StringComparison.OrdinalIgnoreCase) ||
            p.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
        {
            hive = RegistryHive.CurrentUser;
            subKey = StripHivePrefix(p, "HKCU:", "HKEY_CURRENT_USER");
            return subKey.Length > 0;
        }

        if (p.StartsWith("HKLM:", StringComparison.OrdinalIgnoreCase) ||
            p.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
        {
            hive = RegistryHive.LocalMachine;
            subKey = StripHivePrefix(p, "HKLM:", "HKEY_LOCAL_MACHINE");
            return subKey.Length > 0;
        }

        return false;
    }

    private static string StripHivePrefix(string p, string shortH, string longH)
    {
        if (p.StartsWith(shortH, StringComparison.OrdinalIgnoreCase))
            return p[shortH.Length..].TrimStart('\\');
        if (p.StartsWith(longH, StringComparison.OrdinalIgnoreCase))
            return p[longH.Length..].TrimStart('\\');
        return "";
    }

    private static bool IsAbsentSentinel(object? v)
    {
        if (v == null) return true;
        if (v is string s)
            return s is "(없음)" or "(none)" || string.IsNullOrWhiteSpace(s);
        if (v is JValue jv && jv.Type == JTokenType.String)
        {
            var t = jv.Value<string>();
            return t is "(없음)" or "(none)" || string.IsNullOrWhiteSpace(t);
        }
        return false;
    }

    private static string? CoerceString(object? v)
    {
        if (v is string s) return s;
        if (v is JValue jv)
        {
            if (jv.Type == JTokenType.String) return jv.Value<string>();
            return jv.ToString();
        }
        return v?.ToString();
    }

    private static int? CoerceInt(object? v)
    {
        switch (v)
        {
            case int i: return i;
            case long l: return checked((int)l);
            case JValue jv:
                if (jv.Type == JTokenType.Integer) return jv.Value<int>();
                if (jv.Type == JTokenType.Float) return (int)jv.Value<double>();
                if (jv.Type == JTokenType.String && int.TryParse(jv.Value<string>(), out var p))
                    return p;
                return null;
            default:
                return null;
        }
    }
}
