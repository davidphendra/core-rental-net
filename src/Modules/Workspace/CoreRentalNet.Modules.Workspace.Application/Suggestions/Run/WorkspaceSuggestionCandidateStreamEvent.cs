namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>One approved setup, streamed before the run's terminal frame.</summary>
public sealed record WorkspaceSuggestionCandidateStreamEvent(string CandidateWords) : WorkspaceSuggestionStreamEvent;
