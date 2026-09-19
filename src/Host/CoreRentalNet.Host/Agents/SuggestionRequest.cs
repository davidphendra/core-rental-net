using System.Text.Json.Serialization;
using CoreRentalNet.Host.Presentation;

namespace CoreRentalNet.Host.Agents;

/// <summary>What the application sends: one sentence, the slot rules, and the whole compact catalogue.</summary>
/// <remarks>
/// The wire shape of <c>suggestion.request.schema.json</c>. The catalogue is
/// <see cref="CompactCatalogItem"/> — the projection the application already publishes, description included,
/// built in process from the catalogue it loaded. Nothing else crosses: no identity, no address, no draft.
/// </remarks>
internal sealed record SuggestionRequest(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("ceilingMonthly")] int? CeilingMonthly,
    [property: JsonPropertyName("slots")] IReadOnlyList<SuggestionSlotRule> Slots,
    [property: JsonPropertyName("catalogue")] IReadOnlyList<CompactCatalogItem> Catalogue);
