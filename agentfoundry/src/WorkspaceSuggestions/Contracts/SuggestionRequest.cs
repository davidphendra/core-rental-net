using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>What the application sends: one sentence, the slot rules, and the currency to think in.</summary>
/// <remarks>
/// The wire shape of <c>suggestion.request.schema.json</c>. <b>The catalogue is deliberately not carried.</b>
/// The agent searches the catalogue for itself through the MCP tools it was given, so this request states what
/// the customer asked for and the rules it must compose within, and nothing else.
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
    IReadOnlyList<SlotRule> Slots);
