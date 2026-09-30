using Microsoft.Extensions.Configuration;
using CoreRentalNet.Agents.Shared.Configuration;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>The name Foundry resolves, read from configuration so the deployment names it once.</summary>
/// <remarks>
/// <para>
/// Hosting resolves an incoming request by the agent name the request carries, and looks both the agent and its
/// session store up in keyed services under that name. A registration name that disagrees with the deployed name
/// is therefore not acknowledged: the request warns, falls back to default resolution, and the keyed session
/// store is skipped. The name comes from configuration - the platform's environment variable when it set one, the
/// application's own value otherwise - rather than from a constant that cannot agree with the deployment.
/// </para>
/// <para>
/// The standard configuration order does the work: an environment variable overrides the value in
/// <c>appsettings.json</c>, and <c>null</c> or whitespace counts as absent, so a setting that was injected empty
/// is refused by name rather than accepted as a name nobody meant.
/// </para>
/// </remarks>
internal sealed record WorkspaceSuggestionAgentIdentity
{
    /// <summary>The configuration key, and the environment variable the platform injects it through.</summary>
    public const string ConfigurationKey = "FOUNDRY_AGENT_NAME";

    /// <summary>The name hosting resolves from a request, and the identity a run record names.</summary>
    public required string AgentName { get; init; }

    /// <summary>The configured identity, or a failure that names the setting nobody set.</summary>
    public static WorkspaceSuggestionAgentIdentity FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new WorkspaceSuggestionAgentIdentity
        {
            AgentName = HostedAgentName.Read(configuration, ConfigurationKey),
        };
    }
}
