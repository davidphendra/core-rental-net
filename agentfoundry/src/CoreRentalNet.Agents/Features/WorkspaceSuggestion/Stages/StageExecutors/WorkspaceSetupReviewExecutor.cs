using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Decides whether the composed setups satisfy the request. Validity only; no orchestration.</summary>
internal sealed class WorkspaceSetupReviewExecutor(AIAgent stageAgent, ILogger logger)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.Reviewer, logger)
{
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

                // A review is what finishes an attempt, so the count moves here and not in the retry
                // decision: an accepted second attempt completed two attempts, and a count that moved only
                // on a rejection would report one.
                workspaceSuggestionWorkflowState.CompletedAttemptCount++;
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
