using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Events;
/// <summary>That a processing stage has begun.</summary>
public sealed record WorkspaceProcessingStageStartedEvent(
    string CustomerWorkflowIdentifier,
    WorkspaceProcessingStage ProcessingStage)
    : WorkspaceSuggestionStreamEvent(CustomerWorkflowIdentifier);
