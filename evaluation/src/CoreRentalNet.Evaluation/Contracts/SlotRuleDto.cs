using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One slot rule on the wire.</summary>
public sealed record SlotRuleDto(
    [property: JsonPropertyName("slot")] string Slot,
    [property: JsonPropertyName("capacity")] int Capacity);
