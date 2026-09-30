using System.ComponentModel;
using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;

/// <summary>A slot and how many of it a composition may hold, as the request states them.</summary>
public sealed record SlotRule(
    [property: JsonPropertyName("slot")]
    [property: Description("The slot.")]
    WorkspaceSlot Slot,
    [property: JsonPropertyName("capacity")]
    [property: Description("The most this slot may hold.")]
    int Capacity);
