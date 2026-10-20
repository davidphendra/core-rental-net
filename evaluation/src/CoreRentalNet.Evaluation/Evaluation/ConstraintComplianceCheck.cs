using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The workspace-rule gate, expressed as a built-in MAF evaluation check.</summary>
public sealed class ConstraintComplianceCheck(IReadOnlyDictionary<string, ProductRecord> catalogue)
{
    public EvalCheck Create()
        => FunctionEvaluator.Create(
            MetricNames.ConstraintCompliance,
            (EvalItem item) => Evaluate(item));

    private EvalCheckResult Evaluate(EvalItem item)
    {
        var result = CandidateConstraints.Evaluate(
            EvaluationContext.Parse(item.Context),
            catalogue);

        return new EvalCheckResult(
            result.Compliant,
            result.Compliant ? "All constraints met." : string.Join("; ", result.FailureCodes),
            MetricNames.ConstraintCompliance);
    }
}
