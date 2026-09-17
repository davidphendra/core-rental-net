namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>One answer from the catalogue: the products, how many matched, and whether that is all of them.</summary>
/// <remarks>
/// <c>Total</c> and <c>Truncated</c> are what make a partial answer detectable. A slot's tier is the
/// product at the median position of its candidates, so a set that arrived short does not change an
/// answer - it changes which answer is right, silently. Measured: eight monitors give 300/400/475, and
/// five give 300/350/400.
/// </remarks>
public sealed record CataloguePage(
    IReadOnlyList<CatalogueItem> Value,
    int Count,
    int Total,
    bool Truncated,
    string Currency);
