using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Orders each component's retrieved products, so the composer chooses from a short good list.</summary>
/// <remarks>
/// It selects nothing itself and composes nothing: it hands the model one component's rows at a time and keeps
/// what came back through the selection policy, which is where the two guarantees live — a product cannot be
/// introduced, and a component cannot be lost to a stronger sibling.
/// </remarks>
internal sealed class WorkspaceComponentProductRerankingExecutor(
    AIAgent stageAgent,
    WorkspaceComponentProductSelectionPolicy selectionPolicy,
    ILogger logger)
    : WorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.Reranker, logger)
{
    protected override object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.SelectedWorkspaceCandidates;

    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.RerankingWorkspaceCandidates,
            async stageCancellationToken =>
            {
                var agentResponse = await stageAgent.RunAsync<WorkspaceComponentProductRankingResult>(
                    WorkspaceStageInput.Reranking(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                workspaceSuggestionWorkflowState.SelectedWorkspaceCandidates = selectionPolicy.SelectFromPool(
                    workspaceSuggestionWorkflowState.CandidatePool, agentResponse.Result);
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
