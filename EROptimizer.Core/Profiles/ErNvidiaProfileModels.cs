using System.Text.Json.Serialization;

namespace EROptimizer.Core.Profiles;

public sealed class ErNvidiaProfileDoc
{
    public string ProfileName { get; set; } = "";
    public string ApplicationName { get; set; } = "";
    public List<ErNvidiaProfileSetting> Settings { get; set; } = new();
}

public sealed class ErNvidiaProfileSetting
{
    public long SettingId { get; set; }
    public string? SettingName { get; set; }
    public string SettingType { get; set; } = "Integer";
    public string Value { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? BinaryValue { get; set; }
}
