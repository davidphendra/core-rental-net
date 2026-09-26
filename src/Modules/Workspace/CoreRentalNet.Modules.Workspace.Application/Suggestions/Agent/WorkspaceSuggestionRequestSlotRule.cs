using System.Text.Json.Serialization;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>A slot and how many of it a composition may hold, as the agent is told.</summary>
/// <remarks>
/// The slot is written PascalCase on the wire, as the agent's contract declares, so the converter is attached
/// to this property rather than added globally: the application's own API writes its enums in camel case, and
/// the two conventions are both correct in their own places.
/// </remarks>
public sealed record WorkspaceSuggestionRequestSlotRule(
    [property: JsonPropertyName("slot")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<SlotId>))]
    SlotId Slot,
    [property: JsonPropertyName("capacity")] int Capacity);
