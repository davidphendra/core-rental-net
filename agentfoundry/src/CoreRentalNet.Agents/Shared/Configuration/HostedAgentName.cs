using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Agents.Shared.Configuration;

/// <summary>How this deployable reads the name hosting resolves a request by.</summary>
/// <remarks>
/// <para>
/// Hosting resolves an incoming request by the agent name the request carries, and looks both the agent and its
/// session store up in keyed services under that name. A registration name that disagrees with the name in the
/// request is therefore not acknowledged: the request warns, falls back to default resolution, and the keyed
/// session store is skipped.
/// </para>
/// <para>
/// The rule is shared; the type and the key are not, because two features serve two names and each has to be
/// identified separately in the container. <c>null</c> and whitespace count as absent, so a setting that was
/// injected empty is refused by name rather than accepted as a name nobody meant.
/// </para>
/// </remarks>
public static class HostedAgentName
{
    /// <summary>The name at the given key, the default when it is absent, or a failure naming the setting.</summary>
    public static string Read(
        IConfiguration configuration,
        string configurationKey,
        string? defaultAgentName = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredName = configuration[configurationKey]?.Trim();

        var agentName = configuredName is { Length: > 0 } ? configuredName : defaultAgentName;

        return agentName is { Length: > 0 }
            ? agentName
            : throw new InvalidOperationException($"{configurationKey} is not configured.");
    }
}
