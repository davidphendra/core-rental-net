using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The rephraser must stay compact without turning a room type or a retry figure into a requirement.</summary>
public sealed class RephraserPromptTests
{
    private static readonly string Prompt =
        new EmbeddedInstructionSource("workspace-requirement-rephraser.v6.md").Text;

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
    public void Only_relevant_categories_are_returned()
    {
        Prompt.Should().Contain("Return only relevant categories.");
        Prompt.Should().Contain("An omitted category is irrelevant.");
        Prompt.Should().Contain("Return at most one entry per key.");
        Prompt.Should().NotContain("Every one of the seven categories is always present.");
        Prompt.Should().Contain("\"categories\": [");
        Prompt.Should().NotContain("\"relevant\":");
        Prompt.Should().NotContain("\"max_amount\"");
    }

    [Fact]
    public void The_output_does_not_echo_the_customer_sentence_or_repeat_a_per_category_budget()
    {
        Prompt.Should().Contain("Do not return `original_query`.");
        Prompt.Should().Contain("Do not return a per-category budget.");
        Prompt.Should().Contain("\"allocations\"");
        Prompt.Should().Contain("Every allocation key must appear in `categories`.");
    }

    [Fact]
    public void No_budget_fields_are_invented_when_the_request_has_no_total()
    {
        Prompt.Should().Contain("When neither exists, `total_budget.amount` is null and `allocations` must be omitted entirely.");
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
    public void The_restored_word_and_budget_rules_remain_in_the_compact_prompt()
    {
        Prompt.Should().Contain("A term longer than eighty characters is refused.");
        Prompt.Should().Contain("A term must be broad enough that a catalogue could contain it.");
        Prompt.Should().Contain("Adding a word narrows");
        Prompt.Should().Contain("Values are integers in the currency you were given.");
        Prompt.Should().Contain("Budget words are not numbers.");
        Prompt.Should().Contain("A budget never changes what the customer asked for.");
    }

    [Fact]
    public void The_composition_and_verdict_remain_outside_the_rephraser()
    {
        Prompt.Should().Contain("The composition is not yours.");
        Prompt.Should().Contain("Quantities are never search terms.");
        Prompt.Should().Contain("The verdict is not yours.");
        Prompt.Should().Contain("The key is a catalogue category, not a slot of the workspace.");
    }

    [Fact]
    public void A_found_component_is_not_treated_as_a_problem_on_retry()
    {
        Prompt.Should().Contain("A component that either of its searches found a product for is answered.");
        Prompt.Should().Contain("The feedback is only ever the previous attempt's.");
    }


    [Fact]
    public void A_retry_must_change_a_search_that_found_nothing()
    {
        Prompt.Should().Contain(
            "Do not repeat the same search terms for a component whose previous searches found nothing.");
        Prompt.Should().Contain("Repeating the same terms is not a correction; widen or change them.");
    }

    [Fact]
    public void A_brand_or_model_is_searched_as_its_own_term()
    {
        Prompt.Should().Contain("A brand or model is a term of its own.");
        Prompt.Should().Contain("[\"SertaPedic\"");
        Prompt.Should().Contain("[\"VIVO\"");
    }
}
