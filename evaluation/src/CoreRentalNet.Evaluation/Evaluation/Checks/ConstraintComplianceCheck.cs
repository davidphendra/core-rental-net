using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Checks;

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
