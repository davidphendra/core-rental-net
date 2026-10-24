using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One catalogue row.</summary>
public sealed record CatalogueItemDto(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("monthlyPrice")] CataloguePriceDto? MonthlyPrice = null);
