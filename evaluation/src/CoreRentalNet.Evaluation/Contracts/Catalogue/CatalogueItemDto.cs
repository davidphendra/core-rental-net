using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Suggestion;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Catalogue;

/// <summary>One catalogue row.</summary>
public sealed record CatalogueItemDto(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("monthlyPrice")] CataloguePriceDto? MonthlyPrice = null);
