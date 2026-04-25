using System.Text.Json;
using EROptimizer.Core.Models;

namespace EROptimizer.Core.Backup;

public sealed class BackupSession
{
    private readonly List<RegistryBackupEntry> _registry = new();
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public BackupSession(string rootDirectory, string sessionId)
    {
        SessionId = sessionId;
        SessionPath = Path.Combine(rootDirectory, "EROptimizer_Backup", sessionId);
        FilesPath = Path.Combine(SessionPath, "files");
        Directory.CreateDirectory(SessionPath);
        Directory.CreateDirectory(FilesPath);
        RegistryJsonPath = Path.Combine(SessionPath, "registry_backup.json");
        File.WriteAllText(RegistryJsonPath, "[]");
    }

    public string SessionId { get; }
    public string SessionPath { get; }
    public string FilesPath { get; }
    public string RegistryJsonPath { get; }

    public void AddRegistryBackup(string hivePath, string valueName, object? valueBefore, string kind)
    {
        _registry.Add(new RegistryBackupEntry
        {
            Timestamp = DateTimeOffset.Now.ToString("o"),
            Path = hivePath,
            Name = valueName,
            ValueBefore = valueBefore,
            Kind = kind
        });
        FlushRegistryJson();
    }

    public void FlushRegistryJson()
    {
        var json = JsonSerializer.Serialize(_registry, _json);
        File.WriteAllText(RegistryJsonPath, json);
    }

    public string BackupFile(string sourcePath, string? destFileName = null)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException(sourcePath);
        var name = destFileName ?? Path.GetFileName(sourcePath);
        var dest = Path.Combine(FilesPath, name);
        File.Copy(sourcePath, dest, overwrite: true);
        return dest;
    }

    public void WritePowerPlanBackup(string activeSchemeGuid)
    {
        File.WriteAllText(Path.Combine(SessionPath, "power_plan_backup.txt"), activeSchemeGuid.Trim());
    }
}
