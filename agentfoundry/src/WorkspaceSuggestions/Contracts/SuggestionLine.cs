using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>Which SKU goes in which slot, how many, and the model's own words for why.</summary>
/// <remarks>
/// Carries no price: the application resolves every amount. The SKU and the quantity are checked by the
/// application against the catalogue and the slot capacity it sent, all-or-nothing — this type only
/// states them.
/// </remarks>
public sealed record SuggestionLine(
    [property: JsonPropertyName("slot")]
    [property: Description("The slot this line fills.")]
    WorkspaceSlot Slot,
    [property: JsonPropertyName("sku")]
    [property: Description("A SKU from the catalogue the request carried.")]
    string Sku,
    [property: JsonPropertyName("quantity")]
    [property: Description("How many of this SKU, never exceeding the slot's capacity.")]
    int Quantity,
    [property: JsonPropertyName("why")]
    [property: Description("Why this SKU suits the request, in purpose words only — no price, no product name.")]
    string Why);
