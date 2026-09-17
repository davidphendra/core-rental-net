namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>The metadata a caller reads: what a product is for, and what it is not.</summary>
/// <remarks>
/// The published mirror of the module's own vocabulary, exactly as <see cref="CatalogCategory"/>
/// mirrors the domain's categories. A caller matches a request against these tokens; it never sees
/// the record the loader built.
/// </remarks>
public sealed record CatalogMetadata(
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<string> BestFor,
    IReadOnlyList<string> NotFor);
