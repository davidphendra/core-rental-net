using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Shared.Mcp;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Builds the products the reranker may see from what the tools answered, and bounds them.</summary>
/// <remarks>
/// It reads the recorded answers rather than the retriever's own account of them, so the pool holds the tool's
/// bytes — and it bounds per component, so no component is squeezed out by a sibling whose terms matched more.
/// </remarks>
internal sealed class WorkspaceComponentProductPoolExecutor(
    WorkspaceComponentProductPoolBuilder poolBuilder,
    WorkspaceComponentProductPoolPolicy poolPolicy,
    McpToolAnswerLedger recordedToolAnswers,
    ILogger logger)
    : WorkspaceSuggestionStreamingExecutor(WorkspaceWorkflowExecutorNames.ProductPool, logger)
{
    protected override object? ResultOf(WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState)
        => workspaceSuggestionWorkflowState.CandidatePool;

    protected override ValueTask<WorkspaceSuggestionWorkflowState> ProcessAsync(
        WorkspaceSuggestionWorkflowState workspaceSuggestionWorkflowState,
        IWorkflowContext workflowContext,
        CancellationToken cancellationToken = default)
    {
        workspaceSuggestionWorkflowState.CandidatePool = poolPolicy.ApplyPoolBounds(
            poolBuilder.BuildPoolFrom(recordedToolAnswers));

        return ValueTask.FromResult(workspaceSuggestionWorkflowState);
    }
}
