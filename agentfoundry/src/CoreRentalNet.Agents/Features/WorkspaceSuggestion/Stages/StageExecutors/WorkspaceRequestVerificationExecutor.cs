using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Decides whether the sentence is a workspace request at all, and ends the run when it is not.</summary>
internal sealed class WorkspaceRequestVerificationExecutor(AIAgent stageAgent)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.Verifier)
{
    public override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.VerifyingRequest,
            async stageCancellationToken =>
            {
                var agentResponse = await stageAgent.RunAsync<WorkspaceRequestVerificationResult>(
                    WorkspaceStageInput.Verification(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                workspaceSuggestionWorkflowState.RequestVerification = agentResponse.Result;
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
