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

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>A category rate is read from the same records as the run rate, so the two cannot disagree.</summary>
public sealed class CategoryMetricPerformanceTests
{
    [Fact]
    public void Each_category_reports_its_own_metrics()
    {
        var categories = CategoryMetricPerformance.Evaluate(Record());

        categories.Select(category => category.Category).Should().Equal("invalid", "normal");
        categories.Single(c => c.Category == "normal").Values["outcome_accuracy"].Should().Be(1);
        categories.Single(c => c.Category == "invalid").Values["outcome_accuracy"].Should().Be(0);
    }

    [Fact]
    public void A_judge_metric_appears_only_where_the_judge_scored_that_category()
    {
        var categories = CategoryMetricPerformance.Evaluate(Record());

        categories.Single(c => c.Category == "normal").Values["relevance"].Should().BeApproximately(4, 1e-9);
        categories.Single(c => c.Category == "invalid").Values.GetValueOrDefault("relevance").Should().BeNull();
    }

    [Fact]
    public void A_category_with_no_determinate_outcome_has_no_rate()
    {
        var record = Record() with
        {
            Cases = [new CaseRunRecord("TST-002", "ambiguous", RagOutcome.Success, RagOutcome.Indeterminate, false, [], [])],
        };

        CategoryMetricPerformance.Evaluate(record).Single().Values["outcome_accuracy"].Should().BeNull();
    }

    private static EvaluationRunRecord Record() => new()
    {
        RunId = "run",
        Dataset = DatasetName.Validation,
        DatasetVersion = "v1",
        DatasetSha256 = "hash",
        Cases =
        [
            new CaseRunRecord("TST-001", "normal", RagOutcome.Success, RagOutcome.Success, true, [], []),
            new CaseRunRecord("TST-002", "invalid", RagOutcome.Rejected, RagOutcome.Success, false, [],
                ["CONSTRAINT_VIOLATION"]) { FailedMetric = "constraint_compliance" },
        ],
        Judgements = [new CaseJudgement("TST-001", "relevance", 4, "on topic", "gpt-oss-20b")],
    };
}
