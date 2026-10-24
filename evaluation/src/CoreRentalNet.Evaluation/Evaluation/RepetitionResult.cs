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

    /// <summary>Whether the local judge evaluated this repetition.</summary>
    public bool? JudgeEvaluated { get; init; }

    /// <summary>The local judge's verdict, when it evaluated this repetition.</summary>
    public bool? JudgePass { get; init; }

    /// <summary>The local judge's confidence, when it evaluated this repetition.</summary>
    public double? JudgeConfidence { get; init; }

    /// <summary>The number of judge attempts, when it evaluated this repetition.</summary>
    public int? JudgeAttempts { get; init; }

    /// <summary>The local judge's reason, when it evaluated this repetition.</summary>
    public string? JudgeReason { get; init; }

    public bool CasePass { get; init; } = MetricNames.CasePassMetrics.All(metric =>
        Metrics.TryGetValue(metric, out var passed) && passed);
}
