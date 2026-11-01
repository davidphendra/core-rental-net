using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Trace;

/// <summary>A run's raw, uninterpreted trace.</summary>
public sealed record RunTrace(
    string RunId,
    IReadOnlyList<TraceStageCall> StageCalls,
    IReadOnlyList<TraceToolExchange> ToolExchanges)
{
    /// <summary>The activity spans the agent emitted, when span capture is enabled.</summary>
    public IReadOnlyList<TraceSpan> Spans { get; init; } = [];
}
