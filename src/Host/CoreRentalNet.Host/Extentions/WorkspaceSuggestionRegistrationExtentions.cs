using CoreRentalNet.Host.Components.Shared.Suggestion;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

namespace CoreRentalNet.Host.Extentions;

/// <summary>Wires the workspace suggestion run: the agent it asks, the pipeline it drives and the record it writes.</summary>
/// <remarks>
/// <para>
/// The choice between a real agent and an unavailable one is made once, here, rather than checked at every call
/// site: with the feature off or the endpoint missing, the registered implementation answers <b>unavailable</b>,
/// and the suggestion panel is hidden. A missing setting therefore hides the feature instead of opening it.
/// </para>
/// <para>
/// <b>The connection settings are bound here, in the composition root</b>, so the keys stay where the
/// configuration guard can see them and the module names no configuration key of its own.
/// </para>
/// <para>
/// The handlers are stateless, so they are singletons; the run is created per request because an HTTP request has
/// its own scope and nothing is shared between two runs. <b>The progress reader and the adapter are the
/// exception:</b> the reader keeps a per-run cursor, so both are scoped and one reader exists per run.
/// </para>
/// </remarks>
internal static class WorkspaceSuggestionRegistrationExtentions
{
    public static void AddWorkspaceSuggestions(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionSettings = new AgentFoundryConnectionSetting(
            Enabled: builder.Configuration.GetValue("AgentFoundry:Enabled", false),
            ProjectEndpoint: builder.Configuration["AgentFoundry:ProjectEndpoint"]?.Trim() ?? string.Empty,
            AgentName: builder.Configuration["AgentFoundry:AgentName"]?.Trim() ?? "core-rental-workspace-suggestion-agent");

        builder.Services.AddSingleton(connectionSettings);
        builder.Services.AddSingleton<WorkspaceSuggestionAvailability>();

        builder.Services.AddScoped<IWorkspaceSuggestionProgressReader, WorkspaceSuggestionProgressReader>();
        builder.Services.AddScoped<IWorkspaceSuggestionAgentAdapter>(serviceProvider =>
            connectionSettings.IsConfigured
                ? new AgentFoundryWorkspaceSuggestionAdapter(
                    connectionSettings,
                    serviceProvider.GetRequiredService<IWorkspaceSuggestionProgressReader>())
                : new NotConfiguredWorkspaceSuggestionAgentAdapter());

        builder.Services.AddSingleton<IWorkspaceSuggestionRunRecordWriter, LoggerWorkspaceSuggestionRunRecordWriter>();
        AddStreamHandlers(builder.Services);
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamProcessor, WorkspaceSuggestionStreamProcessor>();

        builder.Services.AddScoped<IWorkspaceSuggestionRequestFactory, WorkspaceSuggestionRequestPayloadFactory>();
        builder.Services.AddScoped<IWorkspaceSuggestionRunService, WorkspaceSuggestionRunService>();

        // The panel's own composition: the registry a rendered notice is drawn through. It is registered here
        // rather than with the run because it is the panel's, and a component that injects a service the
        // container does not hold throws the moment the panel renders.
        builder.Services.AddSingleton<ISuggestionNoticeComponentResolver, SuggestionNoticeComponentResolver>();
    }

    /// <summary>One handler per agent event, in the order the chain is asked.</summary>
    private static void AddStreamHandlers(IServiceCollection services)
    {
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionStageStartedStreamEventHandler>();
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionStageCompletedStreamEventHandler>();
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionRetryStreamEventHandler>();
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionCandidateApprovedStreamEventHandler>();
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionRawOutputStreamEventHandler>();
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionResultReadyStreamEventHandler>();
        services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionUnavailableStreamEventHandler>();
    }
}
