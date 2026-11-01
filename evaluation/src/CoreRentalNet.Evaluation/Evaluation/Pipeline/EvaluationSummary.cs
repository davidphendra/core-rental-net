using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

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

    /// <summary>Pass rates for the AgentEval RAG diagnostics, over measured rows only.</summary>
    public IReadOnlyDictionary<string, double?> RagMetricPassRates { get; init; } =
        new Dictionary<string, double?>(StringComparer.Ordinal);

    /// <summary>Mean scores for the AgentEval RAG diagnostics, over measured rows only.</summary>
    public IReadOnlyDictionary<string, double?> RagMetricMeans { get; init; } =
        new Dictionary<string, double?>(StringComparer.Ordinal);

    /// <summary>The number of RAG metric rows that produced a measurement.</summary>
    public int RagMeasured { get; init; }

    /// <summary>The number of RAG metric rows that could not be measured.</summary>
    public int RagNotMeasured { get; init; }
}
