namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>The agent's answer as it crossed the boundary: a status, an optional code, and what it composed.</summary>
public sealed record AgentSuggestion(
    string Status,
    string? Code,
    IReadOnlyList<AgentSuggestionOption> Options,
    IReadOnlyList<AgentFinding> Findings);
