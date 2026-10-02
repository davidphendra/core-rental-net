namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>That an attempt was rejected and another is beginning.</summary>
public sealed record WorkspaceSuggestionRetryEvent(int NextAttemptNumber, int MaximumAttemptCount)
    : WorkspaceSuggestionEvent;
