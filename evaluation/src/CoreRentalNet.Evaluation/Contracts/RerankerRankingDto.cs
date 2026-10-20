using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>The reranker's answer, reduced to SKU and order.</summary>
public sealed record RerankerRankingDto(
    [property: JsonPropertyName("categories")] RerankerAssessmentsDto Categories);
