using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Catalog;

/// <summary>The metadata object of one products.json row.</summary>
internal sealed class ProductMetadataRecord
{
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    [JsonPropertyName("bestFor")]
    public List<string>? BestFor { get; set; }

    [JsonPropertyName("notFor")]
    public List<string>? NotFor { get; set; }
}
