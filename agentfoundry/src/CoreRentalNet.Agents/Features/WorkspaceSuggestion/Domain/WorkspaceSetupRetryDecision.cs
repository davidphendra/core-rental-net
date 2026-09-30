namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>What the workflow does after one attempt's review.</summary>
public enum WorkspaceSetupRetryDecision
{
    Accepted,
    RetryWithRephrasing,
    AttemptsExhausted,
}
