using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One line of an approved setup on the wire.</summary>
public sealed record CandidateLineDto(
    [property: JsonPropertyName("slot")] string Slot,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("amount")] decimal Amount);
