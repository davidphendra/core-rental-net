namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>What the agent answered, as it answered it.</summary>
public sealed record AgentSuggestion(
    string Status,
    string? Code,
    IReadOnlyList<AgentSuggestionOption> Options,
    IReadOnlyList<string> Findings);
