using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Guardrails.Pipeline;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Guardrails;
using CoreRentalNet.Agents.Shared.Guardrails.Arguments;
using CoreRentalNet.Agents.Shared.Guardrails.Results;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>Everything the workspace-suggestion feature needs to serve its agent, in one place.</summary>
/// <remarks>
/// The composition root names the feature, not its parts. What a run needs that belongs to this feature — its
/// graph, its stages, the MCP server its retriever searches, its bounds — is registered here, and what is shared
/// with every other feature is registered by the host.
/// </remarks>
public static class WorkspaceSuggestionRegistration
{
    /// <summary>The configuration key the catalogue's endpoint is read from.</summary>
    public const string CatalogueEndpointKey = "CatalogTools:McpEndpoint";

    /// <summary>How large a tool answer may be before it is truncated for the model.</summary>
    private const int MaximumToolResultCharacters = 100_000;

    /// <summary>Registers the feature, and answers the agent name it serves.</summary>
    /// <remarks>
    /// The identity is handed in rather than read here: the composition root reads configuration once, so the
    /// name hosting resolves the agent under and the name the startup report prints come from one value.
    /// </remarks>
    internal static string AddWorkspaceSuggestionFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        WorkspaceSuggestionAgentIdentity workspaceSuggestionAgentIdentity,
        IChatClient modelClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(workspaceSuggestionAgentIdentity);
        ArgumentNullException.ThrowIfNull(modelClient);

        services.AddSingleton(workspaceSuggestionAgentIdentity);
        services.AddSingleton(McpSetting.FromConfiguration(configuration, CatalogueEndpointKey));
        var workflowOptions = configuration.GetSection("WorkspaceSuggestionWorkflow")
            .Get<WorkspaceSuggestionWorkflowOptions>() ?? new WorkspaceSuggestionWorkflowOptions();

        services.AddSingleton(workflowOptions);

        // The deterministic policies - the pool's bound, the selection's, the vocabulary's - are built by the
        // executor builder from these options, which is the one source of what the bounds are. Registering them
        // here as well would be a second source, and the two would be free to disagree.

        // Where a run's catalogue token is read from: the invocation's own headers, which the hosting layer
        // forwards for names carrying its client prefix. Nothing carries one in a message any more, so there is
        // no splitter to register and no member on the request for a splitter to recognise.
        services.AddScoped<AccessTokenHeaderReader>();
        services.AddScoped<IAccessTokenHeaderReader>(provider =>
            provider.GetRequiredService<AccessTokenHeaderReader>());

        // The run's token holder, scoped like the request. The header reader fills it at the first tool call,
        // and the run ending releases it while the catalogue session is still open.
        services.AddScoped<IMcpAccessTokenService, McpAccessTokenService>();

        // The run-scoped chain: the token-leak guardrail, then the model client. One instance records every
        // stage's call, so the totals are the run's, and it names the stage from the options its agent set.
        // The description limiter is inside the telemetry client, so the evaluation trace and the ledger still
        // see the full tool result while the retriever model receives the shortened copy.
        services.AddScoped<ITelemetryChatClient>(provider =>
        {
            var modelFacingClient = new CatalogueToolDescriptionLimitChatClient(
                new GuardrailChatClient(
                    modelClient,
                    provider.GetRequiredService<IMcpAccessTokenService>()),
                provider.GetRequiredService<ILogger<CatalogueToolDescriptionLimitChatClient>>());

            return new TelemetryChatClient(
                modelFacingClient,
                configuration[AgentFoundryRegistration.ModelKey]?.Trim() ?? "(unconfigured)",
                WorkspaceSuggestionAgentRoster.PromptVersions,
                provider.GetRequiredService<ILogger<TelemetryChatClient>>());
        });

        // Scoped like the run, so the tools a call listed stay callable for as long as its token lives.
        services.AddScoped<IMcpAuthorizationConnection, McpAuthorizationConnection>();

        // The run's ending, stated once: the catalogue session a run opened is closed while the token it was
        // opened with is still held, and only then is the token given up. The transport's own session-termination
        // request is authenticated, so releasing first is what made shutdown fail.
        services.AddScoped<IRunEnding, RunEnding>();

        // One attempt's tool answers, recorded as they return and cleared when the next retrieval begins. Scoped
        // like the run, and read by the stage that ranks the products those answers carry.
        services.AddScoped<McpToolAnswerLedger>();

        // The guardrail policies. Registered by interface, so the pipeline below never names a concrete guard and
        // the order they run in is the order they are added.
        services.AddScoped<IToolAllowList, CatalogueToolAllowList>();
        services.AddScoped<IToolArgumentPolicy, CatalogueSearchArgumentPolicy>();
        services.AddSingleton(new ToolArgumentLimits(
            MaximumArguments: 10,
            MaximumStringLength: workflowOptions.MaximumSearchTermCharacterCount,
            MaximumCollectionItems: workflowOptions.MaximumSearchTermCountPerComponent));

        // The order below IS the order they run: allow-list, then authorization, then the size ceiling, then the
        // catalogue's own vocabulary. A denied guard stops the call before the tool executes.
        services.AddScoped<IToolGuard, ToolAllowListGuard>();
        services.AddScoped<IToolGuard, ToolAuthorizationGuard>();
        services.AddScoped<IToolGuard, ArgumentCeilingGuard>();
        services.AddScoped<IToolGuard, ToolArgumentGuard>();

        // The result side, in order: size, then the tool's own contract, then untrusted content. Recording runs
        // last, so the ledger holds the sanitized answer and every later stage reads what the model read.
        services.AddScoped<IToolResultValidator, CatalogueResultValidator>();
        services.AddScoped<IToolResultGuard>(_ => new ToolResultSizeGuard(MaximumToolResultCharacters));
        services.AddScoped<IToolResultGuard, ToolResultSchemaGuard>();
        services.AddScoped<IToolResultGuard, UntrustedToolDataGuard>();
        services.AddScoped<IToolResultGuard, ToolResultRecordingGuard>();

        // The guardrail pipeline, assembled from whatever guards were registered - by interface, so the order
        // they run in is the order above and no guard is named by its implementation here. Scoped like the run,
        // because the authorization guard reads its caller's token.
        services.AddScoped<IToolGuardPipeline>(provider => new ToolGuardPipeline(
            [.. provider.GetServices<IToolGuard>()],
            [.. provider.GetServices<IToolResultGuard>()]));

        // The stage agents - the one place the decorators' order is stated - and the executors built from them.
        // These are stateless factories: they are shared, but the agent they produce is not. Each resolution
        // builds a new workflow, which is what gives a concurrent request its own workflow owner.
        services.AddSingleton<WorkspaceSuggestionAgentBuilder>();
        services.AddSingleton<WorkspaceSuggestionExecutorBuilder>();
        services.AddSingleton<IWorkspaceSuggestionWorkflow, WorkspaceSuggestionWorkflow>();

        // The served agent is the workflow agent itself and is never wrapped: hosting can redirect a hosted
        // workflow's checkpoints only when it can copy that agent. It is transient because a workflow has one
        // owner; the host resolves a new agent for each request, and each resolution builds a new workflow. The
        // run's own state is still reached through the run scope, never captured by the shared factories.
        services.AddKeyedTransient<AIAgent>(workspaceSuggestionAgentIdentity.AgentName, (provider, _) =>
            provider.GetRequiredService<IWorkspaceSuggestionWorkflow>().AsAIAgent());

        return workspaceSuggestionAgentIdentity.AgentName;
    }
}
