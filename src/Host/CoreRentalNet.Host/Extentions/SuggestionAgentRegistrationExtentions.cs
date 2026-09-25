using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Wires the AI workspace builder: the agent it asks, or the reason there is none.
/// </summary>
/// <remarks>
/// <para>
/// The choice between a real agent and a refusal is made once, here, rather than checked at every call site:
/// with the feature off or the endpoint missing, the registered implementation answers <b>unavailable</b>,
/// and the AI section is hidden. A missing setting therefore hides the feature instead of opening it.
/// </para>
/// <para>
/// <b>There is no catalogue registration here.</b> The agent reaches the catalogue through the MCP tools the
/// Host publishes; the application no longer embeds a query, holds a shortlist or reads a vector file for the
/// builder. The similarity search keeps its own registration, which the catalogue's endpoint and the MCP tool
/// both use.
/// </para>
/// </remarks>
internal static class SuggestionAgentRegistrationExtentions
{
    public static WebApplicationBuilder AddSuggestionAgent(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = AgentFoundrySettings.From(builder.Configuration);

        builder.Services.AddSingleton(settings);

        // Availability, which nothing hides now that the application keeps no index of its own. It stays
        // registered because the section still reads it, and a deployment that later wants a gate has one place
        // to write it.
        builder.Services.AddSingleton<SuggestionAvailability>();

        builder.Services.AddSingleton<ISuggestionAgent>(_ =>
            settings.IsConfigured
                ? new FoundrySuggestionAgent(settings)
                : new NoAgentConfigured());

        return builder;
    }
}
