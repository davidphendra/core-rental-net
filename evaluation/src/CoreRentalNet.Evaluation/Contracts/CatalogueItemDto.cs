using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One catalogue row.</summary>
public sealed record CatalogueItemDto(
    [property: JsonPropertyName("sku")] string Sku);
