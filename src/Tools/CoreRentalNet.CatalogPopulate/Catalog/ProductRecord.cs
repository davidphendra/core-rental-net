using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Catalog;

/// <summary>One row of products.json, in the shape the catalogue loader reads.</summary>
internal sealed class ProductRecord
{
    [JsonPropertyName("skuNo")]
    public string? SkuNo { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("pricePerMonth")]
    public int PricePerMonth { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("subCategory")]
    public string? SubCategory { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("metadata")]
    public ProductMetadataRecord? Metadata { get; set; }

    /// <summary>Only a featured product carries a badge, so the key is omitted rather than written null.</summary>
    [JsonPropertyName("badge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Badge { get; set; }
}
