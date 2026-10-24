using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>The monthly price shown in one catalogue answer.</summary>
public sealed record CataloguePriceDto(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string? Currency);
