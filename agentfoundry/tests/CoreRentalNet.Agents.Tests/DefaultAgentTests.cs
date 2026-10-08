using AwesomeAssertions;
using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Hosting;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The agent a request that carries no name is answered by. Hosting falls back to the non-keyed
/// <see cref="AIAgent"/>, so the registration has to point at a keyed agent that exists.
/// </summary>
public sealed class DefaultAgentTests
{
    [Fact]
    public void The_default_agent_is_the_keyed_registration_for_its_name()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<AIAgent>(EchoAgentIdentity.DefaultAgentName, EchoAgent());

        services.AddDefaultAgent(EchoAgentIdentity.DefaultAgentName);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<AIAgent>().Name.Should().Be(EchoAgentIdentity.DefaultAgentName);
    }

    [Fact]
    public void A_default_name_that_is_served_by_nobody_fails_at_resolution()
    {
        // A name with no keyed registration is exactly the silent misroute this exists to surface.
        var services = new ServiceCollection();
        services.AddDefaultAgent("nobody-serves-this");

        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<AIAgent>();

        act.Should().Throw<InvalidOperationException>();
    }

    private static AIAgent EchoAgent()
        => new ChatClientAgent(
            new FixedChatClient("{}"),
            new ChatClientAgentOptions { Name = EchoAgentIdentity.DefaultAgentName });
}
