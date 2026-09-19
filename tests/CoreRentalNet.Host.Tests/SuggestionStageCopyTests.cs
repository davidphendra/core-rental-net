using AwesomeAssertions;
using CoreRentalNet.Host.AiBuilder;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The words the application puts around a run: what a customer reads while waiting, and where those words
/// come from.
/// </summary>
/// <remarks>
/// <para>
/// There is no mapping to test, and that is the point of this file. The story expected the agent's stage
/// identities to be translated into these lines, and at `e05s04` it was recorded as <b>not known</b> whether
/// they could reach the application. They cannot: the `workflow_action` items those identities arrive in are
/// produced by <c>Azure.AI.AgentServer.Responses</c>, which is the server side, and no client package the
/// application references has a type for them — <c>Microsoft.Agents.AI.Foundry</c> matches one output-item
/// kind, <c>"message"</c>, over a closed content union.
/// </para>
/// <para>
/// So the stages are the application's own, and these assertions are what stops the agent's vocabulary
/// reaching a customer by some later route: not by checking a mapping, but by checking that the copy
/// contains nothing the agent could have named.
/// </para>
/// </remarks>
public sealed class SuggestionStageCopyTests
{
    [Fact] // e05s07: the sequence is the application's, in the order the phases happen
    public void The_run_says_three_app_owned_lines_in_order()
    {
        SuggestionStageCopy.Sequence.Should().Equal(
            "Reading your request",
            "Matching the catalogue",
            "Checking the suggestion");
    }

    [Fact]
    public void Every_line_is_a_sentence_a_customer_could_read()
    {
        foreach (var line in SuggestionStageCopy.Sequence)
        {
            line.Should().NotBeNullOrWhiteSpace();
            line.Should().MatchRegex("^[A-Z][a-z]+ [a-z]", "each line is prose, not an identifier");
            line.Should().NotContain("_").And.NotContain("{").And.NotContain("<");
        }

        // Guards against the rule passing because the sequence is empty.
        SuggestionStageCopy.Sequence.Should().OnlyHaveUniqueItems().And.HaveCount(3);
    }

    [Fact] // the agent's vocabulary never reaches the customer
    public void No_line_carries_a_word_only_the_agent_could_have_supplied()
    {
        // The executor identities, the workflow verbs they arrive under, and the two agent names. None of
        // these may appear in what a customer reads: they are either internal protocol or the name of a
        // thing the customer was never told exists.
        string[] theAgents = ["rephraser", "suggestor", "executor", "workflow", "InvokeExecutor", "action_id"];

        foreach (var line in SuggestionStageCopy.Sequence)
        {
            foreach (var word in theAgents)
            {
                line.Should().NotContain(word, $"'{line}' is the application's wording, not the agent's");
            }
        }
    }
}
