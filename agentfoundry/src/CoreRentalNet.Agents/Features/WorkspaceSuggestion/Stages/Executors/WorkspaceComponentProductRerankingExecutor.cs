using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Orders each component's retrieved products, so the composer chooses from a short good list.</summary>
/// <remarks>
/// It ranks and projects only. The reranker's chosen SKUs are resolved against the candidate pool for their
/// component; that projection is intentionally non-validating.
/// </remarks>
internal sealed class WorkspaceComponentProductRerankingExecutor(
    AIAgent stageAgent,
    ILogger logger)
    : WorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.Reranker, logger, modelBacked: true)
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

                workspaceSuggestionWorkflowState.SelectedWorkspaceCandidates = MapCandidates(
                    workspaceSuggestionWorkflowState.CandidatePool,
                    agentResponse.Result);
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }

    private static IReadOnlyList<SelectedWorkspaceComponentProduct> MapCandidates(
        IReadOnlyList<RetrievedWorkspaceComponentProduct> candidatePool,
        WorkspaceComponentProductRankingResult rankingResult)
    {
        var productsBySku = new Dictionary<string, RetrievedWorkspaceComponentProduct>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var product in candidatePool)
        {
            productsBySku.TryAdd(product.Sku, product);
        }

        var selected = new List<SelectedWorkspaceComponentProduct>();

        foreach (var (_, assessments) in rankingResult.Assessments.EveryComponent())
        {
            foreach (var assessment in assessments)
            {
                if (productsBySku.TryGetValue(assessment.Sku, out var product))
                {
                    selected.Add(new SelectedWorkspaceComponentProduct(product, assessment.Relevance));
                }
            }
        }

        return selected;
    }
}
