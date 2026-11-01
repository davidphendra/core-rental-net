using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>The workflow gate, expressed as a built-in MAF evaluation check.</summary>
public static class WorkflowCorrectnessCheck
{
    public static EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.WorkflowCorrectness,
            (EvalItem item) => Evaluate(item));

    private static EvalCheckResult Evaluate(EvalItem item)
    {
        var result = WorkflowSequence.Evaluate(EvaluationContext.Parse(item.Context));

        return new EvalCheckResult(
            result.Correct,
            result.Correct
                ? "The expected workflow transitions ran to success."
                : string.Join("; ", result.FailureCodes),
            MetricNames.WorkflowCorrectness);
    }
}
