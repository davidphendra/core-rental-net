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
/// Nothing else crosses: no identity, no address, no draft, no SKU and no price — except the customer's own
/// access token, which the run presents to the catalogue. The agent takes it out of the model's input before
/// the run, so it never reaches the prompt, and the names and amounts a customer sees are the catalogue
/// tool's, stated back by the agent.
/// </para>
/// </remarks>
internal sealed record SuggestionRequest(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("ceilingMonthly")] int? CeilingMonthly,
    [property: JsonPropertyName("slots")] IReadOnlyList<SuggestionSlotRule> Slots,
    [property: JsonPropertyName("mcpAccessToken")] string? McpAccessToken = null);
