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

/// <summary>Every metric in a record comes from one grading of the same cases.</summary>
public sealed class RunAssemblerTests
{
    [Fact]
    public void The_record_grades_outcomes_and_answers_from_the_same_pass()
    {
        var record = new RunAssembler().Assemble(Dataset(), Runs(), Identity());

        record.TotalCases.Should().Be(3);
        Metric(record, "outcome_accuracy").Should().BeApproximately(2d / 3, 1e-9);
        Metric(record, "answer_correctness").Should().BeApproximately(0.5, 1e-9);
        Metric(record, "substitution_rate").Should().BeApproximately(0.5, 1e-9);
        Metric(record, "false_acceptance").Should().Be(1);
    }

    [Fact]
    public void A_case_with_no_run_is_recorded_as_indeterminate_not_as_a_failure_to_answer()
    {
        var record = new RunAssembler().Assemble(Dataset(), [Runs()[0]], Identity());
        var missing = record.Cases.Single(testCase => testCase.CaseId == "VAL-002");

        missing.Actual.Should().Be(RagOutcome.Indeterminate);
        missing.FailureCodes.Should().Contain("RUN_MISSING");
        record.Indeterminate.Should().Be(2);
    }

    [Fact]
    public void The_record_carries_the_provenance_a_comparison_needs()
    {
        var record = new RunAssembler().Assemble(Dataset(), Runs(), Identity());

        record.IsLockbox.Should().BeTrue();
        record.DatasetSha256.Should().Be("abc123");
        record.GeneratorModel.Should().Be("Qwen3-Coder-30B-A3B-Instruct");
        record.Metrics.Should().OnlyContain(metric => metric.SampleSize >= 0);
    }

    private static double? Metric(EvaluationRunRecord record, string name)
        => record.Metrics.Single(metric => metric.Metric == name).Score;

    private static RunIdentity Identity() => new(
        "run-1", "0.1.0", "Qwen3-Coder-30B-A3B-Instruct", "gpt-oss-20b", DateTimeOffset.UnixEpoch, 1000);

    private static RagEvaluationDataset Dataset() => new(
        DatasetName.Test, "memory", "v1", "abc123", 0,
        [
            Case("VAL-001", RagOutcome.Success, ["DSKB08XN4JDR", "CHAB09RLYPZ1", "MONB086Q776F"]),
            Case("VAL-002", RagOutcome.Success, ["DSKB08XN4JDR", "CHAB09RLYPZ1", "MONB086Q776F"]),
            Case("VAL-003", RagOutcome.Rejected, []),
        ],
        []);

    private static RagEvaluationCase Case(string id, RagOutcome outcome, IReadOnlyList<string> skus) => new()
    {
        Id = id,
        Category = "normal",
        Query = "query",
        ExpectedOutcome = outcome,
        ExpectedEquipment = outcome == RagOutcome.Rejected
            ? new EquipmentExpectation(2, 1, 0, 0, 0, 0, 0)
            : new EquipmentExpectation(1, 1, 1, 0, 0, 0, 0),
        ExpectedWorkspaceIds = skus,
    };

    private static RagRunResult[] Runs() =>
    [
        Run("VAL-001", RagOutcome.Success, [Line("Desk", "DSKB08XN4JDR"), Line("Chair", "CHAB09RLYPZ1"), Line("Monitor", "MONB086Q776F")]),
        Run("VAL-002", RagOutcome.Success, [Line("Desk", "DSKB08XN4JDR"), Line("Chair", "CHAB09RLYPZ1"), Line("Monitor", "MONB09GS97F5")]),
        Run("VAL-003", RagOutcome.Success, [Line("Desk", "DSKB08XN4JDR")]),
    ];

    private static RagSuggestedLine Line(string slot, string sku) => new(slot, sku, 1);

    private static RagRunResult Run(string id, RagOutcome outcome, IReadOnlyList<RagSuggestedLine> lines) => new()
    {
        CaseId = id,
        Query = "query",
        Outcome = outcome,
        RecommendedLines = lines,
        AnswerText = "answer",
    };
}
