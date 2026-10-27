using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Scoping;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Builds the products the reranker may see from what the tools answered.</summary>
/// <remarks>
/// It reads the recorded answers rather than the retriever's own account of them, so the pool holds the tool's
/// bytes. The reranker's own contract is checked once, by the selection policy.
/// </remarks>
internal sealed class WorkspaceComponentProductPoolExecutor(
    WorkspaceComponentProductPoolBuilder poolBuilder,
    IRunScopedServiceResolver runScopedServiceResolver,
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
        workspaceSuggestionWorkflowState.CandidatePool =
            poolBuilder.BuildPoolFrom(runScopedServiceResolver.Resolve<McpToolAnswerLedger>());

        return ValueTask.FromResult(workspaceSuggestionWorkflowState);
    }
}
