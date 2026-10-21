using CoreRentalNet.Evaluation.Cases;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One evaluation run's rows, metrics and summary.</summary>
public sealed record EvaluationReport(
    IReadOnlyList<CaseResult> Cases,
    IReadOnlyList<MetricRow> Metrics,
    EvaluationSummary Summary)
{
    /// <summary>The portion of the script this report covers.</summary>
    public EvaluationScope Scope { get; init; } = EvaluationScope.All;

    /// <summary>The full fixture size, independent of the selected scope.</summary>
    public int TotalRows { get; init; }

    /// <summary>The reason a run stopped before its scope was complete, or null when it completed.</summary>
    public string? StopReason { get; init; }
}
