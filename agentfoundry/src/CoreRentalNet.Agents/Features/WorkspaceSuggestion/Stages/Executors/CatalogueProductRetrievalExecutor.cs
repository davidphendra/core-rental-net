using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Searches the catalogue with the caller's own entitlement, and chooses nothing.</summary>
internal sealed class CatalogueProductRetrievalExecutor(
    AIAgent stageAgent,
    McpToolAnswerLedger recordedToolAnswers,
    ILogger logger)
    : WorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.Retriever, logger, modelBacked: true)
{
    protected override object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.CatalogueRetrieval;

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
                try
                {
                    var agentResponse = await stageAgent.RunAsync<CatalogueProductRetrievalResult>(
                        WorkspaceStageInput.Retrieval(workspaceSuggestionWorkflowState),
                        cancellationToken: stageCancellationToken);

                    workspaceSuggestionWorkflowState.CatalogueRetrieval = agentResponse.Result;
                }
                catch (CatalogueUnavailableException)
                {
                    // The catalogue refused the call before a model saw a tool, so there is nothing to search and
                    // nothing to retry: the run ends Unavailable, and the graph's own edge carries it there.
                    workspaceSuggestionWorkflowState.CatalogueRetrieval = new CatalogueProductRetrievalResult(
                        IsAvailable: false,
                        UnavailableReason: "The catalogue could not be reached.",
                        ComponentSearchOutcomes: []);
                }
            },
            cancellationToken);

        // Recorded per attempt, because the state keeps only the attempt in flight and a retry would otherwise
        // hide the first attempt's empty searches, which are the ones a correction is built from.
        WorkspaceWorkflowTelemetry.RecordCatalogue(workspaceSuggestionWorkflowState.CatalogueRetrieval);

        return workspaceSuggestionWorkflowState;
    }
}
