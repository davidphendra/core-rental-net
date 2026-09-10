using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>Shape of one entry in products.json. Infrastructure-only; never leaves this layer.</summary>
internal sealed class CatalogFileRecord
{
    [JsonPropertyName("skuNo")]
    public string? SkuNo { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("subCategory")]
    public string? SubCategory { get; set; }

    [JsonPropertyName("pricePerMonth")]
    public decimal PricePerMonth { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("badge")]
    public string? Badge { get; set; }
}
