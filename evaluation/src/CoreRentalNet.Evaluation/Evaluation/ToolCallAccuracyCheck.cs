using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The tool-call gate, expressed as a built-in MAF evaluation check.</summary>
public static class ToolCallAccuracyCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.ToolCallAccuracy,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var result = ToolCallAccuracy.Evaluate(EvaluationContext.Parse(item.Context));

        return new EvalCheckResult(
            result.Accurate,
            result.Accurate
                ? $"Every expected component was searched ({result.Coverage:P0} coverage)."
                : string.Join("; ", result.FailureCodes),
            MetricNames.ToolCallAccuracy);
    }
}
