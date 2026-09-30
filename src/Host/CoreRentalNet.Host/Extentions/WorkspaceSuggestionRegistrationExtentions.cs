using CoreRentalNet.Host.Components.Shared.Suggestion;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
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
/// The handlers are stateless, so they are singletons; the run is created per request because an HTTP request
/// has its own scope and nothing is shared between two runs.
/// </para>
/// </remarks>
internal static class WorkspaceSuggestionRegistrationExtentions
{
    public static void AddWorkspaceSuggestions(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionSettings = new MicrosoftFoundryAgentConnectionSettings(
            Enabled: builder.Configuration.GetValue("AgentFoundry:Enabled", false),
            ProjectEndpoint: builder.Configuration["AgentFoundry:ProjectEndpoint"]?.Trim() ?? string.Empty,
            AgentName: builder.Configuration["AgentFoundry:AgentName"]?.Trim() ?? "core-rental-workspace-suggestion-agent");

        builder.Services.AddSingleton(connectionSettings);
        builder.Services.AddSingleton<WorkspaceSuggestionAvailability>();

        builder.Services.AddSingleton<IWorkspaceSuggestionAgentAdapter>(_ =>
            connectionSettings.IsConfigured
                ? new MicrosoftFoundryWorkspaceSuggestionAgentAdapter(connectionSettings)
                : new UnconfiguredWorkspaceSuggestionAgentAdapter());

        builder.Services.AddSingleton<IWorkspaceSuggestionRunRecordWriter, LoggerWorkspaceSuggestionRunRecordWriter>();

        builder.Services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionStageChangedStreamEventHandler>();
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionRetryStreamEventHandler>();
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionCandidateApprovedStreamEventHandler>();
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionNarrativeDeltaStreamEventHandler>();
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionResultReadyStreamEventHandler>();
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamEventHandler, WorkspaceSuggestionUnavailableStreamEventHandler>();
        builder.Services.AddSingleton<IWorkspaceSuggestionStreamProcessor, WorkspaceSuggestionStreamProcessor>();

        builder.Services.AddScoped<IWorkspaceSuggestionRequestFactory, WorkspaceSuggestionRequestFactory>();
        builder.Services.AddScoped<IWorkspaceSuggestionRunService, WorkspaceSuggestionRunService>();

        // The panel's own composition: the registry a rendered notice is drawn through. It is registered here
        // rather than with the run because it is the panel's, and a component that injects a service the
        // container does not hold throws the moment the panel renders - which is the failure this line exists
        // to prevent.
        builder.Services.AddSingleton<ISuggestionNoticeComponentResolver, SuggestionNoticeComponentResolver>();
    }
}
