using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;

/// <summary>The monthly price a catalogue tool reports, as the tool publishes it.</summary>
/// <remarks>
/// The catalogue publishes a product's price as an amount and a currency. This mirror keeps that shape instead of
/// flattening it to a decimal, so the agent reads the same contract the Host tool answers with.
/// </remarks>
public sealed record CatalogueSearchToolPrice(
    [property: JsonPropertyName("amount")]
    [property: JsonRequired]
    decimal Amount,
    [property: JsonPropertyName("currency")]
    [property: JsonRequired]
    string Currency);
