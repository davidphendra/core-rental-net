using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Sources;

/// <summary>One review row from a review category file, as far as this tool reads it.</summary>
internal sealed class AmazonReviewRecord
{
    [JsonPropertyName("parent_asin")]
    public string? ParentAsin { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("helpful_vote")]
    public int? HelpfulVote { get; set; }

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }
}
