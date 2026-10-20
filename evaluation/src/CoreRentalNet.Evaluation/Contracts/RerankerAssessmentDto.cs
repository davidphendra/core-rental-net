using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One product the reranker ranked.</summary>
public sealed record RerankerAssessmentDto(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("relevance")] string Relevance);
