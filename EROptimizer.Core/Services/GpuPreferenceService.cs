using EROptimizer.Core.Backup;
using EROptimizer.Core.Models;
using Microsoft.Win32;

namespace EROptimizer.Core.Services;

public static class GpuPreferenceService
{
    private const string SubKey = @"Software\Microsoft\DirectX\UserGpuPreferences";

    public static StepResult Apply(string exeFullPath, BackupSession backup, ErLog log)
    {
        const string name = "GPU 고성능 등록";
        try
        {
            if (!File.Exists(exeFullPath))
                return new StepResult { Name = name, Success = false, Skipped = true, Message = "exe 없음" };

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(SubKey, true)
                ?? throw new InvalidOperationException("UserGpuPreferences");

            var before = key.GetValue(exeFullPath);
            var hivePath = "HKCU:\\" + SubKey;
            backup.AddRegistryBackup(hivePath, exeFullPath, before ?? "(없음)", "String");
            key.SetValue(exeFullPath, ErConstants.GpuPreferenceValue, RegistryValueKind.String);
            log.Info($"GpuPreference: {exeFullPath} = {ErConstants.GpuPreferenceValue}");
            return new StepResult { Name = name, Success = true, Skipped = false, Message = "적용됨" };
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            return new StepResult { Name = name, Success = false, Skipped = false, Message = ex.Message };
        }
    }
}
