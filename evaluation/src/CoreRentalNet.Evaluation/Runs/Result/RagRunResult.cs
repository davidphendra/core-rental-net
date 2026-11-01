using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Assembly;
namespace CoreRentalNet.Evaluation.Runs.Result;

/// <summary>One case's execution, with the three candidate sets kept apart.</summary>
/// <remarks>
/// Retrieved, reranked and recommended are different stages and are never merged. A product can be found by
/// search, dropped by the reranker, or reranked and then omitted from the recommendation; collapsing them would
/// make every one of those failures look identical in the report.
/// </remarks>
public sealed record RagRunResult
{
    public required string CaseId { get; init; }

    public required string Query { get; init; }

    /// <summary>Everything either retrieval tool returned, in the order it returned it.</summary>
    public IReadOnlyList<string> RetrievedSkus { get; init; } = [];

    /// <summary>Each tool's answer separately, so BM25 and semantic recall can be told apart.</summary>
    public IReadOnlyList<RagRetrievalPass> RetrievalPasses { get; init; } = [];

    /// <summary>What the reranker selected from that.</summary>
    public IReadOnlyList<string> RerankedSkus { get; init; } = [];

    /// <summary>What the final recommendation actually contains.</summary>
    public IReadOnlyList<RagSuggestedLine> RecommendedLines { get; init; } = [];

    public RagOutcome Outcome { get; init; } = RagOutcome.Indeterminate;

    /// <summary>The status the run reported verbatim, kept for the report even when it mapped to nothing.</summary>
    public string RunStatus { get; init; } = string.Empty;

    public bool Completed { get; init; }

    public bool TimedOut { get; init; }

    public double WallMs { get; init; }

    public IReadOnlyList<string> FailureCodes { get; init; } = [];

    /// <summary>The distinct SKUs the recommendation names, which is the set a recommendation metric compares.</summary>
    /// <summary>The answer a judge reads, rendered from the recommendation rather than paraphrased.</summary>
    public string AnswerText { get; init; } = string.Empty;

    public IReadOnlySet<string> RecommendedSkus => RecommendedLines
        .Select(line => line.Sku)
        .ToHashSet(StringComparer.Ordinal);
}
