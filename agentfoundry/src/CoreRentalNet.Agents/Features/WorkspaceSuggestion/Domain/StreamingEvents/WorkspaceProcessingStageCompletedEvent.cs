using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;

/// <summary>That a processing stage has finished.</summary>
public sealed record WorkspaceProcessingStageCompletedEvent(
    string CustomerWorkflowIdentifier,
    WorkspaceProcessingStage ProcessingStage)
    : WorkspaceSuggestionStreamEvent(CustomerWorkflowIdentifier);
