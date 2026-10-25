namespace CoreRentalNet.Host.Mcp;

/// <summary>Compact product facts carried only by the MCP read model.</summary>
/// <remarks>REST answers do not carry these; the catalogue API keeps its published shape.</remarks>
public sealed record CatalogueToolFacts(IReadOnlyDictionary<string, string> Values);
