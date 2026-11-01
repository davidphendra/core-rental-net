using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
using Xunit;
using AwesomeAssertions;

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>An estimate carries its sample size, and refuses to look confident on a small one.</summary>
public sealed class ProportionEstimateTests
{
    [Fact]
    public void A_small_sample_is_marked_insufficient()
    {
        var estimate = ProportionEstimate.From("outcome_accuracy", [true, false], 0, 0.95, minimumSampleSize: 30);

        estimate.Value.Should().Be(0.5);
        estimate.SampleSize.Should().Be(2);
        estimate.Status.Should().Be(ProportionEstimateStatus.InsufficientSample);
    }

    [Fact]
    public void A_sample_at_the_floor_is_evaluable_and_the_interval_brackets_the_value()
    {
        var outcomes = Enumerable.Range(0, 40).Select(index => index % 4 != 0).ToArray();

        var estimate = ProportionEstimate.From("outcome_accuracy", outcomes, 0, 0.95, minimumSampleSize: 30);

        estimate.Status.Should().Be(ProportionEstimateStatus.Evaluable);
        estimate.Lower.Should().BeLessThan(estimate.Value);
        estimate.Upper.Should().BeGreaterThan(estimate.Value);
        estimate.Lower.Should().BeGreaterThanOrEqualTo(0);
        estimate.Upper.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void No_outcomes_yields_the_whole_range_and_no_claim()
    {
        var estimate = ProportionEstimate.From("outcome_accuracy", [], excluded: 3, 0.95, 30);

        estimate.SampleSize.Should().Be(0);
        estimate.Excluded.Should().Be(3);
        estimate.Lower.Should().Be(0);
        estimate.Upper.Should().Be(1);
        estimate.Status.Should().Be(ProportionEstimateStatus.InsufficientSample);
    }
}
