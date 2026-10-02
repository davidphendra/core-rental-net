namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>One setup the reviewer approved, streamed before the run's ending.</summary>
public sealed record WorkspaceSuggestionCandidateApprovedEvent(WorkspaceSuggestionCandidate Candidate)
    : WorkspaceSuggestionEvent;
