using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;

/// <summary>The end of a run: the status, and nothing a candidate implies.</summary>
/// <remarks>
/// Deliberately separate from the candidates. A caller renders each setup as it arrives and learns the outcome
/// from this event alone, so a candidate's arrival never implies a successful run.
/// </remarks>
public sealed record WorkspaceSuggestionRunCompletedEvent(
    string CustomerWorkflowIdentifier,
    WorkspaceSuggestionRunStatus RunStatus,
    string? OutcomeReason,
    int CompletedAttemptCount,
    AgentRunUsage? RunUsage = null)
    : WorkspaceSuggestionStreamEvent(CustomerWorkflowIdentifier);
