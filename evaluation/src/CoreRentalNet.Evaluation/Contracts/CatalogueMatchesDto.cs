using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>The rows one catalogue answer carried.</summary>
public sealed record CatalogueMatchesDto(
    [property: JsonPropertyName("value")] IReadOnlyList<CatalogueItemDto> Value);
