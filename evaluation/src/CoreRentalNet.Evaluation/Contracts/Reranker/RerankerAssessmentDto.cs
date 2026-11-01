using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Reranker;

/// <summary>One product the reranker ranked.</summary>
public sealed record RerankerAssessmentDto(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("relevance")] string Relevance);
