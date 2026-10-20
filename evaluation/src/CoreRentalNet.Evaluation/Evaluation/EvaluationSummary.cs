namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The aggregate view of one evaluation run.</summary>
public sealed record EvaluationSummary(
    int Rows,
    int Executions,
    int CasePass,
    double CasePassRate,
    IReadOnlyDictionary<string, double> MetricPassRates,
    IReadOnlyDictionary<string, int> Stability);
