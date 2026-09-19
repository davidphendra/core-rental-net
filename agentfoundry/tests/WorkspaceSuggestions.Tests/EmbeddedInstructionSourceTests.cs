using AwesomeAssertions;
using Microsoft.Agents.AI;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Prompts;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// A prompt has to travel with the binary, a run has to be able to name which prompt produced it, and the
/// roster has to be what decides which prompt an agent gets — one file per agent, named not hard-coded.
/// </summary>
public sealed class EmbeddedInstructionSourceTests
{
    [Fact]
    public void The_rephraser_prompt_is_embedded_and_readable()
    {
        var source = new EmbeddedInstructionSource("rephraser.v1.md");

        source.Text.Should().NotBeNullOrWhiteSpace();
        source.Text.Should().Contain("notWorkspace", "the off-topic verdict is part of the prompt");
        source.Version.Should().Be("rephraser.v1");
    }

    [Fact]
    public void The_suggestor_prompt_is_embedded_and_readable()
    {
        var source = new EmbeddedInstructionSource("suggestor.v1.md");

        source.Text.Should().NotBeNullOrWhiteSpace();
        source.Text.Should().Contain("catalogue", "the catalogue is the only thing it may choose from");
        source.Version.Should().Be("suggestor.v1");
    }

    [Fact]
    public void The_version_comes_from_the_file_name_rather_than_from_a_constant()
    {
        new EmbeddedInstructionSource("rephraser.v1.md").Version.Should().Be("rephraser.v1");
        new EmbeddedInstructionSource("suggestor.v1.md").Version.Should().Be("suggestor.v1");
    }

    [Fact]
    public void A_prompt_that_is_not_embedded_is_refused_by_name()
    {
        var act = () => new EmbeddedInstructionSource("no-such-prompt.v9.md");

        act.Should().Throw<InvalidOperationException>().WithMessage("*no-such-prompt.v9.md*");
    }

    [Fact]
    public void Every_agent_on_the_roster_has_its_prompt_embedded()
    {
        foreach (var profile in AgentRoster.All)
        {
            var source = new EmbeddedInstructionSource(profile.PromptFileName);

            source.Text.Should().NotBeNullOrWhiteSpace($"{profile.Name} must be able to find its instructions");
        }
    }

    [Fact]
    public void The_roster_builds_an_agent_against_a_fake_client()
    {
        // The factory is what the tests and the host share, so this proves the roster reaches a real AIAgent
        // with no network and no credential.
        foreach (var profile in AgentRoster.All)
        {
            AIAgent agent = AgentFactory.Build(profile, new FixedChatClient("{}"));

            agent.Name.Should().Be(profile.Name);
        }
    }
}
