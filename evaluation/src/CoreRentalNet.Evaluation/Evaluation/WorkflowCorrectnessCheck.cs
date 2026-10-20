using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

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
