namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>One product, as a compact catalogue answer carries it.</summary>
/// <remarks>
/// The fields the agent reads and no others: no image path and no display flags, because it names SKUs
/// and the application resolves pictures. The price is a number, and the currency travels once on the
/// page, because the catalogue holds one currency.
/// </remarks>
public sealed record CatalogueItem(
    string Sku,
    string Name,
    string Category,
    string? SubCategory,
    decimal PricePerMonth,
    string Description,
    CatalogueMetadata Metadata);
