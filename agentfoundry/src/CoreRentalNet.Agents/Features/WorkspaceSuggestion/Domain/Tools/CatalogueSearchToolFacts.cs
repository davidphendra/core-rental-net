namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;

/// <summary>Compact catalogue facts published beside a product in a tool answer.</summary>
public sealed record CatalogueSearchToolFacts(IReadOnlyDictionary<string, string> Values);
