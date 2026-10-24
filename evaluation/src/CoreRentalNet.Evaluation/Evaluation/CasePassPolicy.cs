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
        bool timedOut,
        bool judgeEvaluated = false,
        bool judgePass = false)
    {
        if (timedOut)
        {
            return false;
        }

        if (!judgeEvaluated)
        {
            return rerankerAligned
                   || MetricNames.CasePassMetrics.All(metric =>
                       metrics.TryGetValue(metric, out var passed) && passed);
        }

        var hardGate =
            Passed(metrics, MetricNames.ConstraintCompliance)
            && Passed(metrics, MetricNames.ToolCallAccuracy)
            && Passed(metrics, MetricNames.WorkflowCorrectness);

        return hardGate && rerankerAligned && judgePass;
    }

    private static bool Passed(IReadOnlyDictionary<string, bool> metrics, string metric)
        => metrics.TryGetValue(metric, out var passed) && passed;
}
