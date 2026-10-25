namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>Compact, catalogue-backed facts a model may use when choosing or reviewing a product.</summary>
/// <remarks>
/// The facts are a projection of the catalogue's search metadata, not a second source of truth. They are
/// deliberately separate from <see cref="ProductDto"/> on the REST boundary and are carried only by the MCP
/// read model, where a model needs structured evidence rather than a longer description.
/// </remarks>
public sealed record ProductFacts(IReadOnlyDictionary<string, string> Values);
