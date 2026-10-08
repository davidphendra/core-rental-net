using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using CoreRentalNet.Agents.Features.EchoReverse;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The echo agent's name and whether this deployable serves it.
/// </summary>
public sealed class EchoAgentIdentityTests
{
    [Fact]
    public void The_echo_agent_is_named_and_enabled_by_default()
    {
        var identity = EchoAgentIdentity.FromConfiguration(Configuration());

        identity.AgentName.Should().Be(EchoAgentIdentity.DefaultAgentName, "azd addresses the agent by this name");
        identity.IsEnabled.Should().BeTrue("it costs nothing and echoes only the text its caller sent");
    }

    [Fact]
    public void A_configured_name_wins_over_the_default()
    {
        var identity = EchoAgentIdentity.FromConfiguration(
            Configuration((EchoAgentIdentity.AgentNameKey, "echo-under-test")));

        identity.AgentName.Should().Be("echo-under-test");
    }

    [Fact]
    public void A_deployment_can_turn_the_echo_agent_off()
    {
        var identity = EchoAgentIdentity.FromConfiguration(
            Configuration((EchoAgentIdentity.IsEnabledKey, "false")));

        identity.IsEnabled.Should().BeFalse();
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
