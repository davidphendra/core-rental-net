using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>The candidates, and the ordered SKUs each slot was filled from.</summary>
/// <remarks>
/// The second half is the receipt the reviewer re-derives the tiers from. It travels with the options
/// rather than being recomputed there, because only this component holds the catalogue - and a receipt
/// the reviewer produced itself would check nothing.
/// </remarks>
public sealed record Selection(
    IReadOnlyList<SuggestionOption> Options,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Ordered);
