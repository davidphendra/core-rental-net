using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Shared;
namespace CoreRentalNet.Evaluation.Evaluators.Correctness;

/// <summary>How a recommendation compared with the case's ground truth.</summary>
public enum AnswerVerdict
{
    Correct,
    Substituted,
    Miss,
    OutcomeMismatch,
    Indeterminate,
}

/// <summary>Compares a recommendation with its case deterministically, before any judge is consulted.</summary>
/// <remarks>
/// The SKU comparison is set equality, never sequence equality: the reranker may reorder products without being
/// wrong. A different product that fills the same slot with the same quantity is reported as
/// <see cref="AnswerVerdict.Substituted"/> rather than a miss, which keeps substitution visible as its own rate.
/// </remarks>
public static class AnswerCorrectness
{
    public static AnswerVerdict Evaluate(RagEvaluationCase expected, RagRunResult run)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(run);

        if (run.Outcome == RagOutcome.Indeterminate)
        {
            return AnswerVerdict.Indeterminate;
        }

        if (run.Outcome != expected.ExpectedOutcome)
        {
            return AnswerVerdict.OutcomeMismatch;
        }

        if (expected.ExpectedWorkspaceIds.Count == 0)
        {
            return AnswerVerdict.Correct;
        }

        if (run.RecommendedSkus.SetEquals(expected.ExpectedWorkspaceIds))
        {
            return AnswerVerdict.Correct;
        }

        return SameComposition(expected.ExpectedEquipment, run)
            ? AnswerVerdict.Substituted
            : AnswerVerdict.Miss;
    }

    private static bool SameComposition(EquipmentExpectation expected, RagRunResult run)
    {
        var actual = ConstraintRuleBreakdown.Totals(run.RecommendedLines);

        return expected.Desks == actual.GetValueOrDefault("desk")
            && expected.Chairs == actual.GetValueOrDefault("chair")
            && expected.Monitors == actual.GetValueOrDefault("monitor")
            && expected.Lamps == actual.GetValueOrDefault("lamp")
            && expected.Plants == actual.GetValueOrDefault("plant")
            && expected.BeanBags == actual.GetValueOrDefault("bean bag")
            && expected.CoffeeMachines == actual.GetValueOrDefault("coffee machine");
    }
}
