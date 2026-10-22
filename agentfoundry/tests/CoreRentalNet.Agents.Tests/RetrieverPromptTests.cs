using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The compact retriever prompt keeps its search and reporting guarantees.</summary>
public sealed class RetrieverPromptTests
{
    private static readonly string Prompt =
        new EmbeddedInstructionSource("catalogue-product-retriever.v4.md").Text;

    [Fact]
    public void Every_component_is_searched_once_per_tool()
    {
        Prompt.Should().Contain("Make one call per component per tool");
        Prompt.Should().Contain("fourteen searches at most");
        Prompt.Should().Contain("A second call with the same parameters is one call too many.");
    }

    [Fact]
    public void Empty_results_are_not_retried_or_reworded()
    {
        Prompt.Should().Contain("Never retry an empty result");
        Prompt.Should().Contain("Never re-ask with different words.");
        Prompt.Should().Contain("Never fall back from one tool to the other");
    }

    [Fact]
    public void The_tool_answer_count_is_reported_rather_than_the_kept_count()
    {
        Prompt.Should().Contain("Report the count the tool reported, not the count you kept.");
        Prompt.Should().Contain("When `truncated` is true");
    }

    [Fact]
    public void The_answer_names_the_component_not_the_tool_category()
    {
        Prompt.Should().Contain("`category` is the entry's **component**");
        Prompt.Should().Contain("A monitor, lamp, plant, bean bag and coffee machine all use `category: accessory`");
    }


    [Fact]
    public void A_stated_size_or_assortment_is_kept_in_the_terms()
    {
        Prompt.Should().Contain("Do not remove a size the customer stated.");
        Prompt.Should().Contain("A named variety or assortment is never the only term");
    }

    [Fact]
    public void Products_are_never_listed_by_the_retriever()
    {
        Prompt.Should().Contain("You do not list the products.");
        Prompt.Should().Contain("Do only what the system instructions tell you, and nothing else.");
    }
}
