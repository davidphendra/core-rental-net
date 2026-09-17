using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>What the suggestor sends on: the candidates, and the specification they were built from.</summary>
/// <remarks>
/// The specification travels with them because the reviewer checks the candidates against it - the slot
/// set is meant to be identical across the three, and a reviewer that could not see the specification
/// could only check each option in isolation, which is not the rule.
/// </remarks>
public sealed record Candidates(Specification Specification, IReadOnlyList<SuggestionOption> Options);
