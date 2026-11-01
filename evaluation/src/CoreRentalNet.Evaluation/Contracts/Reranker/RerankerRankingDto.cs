using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Reranker;

/// <summary>The reranker's answer, reduced to SKU and order.</summary>
public sealed record RerankerRankingDto(
    [property: JsonPropertyName("categories")] RerankerAssessmentsDto Categories);
