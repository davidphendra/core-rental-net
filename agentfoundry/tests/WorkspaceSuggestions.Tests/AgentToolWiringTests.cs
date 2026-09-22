using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Composition;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// The catalogue's tools reach the agent that reads the catalogue, and only that one.
/// </summary>
/// <remarks>
/// The rephraser turns a sentence into a specification and names no product, so a tool would let it choose one
/// before the specification exists. The tools are discovered at runtime, so what is asserted is the wiring -
/// which profile the composition hands them to - and that the factory puts what it is given on the agent.
/// </remarks>
public sealed class AgentToolWiringTests
{
    private static readonly IReadOnlyList<AITool> Catalogue =
    [
        AIFunctionFactory.Create(() => "a catalogue answer", "search_catalogue", "Finds catalogue products."),
    ];

    [Fact]
    public void The_suggestor_is_handed_the_catalogue_tools()
    {
        AgentRegistration.ToolsFor(AgentRoster.Suggestor, Catalogue).Should().BeSameAs(Catalogue);
    }

    [Fact]
    public void The_rephraser_is_handed_none()
    {
        AgentRegistration.ToolsFor(AgentRoster.Rephraser, Catalogue).Should().BeEmpty();
    }

    [Fact]
    public void The_factory_puts_the_tools_it_is_given_on_the_agents_chat_options()
    {
        var suggestor = AgentFactory.Build(AgentRoster.Suggestor, new ScriptedChatClient("{}"), Catalogue);

        Tools(suggestor).Should().ContainSingle().Which.Name.Should().Be("search_catalogue");
    }

    [Fact]
    public void An_agent_with_no_tools_is_given_none_rather_than_an_empty_list()
    {
        var rephraser = AgentFactory.Build(AgentRoster.Rephraser, new ScriptedChatClient("{}"), []);

        Tools(rephraser).Should().BeNull("an empty tool array would tell the model it may call nothing");
    }

    private static IReadOnlyList<AITool>? Tools(AIAgent agent)
        => agent.GetService(typeof(ChatOptions)) is ChatOptions options ? options.Tools?.ToList() : null;
}
