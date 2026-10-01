using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Agents.AI.Workflows;

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
/// Scoped like the run, because the executors it builds hold the run's token service, its tool-answer ledger and
/// its usage accumulator.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionExecutorBuilder(
    WorkspaceSuggestionAgentBuilder agentBuilder,
    IMcpAccessTokenService accessTokens,
    AccessTokenHeaderReader accessTokenHeaderReader,
    WorkspaceSuggestionWorkflowOptions workflowOptions,
    McpToolAnswerLedger recordedToolAnswers,
    AgentRunUsageAccumulator runUsage)
{
    /// <summary>The graph's nodes, built and bound, ready for the topology to be laid over them.</summary>
    public WorkspaceSuggestionExecutors Build()
        => new(
            Input: new WorkspaceInputExecutor(
                accessTokens, accessTokenHeaderReader, workflowOptions).BindExecutor(),

            Verifier: new WorkspaceRequestVerificationExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Verifier)).BindExecutor(),

            Rephraser: new WorkspaceRequirementRephrasingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Rephraser),
                new WorkspaceComponentSearchVocabularyLimitPolicy(
                    workflowOptions.MaximumSearchTermCountPerComponent,
                    workflowOptions.MaximumSearchTermCharacterCount)).BindExecutor(),

            Retriever: new CatalogueProductRetrievalExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Retriever),
                recordedToolAnswers).BindExecutor(),

            ProductPool: new WorkspaceComponentProductPoolExecutor(
                new WorkspaceComponentProductPoolBuilder(CatalogueSearchToolNames.All),
                new WorkspaceComponentProductPoolPolicy(
                    workflowOptions.MaximumRetrievedProductsPerComponentForReranking),
                recordedToolAnswers).BindExecutor(),

            Reranker: new WorkspaceComponentProductRerankingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reranker),
                new WorkspaceComponentProductSelectionPolicy(
                    workflowOptions.MaximumSelectedProductsPerComponent)).BindExecutor(),

            Composer: new WorkspaceSetupCompositionExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Composer)).BindExecutor(),

            Validator: new WorkspaceSetupCandidateValidationExecutor(
                new WorkspaceSetupCandidateStructureValidator()).BindExecutor(),

            Reviewer: new WorkspaceSetupReviewExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reviewer)).BindExecutor(),

            RetryDecision: new WorkspaceSetupRetryDecisionExecutor(
                new WorkspaceSetupRetryDecisionPolicy(workflowOptions.MaximumAttemptCount)).BindExecutor(),

            Success: new WorkspaceSuggestionSuccessCompletionExecutor(runUsage, accessTokens).BindExecutor(),
            Rejected: new WorkspaceSuggestionRejectionCompletionExecutor(runUsage, accessTokens).BindExecutor(),
            Unavailable: new WorkspaceSuggestionUnavailableCompletionExecutor(runUsage, accessTokens).BindExecutor());
}
