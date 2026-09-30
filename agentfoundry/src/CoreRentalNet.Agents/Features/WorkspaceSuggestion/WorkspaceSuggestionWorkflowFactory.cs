using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.StageExecutors;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>Builds the workspace workflow graph, once per request.</summary>
/// <remarks>
/// <para>
/// The graph is explicit because the pipeline branches: the verifier ends a run that is not about a workspace,
/// the structural validator and the reviewer each decide a different next step, and a rejected setup set returns
/// to the rephraser. The retry goes back to <b>rephrasing</b> and not to composition, because a setup set that
/// failed review means the sentence has to be read differently rather than composed again from the same reading.
/// </para>
/// <para>
/// Nothing here wraps the workflow itself: hosting can only redirect a hosted workflow's checkpoints when the
/// agent it resolves is the workflow agent, so each stage agent is wrapped in its own decorators and the
/// workflow is served bare.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionWorkflowFactory(
    IChatClient chatClient,
    IMcpAccessTokenService accessTokens,
    CallerAccessTokenHeaderReader callerAccessTokenHeaderReader,
    CallerAuthorisedMcpConnection catalogue,
    WorkspaceSuggestionWorkflowOptions workflowOptions,
    WorkspaceComponentSearchVocabularyLimitPolicy vocabularyLimitPolicy,
    McpToolAnswerLedger recordedToolAnswers,
    AgentRunUsageAccumulator runUsage,
    ILoggerFactory loggerFactory)
{
    public Workflow BuildWorkspaceSuggestionWorkflow()
    {
        var input = new WorkspaceInputExecutor(
            accessTokens,
            callerAccessTokenHeaderReader,
            workflowOptions).BindExecutor();

        var verifier = new WorkspaceRequestVerificationExecutor(StageAgent(WorkspaceSuggestionAgentRoster.Verifier)).BindExecutor();
        var rephraser = new WorkspaceRequirementRephrasingExecutor(
            StageAgent(WorkspaceSuggestionAgentRoster.Rephraser),
            vocabularyLimitPolicy).BindExecutor();
        var retriever = new CatalogueProductRetrievalExecutor(
            StageAgent(WorkspaceSuggestionAgentRoster.Retriever),
            recordedToolAnswers).BindExecutor();
        var composer = new WorkspaceSetupCompositionExecutor(StageAgent(WorkspaceSuggestionAgentRoster.Composer)).BindExecutor();
        var reviewer = new WorkspaceSetupReviewExecutor(StageAgent(WorkspaceSuggestionAgentRoster.Reviewer)).BindExecutor();

        var productPool = new WorkspaceComponentProductPoolExecutor(
            new WorkspaceComponentProductPoolBuilder(CatalogueSearchToolNames.All),
            new WorkspaceComponentProductPoolPolicy(workflowOptions.MaximumRetrievedProductsPerComponentForReranking),
            recordedToolAnswers).BindExecutor();
        var reranker = new WorkspaceComponentProductRerankingExecutor(
            StageAgent(WorkspaceSuggestionAgentRoster.Reranker),
            new WorkspaceComponentProductSelectionPolicy(workflowOptions.MaximumSelectedProductsPerComponent)).BindExecutor();
        var validator = new WorkspaceSetupCandidateValidationExecutor(
            new WorkspaceSetupCandidateStructureValidator()).BindExecutor();
        var retryDecision = new WorkspaceSetupRetryDecisionExecutor(
            new WorkspaceSetupRetryDecisionPolicy(workflowOptions.MaximumAttemptCount)).BindExecutor();

        var success = new WorkspaceSuggestionSuccessCompletionExecutor(runUsage, accessTokens).BindExecutor();
        var rejected = new WorkspaceSuggestionRejectionCompletionExecutor(runUsage, accessTokens).BindExecutor();
        var unavailable = new WorkspaceSuggestionUnavailableCompletionExecutor(runUsage, accessTokens).BindExecutor();

        return new WorkflowBuilder(input)
            .AddEdge(input, verifier)
            .AddSwitch(verifier, verificationSwitch => verificationSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.RequestVerification?.IsWorkspaceRequest is true, [rephraser])
                .WithDefault([rejected]))
            .AddEdge(rephraser, retriever)
            .AddEdge(retriever, productPool)
            .AddEdge(productPool, reranker)
            .AddEdge(reranker, composer)
            .AddEdge(composer, validator)
            .AddSwitch(validator, validationSwitch => validationSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.IsWorkspaceSetupStructureValid is true, [reviewer])
                .WithDefault([retryDecision]))
            .AddSwitch(reviewer, reviewSwitch => reviewSwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.WorkspaceSetupReview?.IsAcceptable is true, [success])
                .WithDefault([retryDecision]))
            .AddSwitch(retryDecision, retrySwitch => retrySwitch
                .AddCase<WorkspaceSuggestionWorkflowState>(
                    state => state?.RetryDecision is WorkspaceSetupRetryDecision.RetryWithRephrasing, [rephraser])
                .WithDefault([unavailable]))
            .WithOutputFrom(
                input, verifier, rephraser, retriever, productPool, reranker, composer,
                validator, reviewer, retryDecision, success, rejected, unavailable)
            .WithName(WorkspaceSuggestionWorkflowName)
            .Build();
    }

    /// <summary>The name the workflow is built under, for a trace rather than for a request.</summary>
    public const string WorkspaceSuggestionWorkflowName = "workspace-suggestion-workflow";

    private AIAgent StageAgent(AgentProfile stageAgentProfile)
        => AgentFactory.Build(stageAgentProfile, StageChatClient(stageAgentProfile));

    /// <summary>The call's chat client for one stage, with the cross-cutting concerns composed in.</summary>
    /// <remarks>
    /// Outside in: the catalogue decorator offers the tools this call's token entitles; the function
    /// loop resolves the tools the model calls; telemetry records what the call cost; the guardrail stops an
    /// answer that repeated the caller's token; and the model client is last. The order matters: a decorator
    /// below the function loop cannot be reached for a tool-calling turn.
    /// </remarks>
    private IChatClient StageChatClient(AgentProfile stageAgentProfile)
        => new CallerAuthorisedMcpChatClient(
            new FunctionInvokingChatClient(
                new ModelCallTelemetryChatClient(
                    new ModelOutputGuardrailChatClient(chatClient, accessTokens),
                    stageAgentProfile.Name,
                    runUsage,
                    loggerFactory.CreateLogger<ModelCallTelemetryChatClient>()),
                loggerFactory),
            accessTokens,
            catalogue,
            stageAgentProfile.UsesCatalogueTools,
            recordedToolAnswers,
            loggerFactory.CreateLogger<CallerAuthorisedMcpChatClient>());
}
