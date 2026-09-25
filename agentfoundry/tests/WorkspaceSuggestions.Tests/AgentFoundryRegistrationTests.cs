using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Composition;
using WorkspaceSuggestions.Prompts;
using WorkspaceSuggestions.Workflows;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// The composition this host is built from. It was the least-covered file in the agent project, and the
/// parts that were uncovered are exactly the parts a deployment depends on.
/// </summary>
public sealed class AgentRegistrationTests
{
    [Fact]
    public void A_missing_project_endpoint_is_refused_by_name()
    {
        var act = () => AgentRegistration.BuildChatClient(Configuration(
            ("AZURE_AI_MODEL_DEPLOYMENT_NAME", "gpt-4.1-mini")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*FOUNDRY_PROJECT_ENDPOINT*");
    }

    [Fact]
    public void A_missing_model_deployment_is_refused_by_name()
    {
        var act = () => AgentRegistration.BuildChatClient(Configuration(
            ("FOUNDRY_PROJECT_ENDPOINT", "https://example.invalid/api/projects/p")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*AZURE_AI_MODEL_DEPLOYMENT_NAME*");
    }

    [Fact]
    public void An_empty_setting_counts_as_missing_rather_than_as_a_value()
    {
        // An environment variable set to nothing is the shape a half-finished deployment actually has.
        var act = () => AgentRegistration.BuildChatClient(Configuration(
            ("FOUNDRY_PROJECT_ENDPOINT", "  "),
            ("AZURE_AI_MODEL_DEPLOYMENT_NAME", "gpt-4.1-mini")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*FOUNDRY_PROJECT_ENDPOINT*");
    }

    [Fact]
    public void Every_agent_on_the_roster_is_registered_under_its_own_name()
    {
        var services = new ServiceCollection();

        services.AddRosterAgents(new ScriptedChatClient("{}"), []);

        using var provider = services.BuildServiceProvider();

        foreach (var profile in AgentRoster.All)
        {
            provider.GetRequiredKeyedService<AIAgent>(profile.Name).Name.Should().Be(profile.Name);
        }

        AgentRoster.All.Select(profile => profile.Name).Should().Equal("rephraser", "suggestor");
    }

    [Fact]
    public void The_workflow_is_published_as_the_single_named_agent_foundry_resolves()
    {
        var agent = AgentRegistration.BuildWorkflowAgent(new ScriptedChatClient("{}"), "gpt-4.1-mini", []);

        agent.Name.Should().Be(WorkspaceSuggestionWorkflow.AgentName);
        agent.Name.Should().Be("core-rental-workspace-suggestion-agent");
    }

    [Fact]
    public void The_reported_prompt_version_names_every_prompt_the_run_used()
    {
        // One string for a run that is two agents: reporting only the suggestor would understate what ran.
        AgentRegistration.PromptVersions().Should().Be("rephraser.v1+suggestor.v3");
    }

    [Theory]
    [InlineData("suggestor.v1.md", "suggestor.v1")]
    [InlineData("rephraser.v10.md", "rephraser.v10")]
    [InlineData("already-named.v1", "already-named.v1")]
    public void A_prompt_version_comes_from_the_file_name(string fileName, string expected)
    {
        PromptVersion.From(fileName).Should().Be(expected);
    }

    [Fact]
    public void A_prompt_with_no_file_name_has_no_version_rather_than_a_blank_one()
    {
        var act = () => PromptVersion.From("  ");

        act.Should().Throw<ArgumentException>();
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
