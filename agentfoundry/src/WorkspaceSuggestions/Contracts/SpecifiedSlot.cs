using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>One slot the customer cares about: how many, and what for, in ordinary words.</summary>
/// <remarks>
/// Carries no SKU, no price and no product name by construction — the title says "specified", not "chosen".
/// The purpose is what the suggestor reasons from, and it stays in words because that is where "close to
/// their query" lives.
/// </remarks>
public sealed record SpecifiedSlot(
    [property: JsonPropertyName("slot")]
    [property: Description("A slot from the request's own vocabulary.")]
    WorkspaceSlot Slot,
    [property: JsonPropertyName("quantity")]
    [property: Description("How many, never above the slot's capacity.")]
    int Quantity,
    [property: JsonPropertyName("purpose")]
    [property: Description("A short purpose in ordinary words - for example 'a wide, stable surface for two monitors'. Never a product, a brand or a price.")]
    string Purpose);
