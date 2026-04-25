using EROptimizer.Core.Backup;
using EROptimizer.Core.Models;
using Microsoft.Win32;

namespace EROptimizer.Core.Services;

public static class GameBarService
{
    private sealed record RegOp(RegistryHive Hive, string SubKey, string ValueName, int DWordValue, bool Optional);

    private static readonly RegOp[] Ops =
    [
        new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, false),
        new(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, false),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, false),
        new(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 0, false),
        new(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 0, false),
        new(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "GameModeEnabled", 0, true),
        new(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, true),
        new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "GameDVR_Enabled", 0, true),
    ];

    public static StepResult Apply(BackupSession backup, ErLog log)
    {
        const string name = "게임 바/DVR/모드";
        var errors = 0;
        foreach (var op in Ops)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(op.Hive, RegistryView.Registry64);
                using var key = baseKey.CreateSubKey(op.SubKey, true)
                    ?? throw new InvalidOperationException($"키 생성 실패: {op.SubKey}");

                var before = key.GetValue(op.ValueName);
                var hivePath = (op.Hive == RegistryHive.LocalMachine ? "HKLM:" : "HKCU:") + "\\" + op.SubKey;
                backup.AddRegistryBackup(hivePath, op.ValueName, before ?? "(없음)", "DWord");
                key.SetValue(op.ValueName, op.DWordValue, RegistryValueKind.DWord);
                log.Info($"{hivePath}\\{op.ValueName} = {op.DWordValue} (이전={before})");
            }
            catch (Exception ex)
            {
                if (op.Optional)
                    log.Warn($"[선택키 스킵] {op.SubKey}\\{op.ValueName}: {ex.Message}");
                else
                {
                    errors++;
                    log.Warn($"실패: {op.SubKey}\\{op.ValueName}: {ex.Message}");
                }
            }
        }

        return new StepResult
        {
            Name = name,
            Success = errors == 0,
            Skipped = false,
            Message = errors == 0 ? "모든 필수 키 적용" : $"필수 키 중 {errors}개 실패(로그 참고)"
        };
    }
}
