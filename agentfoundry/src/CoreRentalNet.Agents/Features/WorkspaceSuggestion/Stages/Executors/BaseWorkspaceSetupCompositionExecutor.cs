using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Composes setups from the retrieved products and the specification, with no tools of its own.</summary>
internal sealed class BaseWorkspaceSetupCompositionExecutor(AIAgent stageAgent, ILogger logger)
    : BaseWorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.Composer, logger)
{
    protected override object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.ProposedWorkspaceSetups;

    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
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
