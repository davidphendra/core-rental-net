using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Reporting.Bias;

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

    /// <summary>The A-D bias metrics, when the run was configured to produce them.</summary>
    public BiasReport? Bias { get; init; }

    /// <summary>The AgentEval stochastic statistics over this run's repetitions.</summary>
    public StochasticSummary? Stochastic { get; init; }

    /// <summary>The evaluation policy digest recorded when the run was produced.</summary>
    public string? PolicyDigest { get; init; }

    /// <summary>Whether the AgentEval RAG judge was configured for this run.</summary>
    public bool RagConfigured { get; init; }
}
