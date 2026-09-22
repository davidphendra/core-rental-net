using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Composition;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// The roster is registered under the names the workflow resolves its executors by, which is what makes
/// adding an agent a roster entry rather than a change to the composition.
/// </summary>
public sealed class RosterRegistrationTests
{
    [Fact]
    public void Every_agent_on_the_roster_is_registered_under_its_own_name()
    {
        var services = new ServiceCollection();

        services.AddRosterAgents(new FixedChatClient("{}"), []);

        using var provider = services.BuildServiceProvider();

        foreach (var profile in AgentRoster.All)
        {
            provider.GetRequiredKeyedService<AIAgent>(profile.Name)
                .Name.Should().Be(profile.Name, $"{profile.Name} is the name the workflow resolves");
        }
    }

    [Fact]
    public void A_missing_project_endpoint_is_refused_by_name()
    {
        var act = () => AgentRegistration.BuildChatClient(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AZURE_AI_MODEL_DEPLOYMENT_NAME"] = "gpt-4.1-mini",
            }).Build());

        act.Should().Throw<InvalidOperationException>().WithMessage("*FOUNDRY_PROJECT_ENDPOINT*");
    }

    [Fact]
    public void A_missing_model_deployment_is_refused_by_name()
    {
        var act = () => AgentRegistration.BuildChatClient(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FOUNDRY_PROJECT_ENDPOINT"] = "https://example.invalid/api/projects/probe",
            }).Build());

        act.Should().Throw<InvalidOperationException>().WithMessage("*AZURE_AI_MODEL_DEPLOYMENT_NAME*");
    }
}
