using Microsoft.Extensions.Configuration;
using CoreRentalNet.Agents.Shared.Configuration;

namespace CoreRentalNet.Agents.Features.EchoReply;

/// <summary>The name a console addresses the echo agent by, and whether this deployable serves it at all.</summary>
/// <remarks>
/// Its own configuration key rather than a reserved <c>FOUNDRY_</c> one, because nothing injects it: the platform
/// names the deployed agent, and this test aid names itself. Enabled by default, because it spends nothing and
/// echoes only the text its caller sent; a deployment turns it off with <c>ECHOAGENT__ISENABLED=false</c>.
/// </remarks>
public sealed record EchoAgentIdentity
{
    public const string AgentNameKey = "EchoAgent:AgentName";
    public const string IsEnabledKey = "EchoAgent:IsEnabled";
    public const string DefaultAgentName = "echo-agent";

    public required string AgentName { get; init; }

    public bool IsEnabled { get; init; } = true;

    public static EchoAgentIdentity FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new EchoAgentIdentity
        {
            AgentName = HostedAgentName.Read(configuration, AgentNameKey, DefaultAgentName),
            IsEnabled = !bool.TryParse(configuration[IsEnabledKey], out var isEnabled) || isEnabled,
        };
    }
}
