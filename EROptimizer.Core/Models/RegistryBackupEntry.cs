namespace EROptimizer.Core.Models;

public sealed class RegistryBackupEntry
{
    public string Timestamp { get; set; } = "";
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public object? ValueBefore { get; set; }
    public string Kind { get; set; } = "";
}
