using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Sources;

/// <summary>One product row from a <c>meta_*.jsonl</c> file, as far as this tool reads it.</summary>
internal sealed class AmazonMetaRecord
{
    [JsonPropertyName("parent_asin")]
    public string? ParentAsin { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public List<string>? Description { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, JsonElement>? Details { get; set; }

    [JsonPropertyName("price")]
    public double? Price { get; set; }

    [JsonPropertyName("average_rating")]
    public double? AverageRating { get; set; }

    [JsonPropertyName("rating_number")]
    public int? RatingNumber { get; set; }

    [JsonPropertyName("store")]
    public string? Store { get; set; }

    [JsonPropertyName("images")]
    public List<AmazonImageRecord>? Images { get; set; }
}
