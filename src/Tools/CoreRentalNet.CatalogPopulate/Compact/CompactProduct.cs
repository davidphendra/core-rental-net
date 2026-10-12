using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Compact;

/// <summary>One product in the compact input: its source meta, all its reviews, and the true review count.</summary>
internal sealed record CompactProduct(
    [property: JsonPropertyName("parentAsin")] string ParentAsin,
    [property: JsonPropertyName("categoryKey")] string CategoryKey,
    [property: JsonPropertyName("meta")] CompactMeta Meta,
    [property: JsonPropertyName("reviews")] IReadOnlyList<CompactReview> Reviews,
    [property: JsonPropertyName("reviewCount")] int ReviewCount);
