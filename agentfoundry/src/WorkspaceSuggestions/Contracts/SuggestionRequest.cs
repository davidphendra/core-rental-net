using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>What the application sends: one sentence, the slot rules, and the whole compact catalogue.</summary>
/// <remarks>
/// The wire shape of <c>suggestion.request.schema.json</c>. The request is the agent's entire world —
/// it is tool-less and never calls back — so this type has to carry everything a choice may rest on.
/// </remarks>
public sealed record SuggestionRequest(
    [property: JsonPropertyName("runId")]
    [property: Description("Opaque run identifier, recorded by the application. Not an identity.")]
    string RunId,
    [property: JsonPropertyName("query")]
    [property: Description("The customer's own words, verbatim.")]
    string Query,
    [property: JsonPropertyName("currency")]
    [property: Description("The currency the catalogue is priced in.")]
    string Currency,
    [property: JsonPropertyName("ceilingMonthly")]
    [property: Description("The monthly ceiling the customer named, or null when they named none.")]
    int? CeilingMonthly,
    [property: JsonPropertyName("slots")]
    [property: Description("The slots and their capacities.")]
    IReadOnlyList<SlotRule> Slots,
    [property: JsonPropertyName("catalogue")]
    [property: Description("The whole compact catalogue, description included.")]
    IReadOnlyList<CatalogueItem> Catalogue);
