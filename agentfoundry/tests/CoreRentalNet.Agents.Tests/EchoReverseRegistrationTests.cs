using AwesomeAssertions;
using CoreRentalNet.Agents.Features.EchoReverse;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The echo workflow has the same single-owner rule as every other workflow.</summary>
public sealed class EchoReverseRegistrationTests
{
    [Fact]
    public void The_echo_agent_is_resolved_fresh_for_each_request()
    {
        var identity = new EchoAgentIdentity
        {
            AgentName = EchoAgentIdentity.DefaultAgentName,
            IsEnabled = true,
        };

        var services = new ServiceCollection();
        services.AddEchoReverseFeature(identity);

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredKeyedService<AIAgent>(identity.AgentName);
        var second = provider.GetRequiredKeyedService<AIAgent>(identity.AgentName);

        first.Should().NotBeSameAs(second);
    }
}
