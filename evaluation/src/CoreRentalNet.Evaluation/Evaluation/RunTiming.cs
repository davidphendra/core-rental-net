namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The timing breakdown of one execution.</summary>
public sealed record RunTiming(
    double TotalMs,
    IReadOnlyList<StageTiming> Stages,
    IReadOnlyList<ModelCallTiming> ModelCalls,
    IReadOnlyList<ToolCallTiming> ToolCalls)
{
    public double ModelMs { get; } = ModelCalls.Count > 0
        ? ModelCalls.Sum(call => call.DurationMs)
        : Stages.Sum(stage => stage.ModelMs);

    public double ToolMs { get; } = ToolCalls.Count > 0
        ? ToolCalls.Sum(call => call.DurationMs)
        : Stages.Sum(stage => stage.ToolMs);
    public double GuardrailMs { get; } = Stages.Sum(stage => stage.GuardrailMs);
    public double HostMs { get; } = Stages.Sum(stage => stage.HostMs);
    public double UnattributedMs { get; } = Stages.Sum(stage => stage.UnattributedMs);

    public string? SlowestStage { get; } = Stages
        .OrderByDescending(stage => stage.TotalMs)
        .Select(stage => stage.Stage)
        .FirstOrDefault();
}
