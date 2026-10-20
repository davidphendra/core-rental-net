namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One evaluation run's rows, metrics and summary.</summary>
public sealed record EvaluationReport(
    IReadOnlyList<CaseResult> Cases,
    IReadOnlyList<MetricRow> Metrics,
    EvaluationSummary Summary);
