using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Timing;

/// <summary>The timing breakdown of one execution.</summary>
public sealed record RunTiming(
    double TotalMs,
    IReadOnlyList<StageTiming> Stages,
    IReadOnlyList<ModelCallTiming> ModelCalls,
    IReadOnlyList<ToolCallTiming> ToolCalls)
{
    /// <summary>The actual case wall time, when the runner measured it.</summary>
    public double WallMs { get; init; }

    public double ModelMs { get; } = ModelCalls.Count > 0
        ? ModelCalls.Sum(call => call.DurationMs)
        : Stages.Sum(stage => stage.ModelMs);

    public double ToolMs { get; } = ToolCalls.Count > 0
        ? ToolCalls.Sum(call => call.DurationMs)
        : Stages.Sum(stage => stage.ToolMs);

    public double GuardrailMs { get; } = Stages.Sum(stage => stage.GuardrailMs);

    public double HostMs { get; } = Stages.Sum(stage => stage.HostMs);

    public double UnattributedMs { get; } = Stages.Sum(stage => stage.UnattributedMs);

    /// <summary>Time the trace could not attribute to a node, usually workflow scheduling.</summary>
    public double UnaccountedMs => Math.Max(0, WallMs - TotalMs);

    public string? SlowestStage { get; } = Stages
        .OrderByDescending(stage => stage.TotalMs)
        .Select(stage => stage.Stage)
        .FirstOrDefault();
}
