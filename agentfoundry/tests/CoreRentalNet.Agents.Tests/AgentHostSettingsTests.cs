using AwesomeAssertions;
using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The host's configuration, resolved once. Every reader downstream — the registrations and the startup
/// report — takes this value rather than a key, so two readers cannot disagree about what a setting said.
/// </summary>
public sealed class AgentHostSettingsTests
{
    private const string WorkspaceAgentName = "core-rental-workspace-suggestion-agent";

    [Fact]
    public void A_request_that_names_no_agent_falls_back_to_the_workspace_agent()
    {
        var settings = Settings(Workspace(WorkspaceAgentName));

        settings.DefaultAgentName.Should().Be(WorkspaceAgentName);
    }

    [Fact]
    public void A_configured_default_agent_name_wins()
    {
        var settings = Settings(
            Workspace(WorkspaceAgentName),
            (AgentHostSettings.DefaultAgentNameKey, EchoAgentIdentity.DefaultAgentName));

        settings.DefaultAgentName.Should().Be(EchoAgentIdentity.DefaultAgentName);
    }

    [Fact]
    public void The_workspace_agent_name_is_resolved_by_its_own_rules()
    {
        // The old composition root read this key raw. The identity trims it; the host must carry the trimmed value.
        var settings = Settings(Workspace($"  {WorkspaceAgentName}  "));

        settings.WorkspaceSuggestion.AgentName.Should().Be(WorkspaceAgentName);
    }

    [Fact]
    public void The_echo_agent_is_carried_with_its_enabled_state()
    {
        var settings = Settings(Workspace(WorkspaceAgentName), (EchoAgentIdentity.IsEnabledKey, "false"));

        settings.Echo.AgentName.Should().Be(EchoAgentIdentity.DefaultAgentName);
        settings.Echo.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void A_default_that_names_a_disabled_echo_agent_is_refused_by_name()
    {
        var act = () => Settings(
            Workspace(WorkspaceAgentName),
            (AgentHostSettings.DefaultAgentNameKey, EchoAgentIdentity.DefaultAgentName),
            (EchoAgentIdentity.IsEnabledKey, "false"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{AgentHostSettings.DefaultAgentNameKey}*{EchoAgentIdentity.IsEnabledKey}*");
    }

    [Fact]
    public void The_model_transport_budget_is_bound_rather_than_left_to_its_default()
    {
        var settings = Settings(
            Workspace(WorkspaceAgentName),
            ("ModelTransport:NetworkTimeout", "00:05:00"),
            ("ModelTransport:MaximumRetryAttempts", "4"));

        settings.ModelTransport.NetworkTimeout.Should().Be(TimeSpan.FromMinutes(5));
        settings.ModelTransport.MaximumRetryAttempts.Should().Be(4);
    }

    [Fact]
    public void An_absent_project_endpoint_and_model_are_carried_as_absent()
    {
        var settings = Settings(Workspace(WorkspaceAgentName));

        settings.ProjectEndpoint.Should().BeNull();
        settings.ModelDeployment.Should().BeNull();
    }

    private static (string Key, string Value) Workspace(string name)
        => (WorkspaceSuggestionAgentIdentity.ConfigurationKey, name);

    private static AgentHostSettings Settings(params (string Key, string Value)[] values)
        => AgentHostSettings.FromConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.ToDictionary(
                    pair => pair.Key,
                    pair => (string?)pair.Value))
                .Build());
}
