using AwesomeAssertions;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Runs;

/// <summary>A failed case names the first thing it broke, in the order a reader would check it.</summary>
public sealed class RunAssemblerFailureTests
{
    [Fact]
    public void A_broken_rule_is_named_before_a_wrong_recommendation()
    {
        var record = new RunAssembler().Assemble(Dataset(RagOutcome.Success), [Run(RagOutcome.Rejected)], Identity());

        var failed = record.Cases.Single();
        failed.FailedMetric.Should().Be("constraint_compliance");
        failed.Query.Should().Be("two monitors please");
        failed.RetrievedSkus.Should().Equal("MONB086Q776F");
    }

    [Fact]
    public void A_case_that_broke_nothing_records_no_failed_metric()
    {
        var record = new RunAssembler().Assemble(Dataset(RagOutcome.Success), [Run(RagOutcome.Success, compliant: true)], Identity());

        record.Cases.Single().FailedMetric.Should().BeNull();
    }

    private static RunIdentity Identity() => new(
        "run", "0.1.0", "Qwen3-Coder-30B-A3B-Instruct", "gpt-oss-20b", DateTimeOffset.UnixEpoch, 1);

    private static RagEvaluationDataset Dataset(RagOutcome outcome) => new(
        DatasetName.Validation, "memory", "v1", "hash", 0,
        [
            new RagEvaluationCase
            {
                Id = "VAL-001",
                Category = "normal",
                Query = "two monitors please",
                ExpectedOutcome = outcome,
                ExpectedEquipment = new EquipmentExpectation(1, 1, 2, 0, 0, 0, 0),
            },
        ],
        []);

    private static RagRunResult Run(RagOutcome outcome, bool compliant = false) => new()
    {
        CaseId = "VAL-001",
        Query = "two monitors please",
        Outcome = outcome,
        RetrievedSkus = ["MONB086Q776F"],
        AnswerText = "answer",
        RecommendedLines = compliant
            ? [new RagSuggestedLine("Desk", "D", 1), new RagSuggestedLine("Chair", "C", 1), new RagSuggestedLine("Monitor", "M", 2)]
            : [new RagSuggestedLine("Desk", "D", 1)],
    };
}
