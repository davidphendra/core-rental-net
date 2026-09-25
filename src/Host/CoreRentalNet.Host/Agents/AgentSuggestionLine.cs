using System.Text.Json.Serialization;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Agents;

/// <summary>One line the agent proposed: which SKU in which slot, how many, and why in its own words.</summary>
/// <remarks>
/// The slot is <see cref="SlotId"/> and is written <b>PascalCase</b> on the wire — <c>"Desk"</c>, not
/// <c>"desk"</c> — because that is the vocabulary the agent's contract declares. The application's own API
/// writes its enums in camel case, so the converter is attached to this property rather than added globally:
/// the two conventions are both correct, in their own places.
///
/// <see cref="Name"/> and <see cref="Amount"/> are the catalogue's, stated by the agent from the tool result
/// it was given; the application adds the amounts up rather than asking the model to.
/// </remarks>
internal sealed record AgentSuggestionLine(
    [property: JsonPropertyName("slot")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<SlotId>))]
    SlotId Slot,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("why")] string Why);
