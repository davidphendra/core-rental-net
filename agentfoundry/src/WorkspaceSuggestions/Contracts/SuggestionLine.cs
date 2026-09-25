using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>Which SKU goes in which slot, how many, and the model's own words for why.</summary>
/// <remarks>
/// The name and the amount are the catalogue tool's, stated so the customer can read them; nothing the model
/// invents reaches the page. The application sums the lines for the candidate's total rather than asking the
/// model to add.
/// </remarks>
public sealed record SuggestionLine(
    [property: JsonPropertyName("slot")]
    [property: Description("The slot this line fills.")]
    WorkspaceSlot Slot,
    [property: JsonPropertyName("sku")]
    [property: Description("A SKU from a catalogue search result.")]
    string Sku,
    [property: JsonPropertyName("name")]
    [property: Description("The product's name, exactly as the catalogue tool returned it.")]
    string Name,
    [property: JsonPropertyName("quantity")]
    [property: Description("How many of this SKU, never exceeding the slot's capacity.")]
    int Quantity,
    [property: JsonPropertyName("amount")]
    [property: Description("This line's total for its quantity, taken from the price the catalogue tool returned.")]
    decimal Amount,
    [property: JsonPropertyName("why")]
    [property: Description("Why this SKU suits the request, in purpose words only - no price, no product name.")]
    string Why);
