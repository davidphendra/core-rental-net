using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The exact SKU gate, expressed as a built-in MAF evaluation check.</summary>
public static class AnswerExactCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.AnswerExact,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var result = AnswerComparison.Evaluate(EvaluationContext.Parse(item.Context));

        return new EvalCheckResult(
            result.Exact,
            result.Exact ? "Exact SKU set matched." : string.Join("; ", result.FailureCodes),
            MetricNames.AnswerExact);
    }
}
