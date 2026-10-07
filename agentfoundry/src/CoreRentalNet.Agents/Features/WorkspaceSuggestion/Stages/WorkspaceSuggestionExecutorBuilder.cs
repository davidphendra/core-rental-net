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
/// <b>The deterministic policies are built here, from the workflow's own options.</b> They were registered in the
/// container as singletons <i>and</i> constructed in the factory, so three of those registrations were dead and
/// the same bounds existed twice. One place constructs them and that place is this one; the options are the
/// single source of what the bounds are.
/// </para>
/// <para>
/// <b>The executors belong to the graph, and the graph outlives the request.</b> The token service, header reader,
/// tool-answer ledger and model-call telemetry they need are the run's, so they are handed the run scope and reach
/// the run's own instances as they run, rather than holding a root-captured copy of them.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionExecutorBuilder(
    WorkspaceSuggestionAgentBuilder agentBuilder,
    IRunScope runScope,
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
                runScope, workflowOptions, logger).BindExecutor(),

            Verifier: new WorkspaceRequestVerificationExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Verifier),
                logger
            ).BindExecutor(),

            Rephraser: new WorkspaceRequirementRephrasingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Rephraser),
                new WorkspaceComponentSearchVocabularyLimitPolicy(
                    workflowOptions.MaximumSearchTermCountPerComponent,
                    workflowOptions.MaximumSearchTermCharacterCount),
                logger
            ).BindExecutor(),

            Retriever: new CatalogueProductRetrievalExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Retriever),
                runScope, logger
            ).BindExecutor(),

            ProductPool: new WorkspaceComponentProductPoolExecutor(
                new WorkspaceComponentProductPoolBuilder(CatalogueSearchToolNames.All),
                new WorkspaceComponentProductPoolPolicy(
                    workflowOptions.MaximumRetrievedProductsPerComponentForReranking),
                runScope, logger
            ).BindExecutor(),

            Reranker: new WorkspaceComponentProductRerankingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reranker),
                new WorkspaceComponentProductSelectionPolicy(
                    workflowOptions.MaximumSelectedProductsPerComponent),
                logger
            ).BindExecutor(),

            Composer: new WorkspaceSetupCompositionExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Composer),
                logger
            ).BindExecutor(),

            Validator: new WorkspaceSetupCandidateValidationExecutor(
                new WorkspaceSetupCandidateStructureValidator(),
                logger
            ).BindExecutor(),

            Reviewer: new WorkspaceSetupReviewExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reviewer),
                logger
            ).BindExecutor(),

            RetryDecision: new WorkspaceSetupRetryDecisionExecutor(
                new WorkspaceSetupRetryDecisionPolicy(workflowOptions.MaximumAttemptCount),
                logger
            ).BindExecutor(),

            Success: new WorkspaceSuggestionSuccessCompletionExecutor(runScope, logger).BindExecutor(),
            Rejected: new WorkspaceSuggestionRejectionCompletionExecutor(runScope, logger).BindExecutor(),
            Unavailable: new WorkspaceSuggestionUnavailableCompletionExecutor(runScope, logger).BindExecutor());
    }
}
