namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;

/// <summary>That an attempt was reviewed and another one is beginning.</summary>
public sealed record WorkspaceSetupRetryStartedEvent(
    string CustomerWorkflowIdentifier,
    int NextAttemptNumber,
    int MaximumAttemptCount)
    : WorkspaceSuggestionStreamEvent(CustomerWorkflowIdentifier);
