using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;

/// <summary>One product a catalogue tool answered with, as the tool stated it.</summary>
/// <remarks>
/// <b>A mirror, not a reference.</b> The agent reaches the catalogue over MCP and cannot depend on the module that
/// publishes this shape, so the two solutions each declare it. What matters is that the description survives:
/// it is the text that says what a product is, and the text nothing before the reranker reads.
/// </remarks>
public sealed record CatalogueSearchToolItem(
    [property: JsonPropertyName("sku")]
    [property: JsonRequired]
    string Sku,
    [property: JsonPropertyName("category")]
    [property: JsonRequired]
    string Category,
    [property: JsonPropertyName("name")]
    [property: JsonRequired]
    string Name,
    [property: JsonPropertyName("subCategory")]
    string? SubCategory,
    [property: JsonPropertyName("description")]
    [property: JsonRequired]
    string Description,
    [property: JsonPropertyName("pricePerMonth")]
    [property: JsonRequired]
    decimal PricePerMonth);
