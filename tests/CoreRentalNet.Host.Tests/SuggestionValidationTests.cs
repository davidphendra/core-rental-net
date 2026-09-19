using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-16 and AIWB-31 to AIWB-35: the trust boundary. An agent's answer becomes an application fact here, or
/// it becomes nothing at all.
/// </summary>
/// <remarks>
/// Every test below is the same rule seen from a different side: <b>all or nothing</b>. One line that cannot
/// be honoured fails the whole run rather than being quietly dropped, because a partially honoured suggestion
/// is a lie about the price.
/// </remarks>
public sealed class SuggestionValidationTests
{
    private const string Desk = "DSKB08XN4JDR";
    private const string AnotherDesk = "DSKB0B6MCK87";
    private const string DearestDesk = "DSKB07PFFFQ2";
    private const string Chair = "CHAB091PDL8V";
    private const string Monitor = "MONB001AO2QL";

    [Fact] // AIWB-16
    public void A_valid_answer_becomes_candidates_named_and_priced_by_the_catalogue()
    {
        var (valid, candidates) = Validate(Suggested(Option(
            "An uncluttered setup for one person.",
            Line(SlotId.Desk, Desk, 1),
            Line(SlotId.Chair, Chair, 1))));

        valid.Should().BeTrue();

        var candidate = candidates.Should().ContainSingle().Subject;

        // The name and the amount are the catalogue's. The agent named a SKU and a quantity and nothing else.
        candidate.Lines[0].Name.Should().Be("Product DSKB08XN4JDR");
        candidate.Lines[0].LineTotal.Should().Be(266_000m);
        candidate.MonthlyTotal.Should().Be(266_000m + 96_000m);
        candidate.Rationale.Should().Be("An uncluttered setup for one person.");
    }

    [Fact] // AIWB-16, and the point of recomputing: the agent cannot state a price
    public void The_amount_is_the_catalogue_s_even_where_the_agent_named_a_quantity()
    {
        var (valid, candidates) = Validate(Suggested(Option(
            "Two displays, side by side.",
            Line(SlotId.Monitor, Monitor, 2))));

        valid.Should().BeTrue("the monitor slot accepts three");

        var candidate = candidates.Should().ContainSingle().Subject;

        candidate.Lines[0].LineTotal.Should().Be(227_000m * 2);
        candidate.MonthlyTotal.Should().Be(454_000m);
    }

    [Fact] // AIWB-31
    public void A_SKU_the_catalogue_does_not_hold_fails_the_whole_run()
    {
        var (valid, candidates) = Validate(Suggested(Option(
            "A setup that cannot be priced.",
            Line(SlotId.Desk, Desk, 1),
            Line(SlotId.Chair, "NOT-A-SKU", 1))));

        valid.Should().BeFalse("one line the catalogue cannot honour fails all of it");
        candidates.Should().BeEmpty("nothing is shown from a run that failed");
    }

    [Fact] // AIWB-32, and the reason this rule is here at all
    public void A_quantity_of_zero_fails_the_whole_run()
    {
        // Nothing upstream rejects this. The framework's deserializer accepts a zero silently and the schema
        // that forbids it is not enforced on the agent's path, so this check is the only thing between a
        // customer and a line nobody can rent.
        var (valid, candidates) = Validate(Suggested(Option("A line of nothing.", Line(SlotId.Desk, Desk, 0))));

        valid.Should().BeFalse();
        candidates.Should().BeEmpty();
    }

    [Fact] // AIWB-32, the other bound
    public void A_quantity_over_what_the_slot_accepts_fails_the_whole_run()
    {
        var (valid, candidates) = Validate(
            Suggested(Option("Two displays.", Line(SlotId.Monitor, Monitor, 2))),
            slots: new WorkspaceSlotSettings(Monitor: 1));

        valid.Should().BeFalse("the capacity configured here is one monitor");
        candidates.Should().BeEmpty();
    }

    [Fact] // AIWB-33
    public void A_product_that_does_not_belong_in_its_slot_fails_the_whole_run()
    {
        // A desk in the chair's slot: the SKU exists and the quantity is fine, so this is the rule that has to
        // catch it - the catalogue decides which slot a product may fill, not the agent.
        var (valid, candidates) = Validate(Suggested(Option(
            "A desk where a chair goes.",
            Line(SlotId.Chair, Desk, 1))));

        valid.Should().BeFalse();
        candidates.Should().BeEmpty();
    }

    [Fact] // the shape of "all-or-nothing", stated rather than left to be inferred
    public void One_bad_option_fails_every_option()
    {
        var (valid, candidates) = Validate(Suggested(
            Option("A good one.", Line(SlotId.Desk, Desk, 1)),
            Option("A bad one.", Line(SlotId.Chair, "NOT-A-SKU", 1))));

        valid.Should().BeFalse("the good option is not shown either: a partial answer is a lie about the price");
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void An_answer_that_says_it_suggested_something_and_shows_nothing_is_not_a_suggestion()
    {
        var (valid, candidates) = Validate(Suggested());

        valid.Should().BeFalse();
        candidates.Should().BeEmpty();
    }

    [Fact] // AIWB-34
    public void Three_two_and_one_are_each_labelled_by_rank()
    {
        Labelled(
            Option("Cheapest.", Line(SlotId.Desk, Desk, 1)),
            Option("Middle.", Line(SlotId.Desk, AnotherDesk, 1)),
            Option("Dearest.", Line(SlotId.Desk, DearestDesk, 1)))
            .Should().Equal("Budget", "Balanced", "Premium");

        // Two that genuinely spread, so this is about the labelling rather than about the spread rule.
        Labelled(
            Option("Cheapest.", Line(SlotId.Desk, Desk, 1)),
            Option("Dearest.", Line(SlotId.Desk, DearestDesk, 1)))
            .Should().Equal("Budget", "Balanced");

        Labelled(Option("The only one.", Line(SlotId.Desk, Desk, 1)))
            .Should().Equal("Budget");
    }

    [Fact] // the point of sorting: the order is the application's, not the model's
    public void Candidates_are_ordered_by_what_the_catalogue_charges_rather_than_by_the_order_the_model_wrote()
    {
        var (valid, candidates) = Validate(Suggested(
            Option("Dearest first.", Line(SlotId.Desk, DearestDesk, 1)),
            Option("Cheapest second.", Line(SlotId.Desk, Desk, 1))));

        valid.Should().BeTrue();

        candidates.Select(candidate => candidate.MonthlyTotal)
            .Should().BeInAscendingOrder("a customer reads a range from its affordable end");
    }

    [Fact] // AIWB-35, and the reason "fewer" means one
    public void Options_that_cannot_spread_are_shown_as_one_rather_than_as_a_false_range()
    {
        // 266,000 against 271,000 is a factor of 1.02 - no range at all.
        var (valid, candidates) = Validate(Suggested(
            Option("One.", Line(SlotId.Desk, Desk, 1)),
            Option("Another.", Line(SlotId.Desk, AnotherDesk, 1))));

        valid.Should().BeTrue("the run did not fail: fewer are shown rather than padded or failed");

        // And it is provably one rather than a choice: no subset of two or more can satisfy a range the whole
        // set fails, because dropping the cheapest raises the minimum and dropping the dearest lowers the
        // maximum, so either makes the range narrower.
        var candidate = candidates.Should().ContainSingle().Subject;

        candidate.MonthlyTotal.Should().Be(266_000m, "the affordable end is the honest one to show alone");
    }

    [Fact] // the range rule, and explicitly not a rule about the middle
    public void A_middle_option_needs_no_margin_of_its_own()
    {
        // 266,000 to 533,000 spans a factor of two, and the chair sitting between them is neither end. A margin
        // on the middle would refuse an option the range had already accepted.
        var (valid, candidates) = Validate(Suggested(
            Option("Cheapest.", Line(SlotId.Desk, Desk, 1)),
            Option("The middle.", Line(SlotId.Desk, AnotherDesk, 1), Line(SlotId.Chair, Chair, 1)),
            Option("Dearest.", Line(SlotId.Desk, DearestDesk, 1))));

        valid.Should().BeTrue();
        candidates.Should().HaveCount(3, "the range holds, so nothing is dropped from the middle");
    }

    private static string[] Labelled(params AgentSuggestionOption[] options)
    {
        var (valid, candidates) = Validate(Suggested(options));

        valid.Should().BeTrue();

        return [.. candidates.Select(candidate => candidate.Label)];
    }

    private static (bool Valid, IReadOnlyList<SuggestionCandidate> Candidates) Validate(
        AgentSuggestionResult result,
        WorkspaceSlotSettings? slots = null)
    {
        var valid = Validator(Catalogue(), slots ?? new WorkspaceSlotSettings())
            .TryValidate(result, out var candidates);

        return (valid, candidates);
    }

    private static SuggestionValidator Validator(TestCatalogue catalogue, WorkspaceSlotSettings slots)
        => new(catalogue, slots, new SuggestionSpread(SuggestionSpread.DefaultFactor));

    /// <summary>
    /// Three desks that spread, so the range rule holds and a labelling test is about labelling rather than
    /// about the spread. A fixture whose options clustered would fail those tests for the wrong reason.
    /// </summary>
    private static TestCatalogue Catalogue()
        => new TestCatalogue()
            .Add(Desk, 266_000m, CatalogCategory.Desk, subCategory: null)
            .Add(AnotherDesk, 271_000m, CatalogCategory.Desk, subCategory: null)
            .Add(DearestDesk, 533_000m, CatalogCategory.Desk, subCategory: null)
            .Add(Chair, 96_000m, CatalogCategory.Chair, subCategory: null)
            .Add(Monitor, 227_000m, CatalogCategory.Accessory, CatalogSubCategory.Monitor);

    private static AgentSuggestionResult Suggested(params AgentSuggestionOption[] options)
        => new(AgentSuggestionStatus.Suggested, null, options);

    private static AgentSuggestionOption Option(string rationale, params AgentSuggestionLine[] lines)
        => new(lines, rationale);

    private static AgentSuggestionLine Line(SlotId slot, string sku, int quantity)
        => new(slot, sku, quantity, "a purpose");
}
