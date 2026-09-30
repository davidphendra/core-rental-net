namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>That an attempt was rejected and another is beginning.</summary>
public sealed record WorkspaceSuggestionRetryAgentEvent(int NextAttemptNumber, int MaximumAttemptCount)
    : WorkspaceSuggestionAgentEvent;
