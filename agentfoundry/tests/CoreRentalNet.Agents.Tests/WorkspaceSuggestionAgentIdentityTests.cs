using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The name hosting resolves a request by. It is configuration rather than a constant, so a deployment that
/// renames the agent is still acknowledged: hosting looks the agent and its session store up in keyed services
/// under exactly this name, and a disagreement falls back to default resolution.
/// </summary>
public sealed class WorkspaceSuggestionAgentIdentityTests
{
    private const string ConfigurationKey = "FOUNDRY_AGENT_NAME";

    [Fact]
    public void The_agent_name_is_read_from_configuration()
    {
        var identity = WorkspaceSuggestionAgentIdentity.FromConfiguration(
            Configuration((ConfigurationKey, "my-workspace-agent")));

        identity.AgentName.Should().Be("my-workspace-agent");
    }

    [Fact]
    public void A_missing_agent_name_is_refused_by_name()
    {
        var act = () => WorkspaceSuggestionAgentIdentity.FromConfiguration(Configuration());

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{ConfigurationKey}*");
    }

    [Fact]
    public void An_empty_agent_name_counts_as_missing_rather_than_as_a_name()
    {
        // An environment variable set to nothing is the shape a half-finished deployment actually has.
        var act = () => WorkspaceSuggestionAgentIdentity.FromConfiguration(
            Configuration((ConfigurationKey, "   ")));

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{ConfigurationKey}*");
    }

    [Fact]
    public void A_configured_agent_name_is_trimmed()
    {
        var identity = WorkspaceSuggestionAgentIdentity.FromConfiguration(
            Configuration((ConfigurationKey, "  my-workspace-agent  ")));

        identity.AgentName.Should().Be("my-workspace-agent");
    }

    [Fact]
    public void The_environment_overrides_the_application_default()
    {
        var previous = Environment.GetEnvironmentVariable(ConfigurationKey);
        Environment.SetEnvironmentVariable(ConfigurationKey, "from-the-environment");

        try
        {
            var identity = WorkspaceSuggestionAgentIdentity.FromConfiguration(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationKey] = "from-appsettings" })
                    .AddEnvironmentVariables()
                    .Build());

            identity.AgentName.Should().Be("from-the-environment");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationKey, previous);
        }
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
