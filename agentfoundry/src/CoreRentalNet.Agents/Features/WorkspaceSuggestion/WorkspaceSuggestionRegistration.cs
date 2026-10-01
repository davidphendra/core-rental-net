using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

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

    /// <summary>Registers the feature, and answers the agent name it serves.</summary>
    public static string AddWorkspaceSuggestionFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        IChatClient modelClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(modelClient);

        var workspaceSuggestionAgentIdentity = WorkspaceSuggestionAgentIdentity.FromConfiguration(configuration);

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

        // The run-scoped chain: the token-leak guardrail, then the model client. One instance records every
        // stage's call, so the totals are the run's, and it names the stage from the options its agent set.
        services.AddScoped<IModelCallTelemetryChatClient>(provider => new ModelCallTelemetryChatClient(
            new ModelOutputGuardrailChatClient(
                modelClient,
                provider.GetRequiredService<IMcpAccessTokenService>()),
            configuration[AgentFoundryRegistration.ModelKey]?.Trim() ?? "(unconfigured)",
            WorkspaceSuggestionAgentRoster.PromptVersions,
            provider.GetRequiredService<ILogger<ModelCallTelemetryChatClient>>()));

        // Scoped like the run, so the tools a call listed stay callable for as long as its token lives.
        services.AddScoped<IMcpAuthorizationConnection, McpAuthorizationConnection>();

        // One attempt's tool answers, recorded as they return and cleared when the next retrieval begins. Scoped
        // like the run, and read by the stage that ranks the products those answers carry.
        services.AddScoped<McpToolAnswerLedger>();

        // The stage agents - the one place the decorators' order is stated - and the executors built from them.
        // Both are scoped like the run, because the agents hold its token service and its model-call telemetry
        // and the executors hold its ledger.
        services.AddScoped<WorkspaceSuggestionAgentBuilder>();
        services.AddScoped<WorkspaceSuggestionExecutorBuilder>();
        services.AddScoped<IWorkspaceSuggestionWorkflow, WorkspaceSuggestionWorkflow>();

        // The served agent is the workflow agent itself, resolved per request and never wrapped: hosting can
        // redirect a hosted workflow's checkpoints only when it can copy that agent.
        services.AddKeyedScoped<AIAgent>(workspaceSuggestionAgentIdentity.AgentName, (provider, _) =>
            provider.GetRequiredService<IWorkspaceSuggestionWorkflow>().AsAIAgent());

        return workspaceSuggestionAgentIdentity.AgentName;
    }
}
