using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.ChatClients;
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
/// Scoped like the run, because the executors it builds hold the run's token service, its tool-answer ledger and
/// its model-call telemetry.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionExecutorBuilder(
    WorkspaceSuggestionAgentBuilder agentBuilder,
    IMcpAccessTokenService accessTokens,
    AccessTokenHeaderReader accessTokenHeaderReader,
    WorkspaceSuggestionWorkflowOptions workflowOptions,
    McpToolAnswerLedger recordedToolAnswers,
    ITelemetryChatClient telemetryChatClient,
    ILoggerFactory loggerFactory)
{
    /// <summary>The graph's nodes, built and bound, ready for the topology to be laid over them.</summary>
    public WorkspaceSuggestionExecutors Build()
    {
        // One category for every node: a node's own id is the structured property on its two lines, so a
        // category per node would be a second way to say what the line already says.
        var logger = loggerFactory.CreateLogger<BaseWorkspaceSuggestionStreamingExecutor>();

        return new(
            Input: new WorkspaceInputExecutor(
                accessTokens, accessTokenHeaderReader, workflowOptions).BindExecutor(),

            Verifier: new BaseWorkspaceRequestVerificationExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Verifier),
                logger
            ).BindExecutor(),

            Rephraser: new BaseWorkspaceRequirementRephrasingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Rephraser),
                new WorkspaceComponentSearchVocabularyLimitPolicy(
                    workflowOptions.MaximumSearchTermCountPerComponent,
                    workflowOptions.MaximumSearchTermCharacterCount),
                logger
            ).BindExecutor(),

            Retriever: new CatalogueProductRetrievalExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Retriever),
                recordedToolAnswers, logger
            ).BindExecutor(),

            ProductPool: new BaseWorkspaceComponentProductPoolExecutor(
                new WorkspaceComponentProductPoolBuilder(CatalogueSearchToolNames.All),
                new WorkspaceComponentProductPoolPolicy(
                    workflowOptions.MaximumRetrievedProductsPerComponentForReranking),
                recordedToolAnswers, logger
            ).BindExecutor(),

            Reranker: new BaseWorkspaceComponentProductRerankingExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reranker),
                new WorkspaceComponentProductSelectionPolicy(
                    workflowOptions.MaximumSelectedProductsPerComponent),
                logger
            ).BindExecutor(),

            Composer: new BaseWorkspaceSetupCompositionExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Composer),
                logger
            ).BindExecutor(),

            Validator: new BaseWorkspaceSetupCandidateValidationExecutor(
                new WorkspaceSetupCandidateStructureValidator(),
                logger
            ).BindExecutor(),

            Reviewer: new BaseWorkspaceSetupReviewExecutor(
                agentBuilder.For(WorkspaceSuggestionAgentRoster.Reviewer),
                logger
            ).BindExecutor(),

            RetryDecision: new BaseWorkspaceSetupRetryDecisionExecutor(
                new WorkspaceSetupRetryDecisionPolicy(workflowOptions.MaximumAttemptCount),
                logger
            ).BindExecutor(),

            Success: new BaseWorkspaceSuggestionSuccessCompletionExecutor(telemetryChatClient, accessTokens, logger).BindExecutor(),
            Rejected: new BaseWorkspaceSuggestionRejectionCompletionExecutor(telemetryChatClient, accessTokens, logger).BindExecutor(),
            Unavailable: new BaseWorkspaceSuggestionUnavailableCompletionExecutor(telemetryChatClient, accessTokens, logger).BindExecutor());
    }
}
