using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Wires the AI workspace builder: the agent it asks, or the reason there is none, and the guard that admits
/// one run per customer.
/// </summary>
/// <remarks>
/// The choice between a real agent and a refusal is made once, here, rather than checked at every call site:
/// with the feature off or the endpoint missing, the registered implementation answers <b>unavailable</b>,
/// and the AI section is hidden. A missing setting therefore hides the feature instead of opening it.
///
/// The guard is a singleton because its whole purpose is to know what is in flight across requests; a
/// per-request instance would guard nothing.
/// </remarks>
internal static class SuggestionAgentRegistration
{
    public static WebApplicationBuilder AddSuggestionAgent(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = SuggestionAgentSettings.From(builder.Configuration);

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<ISuggestionAgent>(
            settings.IsConfigured ? new FoundrySuggestionAgent(settings) : new NoAgentConfigured());
        builder.Services.AddSingleton<RunGuard>();

        // The run's input: the catalogue this application already loaded, and the slot capacities it
        // already read. A singleton because both of its dependencies are, and because it holds nothing.
        builder.Services.AddSingleton<SuggestionRequestBuilder>();

        return builder;
    }
}
