using AwesomeAssertions;
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

/// <summary>Each retrieval stage is scored on its own, so a drop can be attributed to one of them.</summary>
public sealed class StageComparisonTests
{
    [Fact]
    public void Bm25_and_semantic_are_scored_separately_from_the_rerank()
    {
        var stages = StageComparison.Evaluate(Case(["A", "C"]), Run(), [2]);

        Metric(stages, StageComparison.Bm25, "recall@2").Should().Be(0.5);
        Metric(stages, StageComparison.Semantic, "recall@2").Should().Be(0.5);
        Metric(stages, StageComparison.Combined, "recall@2").Should().Be(0.5);
        Metric(stages, StageComparison.Reranked, "recall@2").Should().Be(1);
    }

    [Fact]
    public void A_case_without_labels_reports_no_stage_score()
    {
        var stages = StageComparison.Evaluate(Case([]), Run(), [3]);

        stages.Should().OnlyContain(stage => stage.Metrics.Values.All(value => value == null));
    }

    [Fact]
    public void Ndcg_follows_the_same_labels_as_recall()
    {
        var stages = StageComparison.Evaluate(Case(["A"]), Run(), [3]);

        Metric(stages, StageComparison.Bm25, "ndcg@3").Should().Be(1);
    }

    private static double? Metric(IReadOnlyList<StageResult> stages, string stage, string metric)
        => stages.Single(result => result.Stage == stage).Metrics[metric];

    private static RagEvaluationCase Case(IReadOnlyList<string> relevant) => new()
    {
        Id = "TS-001",
        Category = "uncategorized",
        Query = "query",
        ExpectedOutcome = RagOutcome.Success,
        RelevantWorkspaceIds = relevant,
    };

    private static RagRunResult Run() => new()
    {
        CaseId = "TS-001",
        Query = "query",
        RetrievalPasses =
        [
            new RagRetrievalPass("search_catalog", ["A", "B"]),
            new RagRetrievalPass("search_semantic_catalog", ["C", "D"]),
        ],
        RetrievedSkus = ["A", "B", "C", "D"],
        RerankedSkus = ["C", "A"],
    };
}
