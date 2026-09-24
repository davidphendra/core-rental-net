using System.Text.Json.Serialization;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Agents;

/// <summary>A slot and how many of it a composition may hold, as the agent is told.</summary>
/// <remarks>
/// The slot is written PascalCase on the wire, as the agent's contract declares — see
/// <see cref="AgentSuggestionLine"/> for why the converter is per property rather than global.
/// </remarks>
internal sealed record SuggestionSlotRule(
    [property: JsonPropertyName("slot")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<SlotId>))]
    SlotId Slot,
    [property: JsonPropertyName("capacity")] int Capacity);
