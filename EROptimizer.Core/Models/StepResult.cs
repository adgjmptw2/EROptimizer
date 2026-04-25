namespace EROptimizer.Core.Models;

public sealed class StepResult
{
    public required string Name { get; init; }
    public bool Success { get; init; }
    public bool Skipped { get; init; }
    public string Message { get; init; } = "";
}
