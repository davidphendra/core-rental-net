using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Compact;

/// <summary>The source fields kept for one product in the compact input.</summary>
internal sealed record CompactMeta(
    [property: JsonPropertyName("parentAsin")] string? ParentAsin,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] IReadOnlyList<string> Description,
    [property: JsonPropertyName("categories")] IReadOnlyList<string> Categories,
    [property: JsonPropertyName("details")] IReadOnlyDictionary<string, JsonElement> Details,
    [property: JsonPropertyName("price")] double? Price,
    [property: JsonPropertyName("averageRating")] double? AverageRating,
    [property: JsonPropertyName("ratingNumber")] int? RatingNumber,
    [property: JsonPropertyName("store")] string? Store,
    [property: JsonPropertyName("image")] string? Image);
