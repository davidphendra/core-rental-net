using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;
using CoreRentalNet.Agents.Shared.Workflows;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>One stage of the workspace pipeline, announcing itself around its work.</summary>
/// <remarks>
/// The state and the event union are fixed once here, so every workspace stage publishes through the union and
/// none of them has to remember that it must.
/// </remarks>
internal abstract class WorkspaceSuggestionStreamingStageExecutor(string executorName)
    : StreamingStageExecutor<WorkspaceSuggestionWorkflowState, WorkspaceSuggestionStreamEvent>(executorName)
{
    /// <summary>Announces a stage, runs its work, and announces that it finished.</summary>
    protected static async ValueTask RunProcessingStageAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        WorkspaceProcessingStage processingStage,
        Func<CancellationToken, ValueTask> processingStageWork,
        CancellationToken cancellationToken)
    {
        workspaceSuggestionWorkflowState.CurrentProcessingStage = processingStage;

        await PublishStreamEventAsync(
            workflowContext,
            new WorkspaceProcessingStageStartedEvent(
                workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, processingStage),
            cancellationToken);

        await processingStageWork(cancellationToken);

        await PublishStreamEventAsync(
            workflowContext,
            new WorkspaceProcessingStageCompletedEvent(
                workspaceSuggestionWorkflowState.CustomerWorkflowIdentifier, processingStage),
            cancellationToken);
    }
}
