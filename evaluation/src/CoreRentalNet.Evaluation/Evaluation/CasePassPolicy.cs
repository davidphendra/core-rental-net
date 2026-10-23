namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The majority rule for one case, with the one-repetition case stated explicitly.</summary>
public static class CasePassPolicy
{
    public static bool Passed(int passCount, int repetitionCount)
        => repetitionCount > 0
           && passCount >= Math.Min(2, repetitionCount);

    public static bool Passed(
        IReadOnlyDictionary<string, bool> metrics,
        bool rerankerAligned,
        bool timedOut)
        => !timedOut
           && (rerankerAligned
               || MetricNames.CasePassMetrics.All(metric =>
                   metrics.TryGetValue(metric, out var passed) && passed));
}
