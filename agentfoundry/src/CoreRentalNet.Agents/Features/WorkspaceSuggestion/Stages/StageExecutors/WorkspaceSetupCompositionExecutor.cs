using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Composes setups from the retrieved products and the specification, with no tools of its own.</summary>
internal sealed class WorkspaceSetupCompositionExecutor(AIAgent stageAgent)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.Composer)
{
    public override async ValueTask<WorkspaceSuggestionWorkflowState> HandleAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.ComposingWorkspaceSetups,
            async stageCancellationToken =>
            {
                var agentResponse = await stageAgent.RunAsync<WorkspaceSetupCandidateSet>(
                    WorkspaceStageInput.Composition(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                workspaceSuggestionWorkflowState.ProposedWorkspaceSetups = agentResponse.Result.Setups;
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
