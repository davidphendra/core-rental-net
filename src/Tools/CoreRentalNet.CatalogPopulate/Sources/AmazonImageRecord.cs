using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Sources;

/// <summary>One image entry in a source meta row. Only the large form is used.</summary>
internal sealed class AmazonImageRecord
{
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}
