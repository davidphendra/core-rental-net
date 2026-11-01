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

/// <summary>Accuracy counts determinate runs only; a silent run is neither correct nor wrong.</summary>
public sealed class OutcomeAccuracyTests
{
    [Fact]
    public void An_indeterminate_run_is_reported_and_excluded_from_accuracy()
    {
        var report = OutcomeAccuracy.Evaluate(
        [
            Record("A", RagOutcome.Success, RagOutcome.Success),
            Record("B", RagOutcome.Success, RagOutcome.Indeterminate),
        ]);

        report.Cases.Should().Be(2);
        report.Determinate.Should().Be(1);
        report.Indeterminate.Should().Be(1);
        report.Accuracy.Should().Be(1);
    }

    [Fact]
    public void The_confusion_matrix_keys_every_expected_and_actual_pair()
    {
        var report = OutcomeAccuracy.Evaluate(
        [
            Record("A", RagOutcome.Success, RagOutcome.Rejected),
            Record("B", RagOutcome.Unavailable, RagOutcome.Unavailable),
        ]);

        report.Confusion["Success->Rejected"].Should().Be(1);
        report.Confusion["Unavailable->Unavailable"].Should().Be(1);
    }

    [Fact]
    public void A_class_with_no_predictions_scores_zero_rather_than_throwing()
    {
        var report = OutcomeAccuracy.Evaluate([Record("A", RagOutcome.Success, RagOutcome.Success)]);
        var rejected = report.Classes.Single(score => score.Outcome == "Rejected");

        rejected.Support.Should().Be(0);
        rejected.Precision.Should().Be(0);
        rejected.F1.Should().Be(0);
    }

    [Fact]
    public void Precision_recall_and_f1_follow_their_definitions()
    {
        var report = OutcomeAccuracy.Evaluate(
        [
            Record("A", RagOutcome.Success, RagOutcome.Success),
            Record("B", RagOutcome.Success, RagOutcome.Success),
            Record("C", RagOutcome.Success, RagOutcome.Rejected),
            Record("D", RagOutcome.Rejected, RagOutcome.Rejected),
            Record("E", RagOutcome.Rejected, RagOutcome.Success),
        ]);

        var success = report.Classes.Single(score => score.Outcome == "Success");
        success.Precision.Should().BeApproximately(2d / 3, 1e-9);
        success.Recall.Should().BeApproximately(2d / 3, 1e-9);
        success.F1.Should().BeApproximately(2d / 3, 1e-9);
    }

    private static OutcomeRecord Record(string id, RagOutcome expected, RagOutcome actual)
        => new(id, "normal", expected, actual);
}
