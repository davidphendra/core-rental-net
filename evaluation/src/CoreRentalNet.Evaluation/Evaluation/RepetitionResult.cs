namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One execution's local metric outcomes and scores.</summary>
public sealed record RepetitionResult(
    int Repetition,
    IReadOnlyDictionary<string, bool> Metrics,
    IReadOnlyList<string> FailureCodes,
    double AnswerF1,
    double RetrievalRecall)
{
    public bool CasePass { get; } = MetricNames.All.All(metric =>
        Metrics.TryGetValue(metric, out var passed) && passed);
}
