using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.Agents;

/// <summary>What the application sends: one sentence, the slot rules, and the currency to think in.</summary>
/// <remarks>
/// <para>
/// The wire shape of <c>suggestion.request.schema.json</c>. <b>The catalogue is deliberately not here.</b> The
/// agent searches the catalogue for itself, through the MCP tools the application publishes at <c>/mcp</c>, so
/// what crosses to Foundry is the customer's own sentence and the rules it must compose within — never a
/// pre-selected set of products and never the whole catalogue.
/// </para>
/// <para>
/// Nothing else crosses: no identity, no address, no draft, no SKU and no price. The names and amounts a
/// customer finally sees are resolved by the application from its own catalogue, which is the rule the
/// validator enforces on the way back.
/// </para>
/// </remarks>
internal sealed record SuggestionRequest(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("ceilingMonthly")] int? CeilingMonthly,
    [property: JsonPropertyName("slots")] IReadOnlyList<SuggestionSlotRule> Slots);
