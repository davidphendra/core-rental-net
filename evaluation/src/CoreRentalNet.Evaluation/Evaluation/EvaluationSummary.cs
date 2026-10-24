namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The aggregate view of one evaluation run.</summary>
public sealed record EvaluationSummary(
    int Rows,
    int Executions,
    int CasePass,
    double CasePassRate,
    IReadOnlyDictionary<string, double> MetricPassRates,
    IReadOnlyDictionary<string, int> Stability)
{
    /// <summary>Whether a local judge was configured for the run.</summary>
    public bool JudgeConfigured { get; init; }

    /// <summary>The number of repetitions the local judge evaluated.</summary>
    public int JudgeEvaluated { get; init; }

    /// <summary>The number of judged repetitions that passed.</summary>
    public int JudgePass { get; init; }

    /// <summary>The pass rate over judged repetitions, or null when the judge did not run.</summary>
    public double? JudgePassRate { get; init; }
}
