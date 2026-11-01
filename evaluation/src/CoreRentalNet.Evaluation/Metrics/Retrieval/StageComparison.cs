using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Retrieval;

/// <summary>Scores each retrieval stage against the same labels, so a regression can be traced to one of them.</summary>
/// <remarks>
/// The four stages are evaluated independently: a product found by BM25 but dropped by the reranker shows as good
/// retrieval and a worse rerank, rather than as one undifferentiated "missing product".
/// </remarks>
public static class StageComparison
{
    public const string Bm25 = "bm25";
    public const string Semantic = "semantic";
    public const string Combined = "combined";
    public const string Reranked = "reranked";

    public static IReadOnlyList<StageResult> Evaluate(
        RagEvaluationCase testCase,
        RagRunResult run,
        IReadOnlyList<int> kValues)
    {
        ArgumentNullException.ThrowIfNull(testCase);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(kValues);

        var relevant = testCase.RelevantWorkspaceIds;
        var grades = relevant
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(sku => sku, _ => 1, StringComparer.Ordinal);

        return
        [
            Score(Bm25, Pass(run, semantic: false), relevant, grades, kValues),
            Score(Semantic, Pass(run, semantic: true), relevant, grades, kValues),
            Score(Combined, run.RetrievedSkus, relevant, grades, kValues),
            Score(Reranked, run.RerankedSkus, relevant, grades, kValues),
        ];
    }

    private static IReadOnlyList<string> Pass(RagRunResult run, bool semantic)
        => [.. run.RetrievalPasses
            .Where(pass => pass.ToolName.Contains("semantic", StringComparison.OrdinalIgnoreCase) == semantic)
            .SelectMany(pass => pass.Skus)];

    private static StageResult Score(
        string stage,
        IReadOnlyList<string> ranked,
        IReadOnlyList<string> relevant,
        IReadOnlyDictionary<string, int> grades,
        IReadOnlyList<int> kValues)
    {
        var metrics = new Dictionary<string, double?>(StringComparer.Ordinal);

        foreach (var k in kValues)
        {
            metrics[$"recall@{k}"] = RankingMetrics.RecallAtK(relevant, ranked, k);
            metrics[$"ndcg@{k}"] = RankingMetrics.NdcgAtK(grades, ranked, k);
        }

        return new StageResult(stage, metrics);
    }
}

/// <summary>One retrieval stage's metrics for one case.</summary>
public sealed record StageResult(string Stage, IReadOnlyDictionary<string, double?> Metrics);
