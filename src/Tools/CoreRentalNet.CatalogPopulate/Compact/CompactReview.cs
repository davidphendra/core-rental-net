using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Compact;

/// <summary>One review kept in the compact input, with only the fields the model is shown.</summary>
internal sealed record CompactReview(
    [property: JsonPropertyName("rating")] double? Rating,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("helpfulVote")] int? HelpfulVote,
    [property: JsonPropertyName("timestamp")] long? Timestamp);
