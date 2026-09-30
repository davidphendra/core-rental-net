namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>One setup the reviewer approved, streamed before the run's ending.</summary>
public sealed record WorkspaceSuggestionCandidateApprovedAgentEvent(WorkspaceSuggestionCandidate Candidate)
    : WorkspaceSuggestionAgentEvent;
