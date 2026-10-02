using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;

/// <summary>Searches the catalogue with the caller's own entitlement, and chooses nothing.</summary>
internal sealed class CatalogueProductRetrievalExecutor(
    AIAgent stageAgent,
    McpToolAnswerLedger recordedToolAnswers,
    ILogger logger)
    : WorkspaceSuggestionStreamingStageExecutor(WorkspaceWorkflowExecutorNames.Retriever, logger)
{
    protected override async ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        // An attempt's searches are the only ones its reranker may consider, so the ledger starts empty: an
        // accumulating one would hand attempt two the products attempt one rejected.
        recordedToolAnswers.ClearRecordedAnswers();

        await RunProcessingStageAsync(
            workspaceSuggestionWorkflowState,
            workflowContext,
            WorkspaceProcessingStage.RetrievingCatalogueProducts,
            async stageCancellationToken =>
            {
                var agentResponse = await stageAgent.RunAsync<CatalogueProductRetrievalResult>(
                    WorkspaceStageInput.Retrieval(workspaceSuggestionWorkflowState),
                    cancellationToken: stageCancellationToken);

                workspaceSuggestionWorkflowState.CatalogueRetrieval = agentResponse.Result;
            },
            cancellationToken);

        return workspaceSuggestionWorkflowState;
    }
}
