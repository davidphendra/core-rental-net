namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>A run's raw, uninterpreted trace.</summary>
public sealed record RunTrace(
    string RunId,
    IReadOnlyList<TraceStageCall> StageCalls,
    IReadOnlyList<TraceToolExchange> ToolExchanges);
