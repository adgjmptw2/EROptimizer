using System.Text.RegularExpressions;
using EROptimizer.Core.Services;
using Microsoft.Win32;

namespace EROptimizer.Core.Diagnostics;

public static class SystemDiagnosisProbe
{
    private static readonly Regex GuidRe = new(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}", RegexOptions.Compiled);

    private const long MinTempFreeBytes = 5L * 1024 * 1024 * 1024;

    public static SystemDiagnosisResult Collect(string? gameExe)
    {
        var adapters = DisplayAdapterProbe.GetAdapters();
        var (dvrOk, dvrD) = EvaluateGameDvr();
        var (barOk, barD) = EvaluateGameBar();
        var (pOk, pD) = EvaluateActivePower();
        var (gOk, gD) = EvaluateGpuPreferenceForExe(gameExe);
        var (bOk, bD) = EvaluateBootConfig(gameExe);
        var (tOk, tD) = EvaluateTempDrive();

        return new SystemDiagnosisResult
        {
            GameDvrOk = dvrOk,
            GameDvrDetail = dvrD,
            GameBarOk = barOk,
            GameBarDetail = barD,
            PowerHighPerformance = pOk,
            PowerDetail = pD,
            GameGpuHighPerformance = gOk,
            GameGpuDetail = gD,
            BootConfigOk = bOk,
            BootConfigDetail = bD,
            TempDriveEnoughSpace = tOk,
            TempSpaceDetail = tD,
            Adapters = adapters
        };
    }

    private static (bool Ok, string Detail) EvaluateGameDvr()
    {
        var a = ReadDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled");
        var b = ReadDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled");
        var c = ReadDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "GameDVR_Enabled");
        int? p = null;
        try
        {
            using var b64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var k = b64.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", false);
            p = k == null ? null : ReadDwordFromValue(k.GetValue("AllowGameDVR"));
        }
        catch
        {
            p = null;
        }

        int eff(int? x) => x ?? 1;
        var policyOk = p is null || p == 0;
        var r = eff(a) == 0 && eff(b) == 0 && eff(c) == 0 && policyOk;
        return (r, r ? "꺼짐(권장)" : "켜짐(또는 기본/미최적화)");
    }

    private static (bool Ok, string Detail) EvaluateGameBar()
    {
        var a = ReadDword(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled");
        var b = ReadDword(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode");
        var c = ReadDword(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "GameModeEnabled");
        var d = ReadDword(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled");
        int e(int? x) => x ?? 1;
        var r = e(a) == 0 && e(b) == 0 && e(c) == 0 && e(d) == 0;
        return (r, r ? "꺼짐(권장)" : "켜짐(또는 기본/미최적화)");
    }

    private static (bool Ok, string Detail) EvaluateActivePower()
    {
        try
        {
            var (ec, o, _) = ProcessRunner.Run("powercfg.exe", "/getactivescheme");
            if (o != null && o.IndexOf(ErConstants.HighPerformancePowerSchemeGuid, StringComparison.OrdinalIgnoreCase) >= 0)
                return (true, "고성능");
            var m = GuidRe.Match(o ?? "");
            var g = m.Success ? m.Value : "";
            if (g.Equals("381b4222-f694-41f0-9685-ff5bb260df2e", StringComparison.OrdinalIgnoreCase))
                return (false, "균형 조정");
            if (g.Equals("a1841308-3541-4fab-bc81-f71556f20b4a", StringComparison.OrdinalIgnoreCase))
                return (false, "절전");
            return (false, string.IsNullOrEmpty(g) ? "기타" : "기타(" + g + ")");
        }
        catch
        {
            return (false, "읽기 실패");
        }
    }

    private static (bool Ok, string Detail) EvaluateGpuPreferenceForExe(string? exe)
    {
        if (string.IsNullOrEmpty(exe))
            return (false, "exe 없음");
        const string sub = @"Software\Microsoft\DirectX\UserGpuPreferences";
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var k = baseKey.OpenSubKey(sub, false);
            if (k == null)
                return (false, "기본값(미등록)");
            var s = k.GetValue(exe)?.ToString() ?? "";
            if (s.Trim().Equals(ErConstants.GpuPreferenceValue.Trim(), StringComparison.Ordinal))
                return (true, "Windows에서 고성능(2;)");
            return (false, string.IsNullOrEmpty(s) ? "기본값" : s);
        }
        catch
        {
            return (false, "읽기 실패");
        }
    }

    private static (bool Ok, string Detail) EvaluateBootConfig(string? gameExe)
    {
        if (string.IsNullOrEmpty(gameExe))
            return (false, "게임 경로 없음");
        var path = BootConfigService.GetBootConfigPath(gameExe!);
        if (!File.Exists(path))
            return (false, "boot.config 없음");
        string raw;
        try
        {
            raw = File.ReadAllText(path);
        }
        catch
        {
            return (false, "읽기 실패");
        }

        var block = BootConfigPackage.BuildBlockText();
        var (ok, msg, merged) = BootConfigMerger.Merge(raw, block);
        if (!ok)
            return (false, "최적화: " + msg);
        var n1 = NormText(merged!);
        var n2 = NormText(raw);
        if (string.Equals(n1, n2, StringComparison.Ordinal))
            return (true, "권장 병합 반영됨");
        return (false, "권장 미적용 또는 부분만 적용");
    }

    private static (bool Ok, string Detail) EvaluateTempDrive()
    {
        string temp;
        try
        {
            temp = Path.GetTempPath();
        }
        catch
        {
            return (false, "읽기 실패");
        }

        try
        {
            var root = Path.GetPathRoot(temp);
            if (string.IsNullOrEmpty(root))
                return (false, "알 수 없음");
            var di = new DriveInfo(root);
            if (di.AvailableFreeSpace >= MinTempFreeBytes)
                return (true, "여유 " + HumanGiB(di.AvailableFreeSpace) + " (≥5GB 권고)");
            return (false, "여유 " + HumanGiB(di.AvailableFreeSpace) + " (5GB 미만 권고)");
        }
        catch
        {
            return (false, "용량 읽기 실패");
        }
    }

    private static int? ReadDword(RegistryHive hive, string sub, string name)
    {
        try
        {
            using var b = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var k = b.OpenSubKey(sub, false);
            if (k == null) return null;
            return ReadDwordFromValue(k.GetValue(name));
        }
        catch
        {
            return null;
        }
    }

    private static int? ReadDwordFromValue(object? v) =>
        v switch
        {
            int i => i,
            long l => (int)l,
            byte b => b,
            _ => null
        };

    private static string HumanGiB(long bytes)
    {
        if (bytes < 0) return "?";
        var g = bytes / 1024d / 1024d / 1024d;
        if (g >= 0.1) return g.ToString("0.0") + "GB";
        var m = bytes / 1024d / 1024d;
        return m.ToString("0") + "MB";
    }

    private static string NormText(string t) =>
        t.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd() + "\n";
}
