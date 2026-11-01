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

/// <summary>Imported judge metrics sit in the same record as the deterministic ones.</summary>
public sealed class RunAssemblerImportTests
{
    [Fact]
    public void An_imported_metric_appears_beside_the_local_metrics()
    {
        var record = new RunAssembler().Assemble(
            Dataset(),
            [Run()],
            new RunIdentity("run", "0.1.0", "Qwen3-Coder-30B-A3B-Instruct", "gpt-oss-20b", DateTimeOffset.UnixEpoch, 1),
            [new EvaluationMetricResult("faithfulness", 0.8, 12, 1, 0.95, 0.6, 0.9, "insufficient sample")]);

        record.Metrics.Should().Contain(metric => metric.Metric == "faithfulness");
        record.Metrics.Should().Contain(metric => metric.Metric == "outcome_accuracy");
    }

    private static RagEvaluationDataset Dataset() => new(
        DatasetName.Validation, "memory", "v1", "hash", 0,
        [new RagEvaluationCase { Id = "VAL-001", Category = "normal", Query = "q", ExpectedOutcome = RagOutcome.Success }],
        []);

    private static RagRunResult Run() => new() { CaseId = "VAL-001", Query = "q", Outcome = RagOutcome.Success, AnswerText = "a" };
}
