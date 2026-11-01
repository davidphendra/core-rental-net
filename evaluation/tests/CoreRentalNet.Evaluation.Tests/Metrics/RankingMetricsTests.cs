using AwesomeAssertions;
using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Metrics;

/// <summary>Ranking metrics score against labels, and say so when there are none.</summary>
public sealed class RankingMetricsTests
{
    private static readonly string[] Relevant = ["A", "B", "C"];

    [Fact]
    public void Recall_at_k_counts_the_relevant_items_in_the_top_k()
    {
        RankingMetrics.RecallAtK(Relevant, ["A", "X", "Y", "Z", "B"], 3).Should().BeApproximately(1d / 3, 1e-9);
        RankingMetrics.RecallAtK(Relevant, ["A", "X", "Y", "Z", "B"], 5).Should().BeApproximately(2d / 3, 1e-9);
    }

    [Fact]
    public void Precision_at_k_divides_by_k_not_by_the_results_returned()
    {
        RankingMetrics.PrecisionAtK(Relevant, ["A", "X"], 5).Should().BeApproximately(1d / 5, 1e-9);
        RankingMetrics.PrecisionAtK(Relevant, ["A", "X"], 2).Should().BeApproximately(1d / 2, 1e-9);
    }

    [Fact]
    public void A_case_with_no_labels_is_not_measurable_rather_than_zero()
    {
        RankingMetrics.RecallAtK([], ["A"], 3).Should().BeNull();
        RankingMetrics.PrecisionAtK([], ["A"], 3).Should().BeNull();
        RankingMetrics.NdcgAtK(new Dictionary<string, int>(), ["A"], 3).Should().BeNull();
    }

    [Fact]
    public void Ndcg_is_one_when_the_ordering_matches_the_grades()
    {
        var grades = new Dictionary<string, int> { ["A"] = 3, ["B"] = 1 };

        RankingMetrics.NdcgAtK(grades, ["A", "B"], 10).Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void Ndcg_falls_when_a_better_item_is_ranked_lower()
    {
        var grades = new Dictionary<string, int> { ["A"] = 3, ["B"] = 1 };

        var reversed = RankingMetrics.NdcgAtK(grades, ["B", "A"], 10);

        reversed.Should().BeLessThan(1).And.BeGreaterThan(0);
    }

    [Fact]
    public void A_permutation_of_equally_relevant_items_does_not_lose_score()
    {
        var grades = new Dictionary<string, int> { ["A"] = 1, ["B"] = 1 };

        RankingMetrics.NdcgAtK(grades, ["B", "A"], 10).Should().BeApproximately(1, 1e-9);
    }
}
