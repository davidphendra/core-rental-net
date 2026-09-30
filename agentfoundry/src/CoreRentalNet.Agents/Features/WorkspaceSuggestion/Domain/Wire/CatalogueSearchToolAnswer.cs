using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;

/// <summary>What a catalogue search tool answered: the rows, and what a monthly ceiling excluded.</summary>
/// <remarks>
/// The ceiling's consequence is stated beside the rows rather than inside them, because it is a fact about the
/// search and not about a product — and it is the only place the cheapest product a budget excluded is named,
/// which is what a retry needs to correct an allocation.
/// </remarks>
public sealed record CatalogueSearchToolAnswer(
    [property: JsonPropertyName("matches")]
    [property: JsonRequired]
    CatalogueSearchToolMatches Matches,
    [property: JsonPropertyName("cheapestProductIgnoringTheCeiling")]
    decimal? CheapestProductIgnoringTheCeiling);
