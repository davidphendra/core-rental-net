using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>What the application sends: one sentence, the slot rules, and the currency to think in.</summary>
/// <remarks>
/// The wire shape of the agent's own request schema. <b>The catalogue is deliberately not here.</b> The agent
/// searches the catalogue for itself, through the MCP tools the application publishes, so what crosses to the
/// agent is the customer's own sentence and the rules it must compose within - never a pre-selected set of
/// products and never the whole catalogue.
/// </remarks>
public sealed record WorkspaceSuggestionRequestPayload(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("ceilingMonthly")] int? CeilingMonthly,
    [property: JsonPropertyName("slots")] IReadOnlyList<WorkspaceSuggestionRequestSlotRule> Slots,
    [property: JsonPropertyName("mcpAccessToken")] string? McpAccessToken = null);
