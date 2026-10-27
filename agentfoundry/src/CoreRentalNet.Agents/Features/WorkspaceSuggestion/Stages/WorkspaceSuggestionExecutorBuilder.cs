using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;
using CoreRentalNet.Agents.Shared.Scoping;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;

/// <summary>Builds one workspace workflow's stage executors, and nothing else.</summary>
/// <remarks>
/// <para>
/// <b>The remaining deterministic policies are built here, from the workflow's own options.</b> The only
/// post-reranker validation is the selection policy; the rephraser prompt owns category reading and vocabulary.
/// </para>
/// <para>
/// <b>The executors belong to one request's graph.</b> The token service, header reader, tool-answer ledger and
/// model-call telemetry they need are the run's, so they are handed the run scope and reach the run's own instances
/// as they run, rather than holding a root-captured copy of them. The builder itself is stateless and shared.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionExecutorBuilder(
    WorkspaceSuggestionAgentBuilder agentBuilder,
    IRunScopedServiceResolver runScopedServiceResolver,
    WorkspaceSuggestionWorkflowOptions workflowOptions,
    ILoggerFactory loggerFactory)
{
    /// <summary>The graph's nodes, built and bound, ready for the topology to be laid over them.</summary>
    public WorkspaceSuggestionExecutors Build()
    {
        // One category for every node: a node's own id is the structured property on its two lines, so a
        // category per node would be a second way to say what the line already says.
        var logger = loggerFactory.CreateLogger<WorkspaceSuggestionStreamingExecutor>();

        return new(
            Input: new WorkspaceInputExecutor(
                runScopedServiceResolver, workflowOptions, logger).BindExecutor(),

            Verifier: new WorkspaceRequestVerificationExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Verifier),
                logger
            ).BindExecutor(),

            Rephraser: new WorkspaceRequirementRephrasingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Rephraser),
                new WorkspaceRequirementBudgetInvariantPolicy(),
                new WorkspaceExplicitModelTermPolicy(),
                logger
            ).BindExecutor(),

            Retriever: new CatalogueProductRetrievalExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Retriever),
                new CatalogueProductRetrievalNormalizer(),
                runScopedServiceResolver, logger
            ).BindExecutor(),

            ProductPool: new WorkspaceComponentProductPoolExecutor(
                new WorkspaceComponentProductPoolBuilder(CatalogueSearchToolNames.All),
                runScopedServiceResolver, logger
            ).BindExecutor(),

            Reranker: new WorkspaceComponentProductRerankingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reranker),
                logger
            ).BindExecutor(),

            Composer: new WorkspaceSetupCompositionExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Composer),
                logger
            ).BindExecutor(),

            Success: new WorkspaceSuggestionSuccessCompletionExecutor(runScopedServiceResolver, logger).BindExecutor(),
            Rejected: new WorkspaceSuggestionRejectionCompletionExecutor(runScopedServiceResolver, logger).BindExecutor(),
            Unavailable: new WorkspaceSuggestionUnavailableCompletionExecutor(runScopedServiceResolver, logger).BindExecutor());
    }
}
