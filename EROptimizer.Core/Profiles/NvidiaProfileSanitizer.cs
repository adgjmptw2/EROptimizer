namespace EROptimizer.Core.Profiles;

public static class NvidiaProfileSanitizer
{
    private static readonly string[] BlockedNameSubstrings =
    {
        "vertical sync",
        "tear control",
        "preferred refresh rate",
        "g-sync",
        "vrr",
        "triple buffer",
        "frame rate limit",
        "vulkan",
        "opengl present", // "Vulkan/OpenGL present method"
        "present method",
        "texture filtering - quality",
        "ansel",
        "virtual reality",
        "smooth afr",
        "openvr",
        "variable refresh"
    };

    private static readonly HashSet<long> BlockedSettingIds = new()
    {
        274606621,
        2_156_231_208L
    };

    public static ErNvidiaProfileDoc Sanitize(ErNvidiaProfileDoc source, string applicationExeFileName)
    {
        var app = Path.GetFileName(applicationExeFileName.Trim());
        if (string.IsNullOrEmpty(app))
            app = "eternalreturn.exe";
        app = app.ToLowerInvariant();

        var list = new List<ErNvidiaProfileSetting>();
        var seen = new HashSet<long>();

        foreach (var s in source.Settings)
        {
            if (!seen.Add(s.SettingId))
                continue;
            if (BlockedSettingIds.Contains(s.SettingId))
                continue;
            if (ShouldDrop(s))
                continue;
            list.Add(new ErNvidiaProfileSetting
            {
                SettingId = s.SettingId,
                SettingName = s.SettingName,
                SettingType = s.SettingType,
                Value = s.Value,
                BinaryValue = s.BinaryValue
            });
        }

        return new ErNvidiaProfileDoc
        {
            ProfileName = source.ProfileName + " (EROptimizer safe subset)",
            ApplicationName = app,
            Settings = list
        };
    }

    private static bool ShouldDrop(ErNvidiaProfileSetting s)
    {
        var name = s.SettingName ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return true;

        var lower = name.ToLowerInvariant();
        foreach (var b in BlockedNameSubstrings)
        {
            if (lower.Contains(b, StringComparison.Ordinal))
                return true;
        }

        if (lower.Contains("dlss"))
        {
            if (s.Value == "0")
                return false;
            return true;
        }

        if (s.SettingId == 284810369)
            return true;

        return false;
    }
}
