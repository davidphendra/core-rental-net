using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The JSON contract returned by the local judge.</summary>
public sealed record JudgeVerdictDto(
    [property: JsonPropertyName("verdict")] string? Verdict,
    [property: JsonPropertyName("confidence")] double Confidence,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("selectedSkus")] IReadOnlyList<string>? SelectedSkus,
    [property: JsonPropertyName("missingRequirements")] IReadOnlyList<string>? MissingRequirements);
