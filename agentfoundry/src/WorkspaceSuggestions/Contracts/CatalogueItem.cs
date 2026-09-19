using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>One catalogue row as the application pushes it: the compact projection, description included.</summary>
/// <remarks>
/// This is the <c>catalogueItem</c> of <c>suggestion.request.schema.json</c>. The projection is pushed
/// exactly as the application publishes it — <see cref="Description"/> is kept rather than trimmed,
/// because the metadata that makes qualitative matching possible is what the payload is spent on.
/// </remarks>
public sealed record CatalogueItem(
    [property: JsonPropertyName("sku")]
    [property: Description("The product's SKU. The only identifier a result may quote back.")]
    string Sku,
    [property: JsonPropertyName("name")]
    [property: Description("The product's name.")]
    string Name,
    [property: JsonPropertyName("category")]
    [property: Description("The product's category: desk, chair or accessory.")]
    string Category,
    [property: JsonPropertyName("subCategory")]
    [property: Description("The product's accessory sub-category, or null when it is not an accessory.")]
    string? SubCategory,
    [property: JsonPropertyName("pricePerMonth")]
    [property: Description("The monthly price, in the request's currency. The agent must never quote it.")]
    int PricePerMonth,
    [property: JsonPropertyName("description")]
    [property: Description("The product's own description.")]
    string Description,
    [property: JsonPropertyName("metadata")]
    [property: Description("What the product says about itself.")]
    CatalogueMetadata Metadata);
