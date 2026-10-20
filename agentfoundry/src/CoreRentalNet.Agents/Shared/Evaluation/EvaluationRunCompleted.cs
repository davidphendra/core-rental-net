namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>The last record of a run: how it ended.</summary>
internal sealed record EvaluationRunCompleted(
    string RunStatus)
    : EvaluationTraceRecord(null, 0, default);
