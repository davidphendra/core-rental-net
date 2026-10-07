using AwesomeAssertions;
using Microsoft.Agents.AI;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// A prompt has to travel with the binary, a run has to be able to name which prompt produced it, and the roster
/// has to be what decides which prompt an agent gets — one file per stage, named not hard-coded.
/// </summary>
public sealed class EmbeddedInstructionSourceTests
{
    [Fact]
    public void The_verification_prompt_is_embedded_and_readable()
    {
        var source = new EmbeddedInstructionSource("workspace-request-verifier.v2.md");

        source.Text.Should().NotBeNullOrWhiteSpace();
        source.Text.Should().Contain("isWorkspaceRequest", "the output contract is part of the prompt");
        source.Version.Should().Be("workspace-request-verifier.v2");
    }

    [Fact]
    public void The_rephrasing_prompt_is_embedded_and_readable()
    {
        var source = new EmbeddedInstructionSource("workspace-requirement-rephraser.v2.md");

        source.Text.Should().NotBeNullOrWhiteSpace();
        source.Text.Should().Contain("search_terms", "the output contract is part of the prompt");
        source.Text.Should().Contain("total_budget", "the budget rules are part of the prompt");
        source.Version.Should().Be("workspace-requirement-rephraser.v2");
    }

    [Fact]
    public void The_retrieval_prompt_is_embedded_and_names_no_product_of_its_own()
    {
        var source = new EmbeddedInstructionSource("catalogue-product-retriever.v2.md");

        source.Text.Should().NotBeNullOrWhiteSpace();
        source.Text.Should().Contain("searches", "what each search came back with is what it reports");
        source.Text.Should().Contain(
            "do not list the products",
            "the products come from the recorded tool answers, so it is told not to restate them");
        source.Version.Should().Be("catalogue-product-retriever.v2");
    }

    [Fact]
    public void The_version_comes_from_the_file_name_rather_than_from_a_constant()
    {
        new EmbeddedInstructionSource("workspace-request-verifier.v2.md").Version
            .Should().Be("workspace-request-verifier.v2");
        new EmbeddedInstructionSource("workspace-setup-composer.v2.md").Version
            .Should().Be("workspace-setup-composer.v2");
    }

    [Fact] // a prompt file is named after the agent it instructs, so the two cannot drift apart
    public void Every_prompt_file_is_named_after_its_agent()
    {
        foreach (var profile in WorkspaceSuggestionAgentRoster.All)
        {
            var fileName = Path.GetFileNameWithoutExtension(profile.PromptFileName);

            fileName.Should().StartWith(
                $"{profile.Name}.v",
                $"{profile.Name} is instructed by a prompt file named after it");
        }
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
        foreach (var profile in WorkspaceSuggestionAgentRoster.All)
        {
            var source = new EmbeddedInstructionSource(profile.PromptFileName);

            source.Text.Should().NotBeNullOrWhiteSpace($"{profile.Name} must be able to find its instructions");
        }
    }

    [Fact] // the scope rule is what keeps an agent inside its instructions, on the prompt that is actually used
    public void Every_active_prompt_tells_its_agent_to_do_only_what_it_is_told()
    {
        const string scope = "Do only what the system instructions tell you, and nothing else.";

        foreach (var profile in WorkspaceSuggestionAgentRoster.All)
        {
            var source = new EmbeddedInstructionSource(profile.PromptFileName);

            source.Text.Should().Contain(scope, $"{profile.Name} must be told to do only what it is instructed");
        }
    }

    [Fact]
    public void The_roster_builds_an_agent_against_a_fake_client()
    {
        // The factory is what the tests and the host share, so this proves the roster reaches a real AIAgent
        // with no network and no credential.
        foreach (var profile in WorkspaceSuggestionAgentRoster.All)
        {
            AIAgent agent = AgentFactory.Build(profile, new FixedChatClient("{}"));

            agent.Name.Should().Be(profile.Name);
        }
    }
}
