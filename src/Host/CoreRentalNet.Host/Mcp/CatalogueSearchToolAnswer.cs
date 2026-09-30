using CoreRentalNet.Host.Presentation;

namespace CoreRentalNet.Host.Mcp;

/// <summary>What a catalogue search tool answers: the compact matches, and what a monthly ceiling excluded.</summary>
/// <remarks>
/// <para>
/// <b>An envelope of its own rather than two more nullable fields on
/// <see cref="CompactCatalogCollection"/>.</b> The fact belongs to the search boundary rather than to the
/// products, and the compact view is the REST API's published answer — a field added for the tools would
/// appear in the API's answer as well, which is the shape a page is written against.
/// </para>
/// <para>
/// It lives beside the tools rather than in <c>Presentation</c> so that folder keeps holding published shapes
/// only. It is public because a tool's method is public — the tool boundary has to be able to name its own
/// answer.
/// </para>
/// </remarks>
/// <param name="Matches">The compact answer the catalogue's own API would give for the same search.</param>
/// <param name="CheapestProductIgnoringTheCeiling">The cheapest product the ceiling excluded, or null when the
/// ceiling excluded nothing — which is also the answer when no ceiling was given.</param>
public sealed record CatalogueSearchToolAnswer(
    CompactCatalogCollection Matches,
    decimal? CheapestProductIgnoringTheCeiling);
