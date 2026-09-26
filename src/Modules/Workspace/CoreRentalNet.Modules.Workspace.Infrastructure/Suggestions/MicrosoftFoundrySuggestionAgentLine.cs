using System.Text.Json.Serialization;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One line the provider proposed: which SKU in which slot, how many, and why in its own words.</summary>
/// <remarks>
/// The slot is written <b>PascalCase</b> on the wire, as the agent's contract declares, so the converter is
/// attached to this property rather than added globally.
/// </remarks>
internal sealed record MicrosoftFoundrySuggestionAgentLine(
    [property: JsonPropertyName("slot")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<SlotId>))]
    SlotId Slot,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("why")] string Why);
