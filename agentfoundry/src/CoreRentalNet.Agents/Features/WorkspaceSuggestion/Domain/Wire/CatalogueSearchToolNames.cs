namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;

/// <summary>The catalogue's search tools, as the agent addresses them.</summary>
/// <remarks>
/// <b>Two solutions, one contract, so the names are stated on both sides.</b> The application publishes them
/// (<c>SearchCatalogueTool</c> and <c>SearchSimilarityCatalogueTool</c> in the Host), and the agent has to
/// recognise an answer to read it — it cannot reference the solution that declared them. The shape those answers
/// take is pinned by <c>WorkspaceComponentProductPoolBuilderTests</c>, which is what keeps the two halves in step.
/// </remarks>
public static class CatalogueSearchToolNames
{
    /// <summary>The catalogue's name search, for the terms a requirement was expanded into.</summary>
    public const string NameSearch = "search_catalogue";

    /// <summary>The catalogue's meaning search, for a described need.</summary>
    public const string MeaningSearch = "search_similarity_catalogue";

    /// <summary>Both, for a registration that has to name them once.</summary>
    public static IReadOnlyList<string> All { get; } = [NameSearch, MeaningSearch];
}
