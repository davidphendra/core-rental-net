using CoreRentalNet.Evaluation.Analysis.Category;
using CoreRentalNet.Evaluation.Analysis.Generalization;
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

/// <summary>A weak category has to be visible even when the overall average looks healthy.</summary>
public sealed class CategoryOutcomePerformanceTests
{
    [Fact]
    public void Each_category_reports_its_own_accuracy_and_sample()
    {
        var results = CategoryOutcomePerformance.Evaluate(
        [
            new OutcomeRecord("A", "boundary", RagOutcome.Success, RagOutcome.Success),
            new OutcomeRecord("B", "adversarial", RagOutcome.Rejected, RagOutcome.Success),
            new OutcomeRecord("C", "adversarial", RagOutcome.Rejected, RagOutcome.Rejected),
        ]);

        results.Should().HaveCount(2);
        results.Single(result => result.Category == "boundary").Accuracy.Should().Be(1);
        results.Single(result => result.Category == "adversarial").Accuracy.Should().Be(0.5);
    }

    [Fact]
    public void A_category_whose_runs_were_all_indeterminate_has_no_accuracy()
    {
        var results = CategoryOutcomePerformance.Evaluate(
            [new OutcomeRecord("A", "unavailable", RagOutcome.Unavailable, RagOutcome.Indeterminate)]);

        results.Single().Accuracy.Should().BeNull();
        results.Single().Indeterminate.Should().Be(1);
    }
}
