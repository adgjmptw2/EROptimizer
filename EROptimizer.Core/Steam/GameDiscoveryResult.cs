namespace EROptimizer.Core;

public sealed class GameDiscoveryResult
{
    public string? SteamRoot { get; init; }
    public string? InstallDirectory { get; init; }
    public string? GameExePath { get; init; }

    public bool IsComplete =>
        !string.IsNullOrEmpty(GameExePath)
        && File.Exists(GameExePath!);
}
