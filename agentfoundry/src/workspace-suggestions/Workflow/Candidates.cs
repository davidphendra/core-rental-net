using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>
/// What the suggestor sends on: the candidates, the specification they were built from, and the
/// candidates each slot was filled from.
/// </summary>
/// <remarks>
/// The ordered SKUs travel with them so the reviewer can re-derive each tier's pick and compare it with
/// what was chosen. Without them the validator could only check the composition against itself, and a
/// wrong pick - the one failure that changes what a customer is offered without changing anything a
/// reader could notice - would pass.
/// </remarks>
public sealed record Candidates(
    Specification Specification,
    IReadOnlyList<SuggestionOption> Options,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Ordered);
