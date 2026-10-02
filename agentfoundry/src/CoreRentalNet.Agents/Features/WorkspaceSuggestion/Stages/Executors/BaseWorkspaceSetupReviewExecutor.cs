using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Decides whether the composed setups satisfy the request. Validity only; no orchestration.</summary>
internal sealed class BaseWorkspaceSetupReviewExecutor(AIAgent stageAgent, ILogger logger)
    : BaseWorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.Reviewer, logger)
{
    protected override object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.WorkspaceSetupReview;

    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.ReviewingWorkspaceSetups,
            async stageCancellationToken =>
            {
                var agentResponse = await stageAgent.RunAsync<WorkspaceSetupReviewResult>(
                    WorkspaceStageInput.Review(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                workspaceSuggestionWorkflowState.WorkspaceSetupReview = agentResponse.Result;
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
