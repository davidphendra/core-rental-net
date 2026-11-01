using AwesomeAssertions;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.Writers;
using CoreRentalNet.Evaluation.Reporting.AgentEval;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Reporting;

/// <summary>A delta is only trustworthy when the dataset and the judge did not move.</summary>
public sealed class RunComparerTests
{
    [Fact]
    public void A_changed_dataset_is_declared_before_the_deltas_are_read()
    {
        var report = RunComparer.Compare(Run("hash-a"), Run("hash-b"));

        report.Incompatibilities.Should().Contain(message => message.Contains("dataset changed"));
    }

    [Fact]
    public void A_changed_judge_is_declared()
    {
        var report = RunComparer.Compare(Run("hash", judge: "gpt-oss-20b"), Run("hash", judge: "other"));

        report.Incompatibilities.Should().Contain(message => message.Contains("judge model changed"));
    }

    [Fact]
    public void The_same_configuration_yields_a_delta_and_no_warning()
    {
        var report = RunComparer.Compare(Run("hash", score: 0.5), Run("hash", score: 0.7));

        report.Incompatibilities.Should().BeEmpty();
        report.Metrics.Single().Delta.Should().BeApproximately(0.2, 1e-9);
    }

    private static EvaluationRunRecord Run(string hash, string judge = "gpt-oss-20b", double score = 0.5) => new()
    {
        RunId = "run",
        Dataset = DatasetName.Validation,
        DatasetVersion = "v1",
        DatasetSha256 = hash,
        JudgeModel = judge,
        GeneratorModel = "Qwen3-Coder-30B-A3B-Instruct",
        Metrics = [new EvaluationMetricResult("outcome_accuracy", score, 40, 0, 0.95, null, null, "evaluable")],
    };
}
