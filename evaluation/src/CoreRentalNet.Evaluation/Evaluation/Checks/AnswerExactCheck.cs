using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Checks;

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
