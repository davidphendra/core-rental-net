using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Catalogue;

/// <summary>The rows one catalogue answer carried.</summary>
public sealed record CatalogueMatchesDto(
    [property: JsonPropertyName("value")] IReadOnlyList<CatalogueItemDto> Value);
