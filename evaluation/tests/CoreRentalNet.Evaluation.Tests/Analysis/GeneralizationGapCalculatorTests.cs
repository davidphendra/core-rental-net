using AwesomeAssertions;
using CoreRentalNet.Evaluation.Analysis.Category;
using CoreRentalNet.Evaluation.Analysis.Generalization;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Analysis;

/// <summary>The gap is a diagnostic, and it is only computed where both runs measured a metric.</summary>
public sealed class GeneralizationGapCalculatorTests
{
    [Fact]
    public void The_gap_is_development_minus_test()
    {
        var report = GeneralizationGapCalculator.Compare(
            Run(DatasetName.Development, ("outcome_accuracy", 0.9)),
            Run(DatasetName.Test, ("outcome_accuracy", 0.6)));

        report.Gaps.Single().Gap.Should().BeApproximately(0.3, 1e-9);
    }

    [Fact]
    public void A_negative_gap_is_reported_as_negative()
    {
        var report = GeneralizationGapCalculator.Compare(
            Run(DatasetName.Development, ("outcome_accuracy", 0.6)),
            Run(DatasetName.Test, ("outcome_accuracy", 0.8)));

        report.Gaps.Single().Gap.Should().BeApproximately(-0.2, 1e-9);
    }

    [Fact]
    public void A_metric_only_one_run_measured_is_left_out_and_the_caveat_is_stated()
    {
        var report = GeneralizationGapCalculator.Compare(
            Run(DatasetName.Development, ("outcome_accuracy", 0.9), ("relevance", 4.2)),
            Run(DatasetName.Test, ("outcome_accuracy", 0.6)));

        report.Gaps.Should().ContainSingle().Which.Metric.Should().Be("outcome_accuracy");
        report.Caveat.Should().Contain("optimistic");
    }

    private static EvaluationRunRecord Run(DatasetName name, params (string Metric, double Score)[] metrics) => new()
    {
        RunId = name.ToString(),
        Dataset = name,
        DatasetVersion = "v1",
        DatasetSha256 = "hash",
        Metrics = [.. metrics.Select(metric => new EvaluationMetricResult(metric.Metric, metric.Score, 40, 0, 0.95, null, null, "evaluable"))],
    };
}
