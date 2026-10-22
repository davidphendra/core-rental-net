using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The rephraser must not turn a room type, an inferred use, or a retry figure into a requirement.</summary>
public sealed class RephraserPromptTests
{
    private static readonly string Prompt =
        new EmbeddedInstructionSource("workspace-requirement-rephraser.v3.md").Text;

    [Fact]
    public void Room_type_does_not_make_a_category_relevant()
    {
        Prompt.Should().Contain("Room type is not a requirement.");
        Prompt.Should().Contain("\"Home office\" does not imply a monitor, a lamp, or a plant.");
        Prompt.Should().NotContain("strongly implies");
    }

    [Fact]
    public void Workspace_intent_cannot_make_a_category_relevant()
    {
        Prompt.Should().Contain("The workspace intent never makes a category relevant.");
        Prompt.Should().Contain("It never changes relevance and it never adds a category.");
    }

    [Fact]
    public void No_budget_fields_are_invented_when_the_request_has_no_total()
    {
        Prompt.Should().Contain("When neither exists, every budget field is null.");
        Prompt.Should().Contain("Never invent a limit, and never derive an allocation from");
        Prompt.Should().NotContain("500000");
    }

    [Fact]
    public void A_previous_search_figure_is_not_a_total()
    {
        Prompt.Should().Contain("Never copy a figure from `previousIssues` or `previousSearches` into `total_budget`.");
        Prompt.Should().Contain("not permission to create a total");
    }

    [Fact]
    public void A_named_variety_always_gets_a_generic_fallback()
    {
        Prompt.Should().Contain("keep that variety and always add the generic category noun as a synonym");
        Prompt.Should().Contain("\"bamboo plant\"` → `search_terms: [\"bamboo plant\"]`, `synonyms: [\"plant\"]");
        Prompt.Should().Contain("Never make a named variety the only term.");
    }

    [Fact]
    public void An_empty_search_without_a_figure_is_not_fixed_by_inventing_a_budget()
    {
        Prompt.Should().Contain("An empty search that named no figure is a catalogue problem.");
        Prompt.Should().Contain("Do not invent a budget.");
        Prompt.Should().Contain("Do not add an unrelated category to fix a catalogue problem.");
    }

    [Fact]
    public void A_brand_or_model_is_searched_as_its_own_term()
    {
        Prompt.Should().Contain("A brand or model is a term of its own.");
        Prompt.Should().Contain("`SertaPedic`");
        Prompt.Should().Contain("[\"VIVO\"");
    }
}
