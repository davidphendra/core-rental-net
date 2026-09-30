using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

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
        services.AddSingleton(CallerAuthorisedMcpSettings.FromConfiguration(configuration, CatalogueEndpointKey));
        var workflowOptions = configuration.GetSection("WorkspaceSuggestionWorkflow")
            .Get<WorkspaceSuggestionWorkflowOptions>() ?? new WorkspaceSuggestionWorkflowOptions();

        services.AddSingleton(workflowOptions);

        // The bound is configuration rather than a constant so a deployment can move it, and it is registered as
        // a type rather than passed around as two numbers so the stage that applies it cannot be given one of
        // them and not the other.
        // Which tools are the catalogue's is declared where the feature declares everything else about itself, so
        // a deployment that publishes a third catalogue tool adds it here rather than editing the pool builder.
        services.AddSingleton(new WorkspaceComponentProductPoolBuilder(CatalogueSearchToolNames.All));
        services.AddSingleton(new WorkspaceComponentProductPoolPolicy(
            workflowOptions.MaximumRetrievedProductsPerComponentForReranking));
        services.AddSingleton(new WorkspaceComponentProductSelectionPolicy(
            workflowOptions.MaximumSelectedProductsPerComponent));
        services.AddSingleton(new WorkspaceComponentSearchVocabularyLimitPolicy(
            workflowOptions.MaximumSearchTermCountPerComponent,
            workflowOptions.MaximumSearchTermCharacterCount));

        // Where a run's catalogue token is read from: the invocation's own headers, which the hosting layer
        // forwards for names carrying its client prefix. Nothing carries one in a message any more, so there is
        // no splitter to register and no member on the request for a splitter to recognise.
        services.AddScoped<CallerAccessTokenHeaderReader>();

        // One run's cost, counted across its stages, and the model and instructions that produced it.
        services.AddScoped(_ => new AgentRunUsageAccumulator(
            configuration[AgentFoundryRegistration.ModelKey]?.Trim() ?? "(unconfigured)",
            WorkspaceSuggestionAgentRoster.PromptVersions));

        // Scoped like the run, so the tools a call listed stay callable for as long as its token lives.
        services.AddScoped<CallerAuthorisedMcpConnection>();

        // One attempt's tool answers, recorded as they return and cleared when the next retrieval begins. Scoped
        // like the run, and read by the stage that ranks the products those answers carry.
        services.AddScoped<McpToolAnswerLedger>();
        services.AddScoped<WorkspaceSuggestionWorkflowFactory>();
        services.AddScoped<IWorkspaceSuggestionWorkflow, WorkspaceSuggestionWorkflow>();

        // The served agent is the workflow agent itself, resolved per request and never wrapped: hosting can
        // redirect a hosted workflow's checkpoints only when it can copy that agent.
        services.AddKeyedScoped<AIAgent>(workspaceSuggestionAgentIdentity.AgentName, (provider, _) =>
            provider.GetRequiredService<IWorkspaceSuggestionWorkflow>().AsAIAgent());

        return workspaceSuggestionAgentIdentity.AgentName;
    }
}
