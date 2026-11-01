using AwesomeAssertions;
using CoreRentalNet.Evaluation.Configuration;
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
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>A pass line is attached to a score, and withheld when the sample cannot support it.</summary>
public sealed class ThresholdPolicyTests
{
    private static readonly EvaluationThresholds Thresholds =
        new(new Dictionary<string, double>(StringComparer.Ordinal) { ["outcome_accuracy"] = 0.8 });

    [Fact]
    public void A_score_above_the_configured_line_passes()
    {
        var metric = Apply(Score(0.9, "evaluable")).Metrics.Single();

        metric.Passed.Should().BeTrue();
        metric.Threshold.Should().Be(0.8);
    }

    [Fact]
    public void A_score_below_the_line_fails()
        => Apply(Score(0.7, "evaluable")).Metrics.Single().Passed.Should().BeFalse();

    [Fact]
    public void A_high_score_on_too_small_a_sample_cannot_pass()
        => Apply(Score(1.0, "insufficient sample")).Metrics.Single().Passed.Should().BeFalse();

    [Fact]
    public void A_metric_with_no_configured_line_gets_no_verdict()
    {
        var metric = Apply(new EvaluationMetricResult("relevance", 4.2, 40, 0, 0.95, 3.9, 4.5, "evaluable")).Metrics.Single();

        metric.Threshold.Should().BeNull();
        metric.Passed.Should().BeNull();
    }

    private static EvaluationMetricResult Score(double score, string status)
        => new("outcome_accuracy", score, 40, 0, 0.95, 0.7, 0.95, status);

    private static EvaluationRunRecord Apply(EvaluationMetricResult metric)
        => ThresholdPolicy.Apply(Record(metric), Thresholds);

    private static EvaluationRunRecord Record(EvaluationMetricResult metric) => new()
    {
        RunId = "run",
        Dataset = DatasetName.Validation,
        DatasetVersion = "v1",
        DatasetSha256 = "hash",
        Metrics = [metric],
    };
}
