namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>What a product says about itself: the tokens a criterion is matched against.</summary>
/// <remarks>
/// The published metadata, not a copy of it. This tree holds no catalogue of its own: everything it
/// knows about a product arrives in the answer to one read.
/// </remarks>
public sealed record CatalogueMetadata(
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Attributes);
