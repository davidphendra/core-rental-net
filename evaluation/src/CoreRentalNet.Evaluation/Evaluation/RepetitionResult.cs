namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One execution's local metric outcomes and scores.</summary>
public sealed record RepetitionResult(
    int Repetition,
    IReadOnlyDictionary<string, bool> Metrics,
    IReadOnlyList<string> FailureCodes,
    double AnswerF1,
    double RetrievalRecall)
{
    /// <summary>The measured timing tree for this execution.</summary>
    public RunTiming? Timing { get; init; }

    /// <summary>Whether the reranker returned a non-empty selection drawn from its input.</summary>
    public bool RerankerAligned { get; init; }

    public bool CasePass { get; init; } = MetricNames.CasePassMetrics.All(metric =>
        Metrics.TryGetValue(metric, out var passed) && passed);
}
