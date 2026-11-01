using AwesomeAssertions;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Evaluators.Judges;
using CoreRentalNet.Evaluation.Evaluators.Correctness;
using CoreRentalNet.Evaluation.Evaluators.Shared;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Runs;

/// <summary>A recommendation is compared as a set, and a same-slot swap is visible as its own verdict.</summary>
public sealed class AnswerCorrectnessTests
{
    private static readonly string[] Monitor = ["MONB086Q776F"];

    [Fact]
    public void The_same_skus_in_a_different_order_are_correct()
    {
        var expected = Case(["MONB086Q776F", "DSKB08XN4JDR", "CHAB09RLYPZ1"]);
        var run = Run(RagOutcome.Success, [Line("Desk", "DSKB08XN4JDR"), Line("Chair", "CHAB09RLYPZ1"), Line("Monitor", "MONB086Q776F")]);

        AnswerCorrectness.Evaluate(expected, run).Should().Be(AnswerVerdict.Correct);
    }

    [Fact]
    public void A_different_product_filling_the_same_slot_is_substituted_not_missed()
    {
        var expected = Case(Monitor);
        var run = Run(RagOutcome.Success, [Line("Desk", "DSKB08XN4JDR"), Line("Chair", "CHAB09RLYPZ1"), Line("Monitor", "MONB09GS97F5")]);

        AnswerCorrectness.Evaluate(expected, run).Should().Be(AnswerVerdict.Substituted);
    }

    [Fact]
    public void A_different_composition_is_a_miss()
    {
        var expected = Case(Monitor);
        var run = Run(RagOutcome.Success, [Line("Desk", "DSKB08XN4JDR"), Line("Chair", "CHAB09RLYPZ1"), Line("Monitor", "MONB09GS97F5", 2)]);

        AnswerCorrectness.Evaluate(expected, run).Should().Be(AnswerVerdict.Miss);
    }

    [Fact]
    public void The_wrong_outcome_is_reported_before_the_products_are_considered()
    {
        var expected = Case(Monitor);
        var run = Run(RagOutcome.Rejected, []);

        AnswerCorrectness.Evaluate(expected, run).Should().Be(AnswerVerdict.OutcomeMismatch);
    }

    [Fact]
    public void A_case_without_named_skus_grades_on_its_outcome_alone()
    {
        var expected = Case([]);
        var run = Run(RagOutcome.Success, []);

        AnswerCorrectness.Evaluate(expected, run).Should().Be(AnswerVerdict.Correct);
    }

    [Fact]
    public void A_run_that_reported_nothing_is_indeterminate_rather_than_wrong()
    {
        var expected = Case(Monitor);
        var run = Run(RagOutcome.Indeterminate, []);

        AnswerCorrectness.Evaluate(expected, run).Should().Be(AnswerVerdict.Indeterminate);
    }

    private static RagEvaluationCase Case(IReadOnlyList<string> skus) => new()
    {
        Id = "VAL-001",
        Category = "normal",
        Query = "one desk, one chair, one monitor",
        ExpectedOutcome = RagOutcome.Success,
        ExpectedEquipment = new EquipmentExpectation(1, 1, 1, 0, 0, 0, 0),
        ExpectedWorkspaceIds = skus,
    };

    private static RagSuggestedLine Line(string slot, string sku, int quantity = 1) => new(slot, sku, quantity);

    private static RagRunResult Run(RagOutcome outcome, IReadOnlyList<RagSuggestedLine> lines) => new()
    {
        CaseId = "VAL-001",
        Query = "one desk, one chair, one monitor",
        Outcome = outcome,
        RecommendedLines = lines,
        AnswerText = "rendered",
    };
}
