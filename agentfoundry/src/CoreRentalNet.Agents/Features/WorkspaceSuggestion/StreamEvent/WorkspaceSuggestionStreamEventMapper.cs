using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.StreamEvent;

/// <summary>Turns the run's own state into the events a caller reads, in one place.</summary>
/// <remarks>
/// The mapper is the only code that decides <b>what</b> a change becomes; <see cref="WorkflowOutputPublisher"/>
/// is the only code that decides how it is put on the wire. Keeping them apart means the event vocabulary can
/// change without any stage knowing how an event is framed, and a stage can publish without constructing an
/// event itself.
/// </remarks>
internal static class WorkspaceSuggestionStreamEventMapper
{
    public static WorkspaceProcessingStageStartedEvent StageStarted(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState, WorkspaceProcessingStage processingStage)
        => new(workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, processingStage);

    public static WorkspaceProcessingStageCompletedEvent StageCompleted(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState, WorkspaceProcessingStage processingStage)
        => new(workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, processingStage);

    public static WorkspaceSetupCandidateApprovedEvent CandidateApproved(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState, WorkspaceSetupCandidate approvedWorkspaceSetup)
        => new(workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, approvedWorkspaceSetup);

    public static WorkspaceSetupRetryStartedEvent RetryStarted(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => new(
            workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier,
            workspaceSuggestionWorkflowState.NextAttemptNumber,
            workspaceSuggestionWorkflowState.MaximumAttemptCount);

    /// <summary>The run's ending, with the reason derived from how it ended rather than stored twice.</summary>
    public static WorkspaceSuggestionRunCompletedEvent RunCompleted(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState, AgentRunUsage runUsage)
        => new(
            workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier,
            workspaceSuggestionWorkflowState.RunStatus,
            OutcomeReasonFor(workspaceSuggestionWorkflowState),
            workspaceSuggestionWorkflowState.CompletedAttemptCount,
            runUsage);

    /// <summary>Why the run ended, in one line, or null when a rejection already said nothing.</summary>
    private static string? OutcomeReasonFor(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.RunStatus switch
        {
            WorkspaceSuggestionRunStatus.Rejected
                => workspaceSuggestionWorkflowState.RequestVerification?.RefusalReason,
            WorkspaceSuggestionRunStatus.Unavailable
                => workspaceSuggestionWorkflowState.CatalogueRetrieval is { IsAvailable: false } unavailableRetrieval
                    ? unavailableRetrieval.UnavailableReason
                    : "No workspace setup satisfying the request could be composed.",
            _ => null,
        };
}
