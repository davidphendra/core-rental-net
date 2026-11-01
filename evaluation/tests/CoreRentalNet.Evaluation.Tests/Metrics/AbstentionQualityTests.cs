using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
using Xunit;
using AwesomeAssertions;

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>Refusing an answerable request and answering an unanswerable one are separate failures.</summary>
public sealed class AbstentionQualityTests
{
    [Fact]
    public void Recommending_for_a_request_that_should_abstain_is_false_acceptance()
    {
        var report = AbstentionQuality.Evaluate(
        [
            Record("A", RagOutcome.Rejected, RagOutcome.Success),
            Record("B", RagOutcome.Unavailable, RagOutcome.Unavailable),
        ]);

        report.Abstainable.Should().Be(2);
        report.FalseAcceptanceRate.Should().Be(0.5);
        report.CorrectAbstentions.Should().Be(1);
    }

    [Fact]
    public void Abstaining_on_an_answerable_request_is_false_rejection()
    {
        var report = AbstentionQuality.Evaluate(
        [
            Record("A", RagOutcome.Success, RagOutcome.Unavailable),
            Record("B", RagOutcome.Success, RagOutcome.Success),
        ]);

        report.Answerable.Should().Be(2);
        report.FalseRejectionRate.Should().Be(0.5);
        report.CorrectAnswers.Should().Be(1);
    }

    [Fact]
    public void An_indeterminate_run_is_counted_separately_and_not_as_a_refusal()
    {
        var report = AbstentionQuality.Evaluate(
        [
            Record("A", RagOutcome.Success, RagOutcome.Indeterminate),
        ]);

        report.Indeterminate.Should().Be(1);
        report.Answerable.Should().Be(0);
        report.FalseRejectionRate.Should().BeNull();
    }

    private static OutcomeRecord Record(string id, RagOutcome expected, RagOutcome actual)
        => new(id, "normal", expected, actual);
}
