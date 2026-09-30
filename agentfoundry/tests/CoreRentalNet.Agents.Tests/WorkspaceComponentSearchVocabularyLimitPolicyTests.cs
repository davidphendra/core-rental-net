using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The bound on what one component's search may carry: eight words together, primary terms before synonyms.
/// </summary>
/// <remarks>
/// The bound exists because recall is already saturated well below it — measured over the real catalogue, three
/// terms within one category return the whole category — so a ninth word buys almost nothing while it dilutes the
/// ranking the answer's cap is spent on. What is asserted here is the arithmetic and the order, which is the part
/// a prompt cannot guarantee.
/// </remarks>
public sealed class WorkspaceComponentSearchVocabularyLimitPolicyTests
{
    private static readonly WorkspaceComponentSearchVocabularyLimitPolicy Policy = new();

    [Fact]
    public void More_primary_terms_than_the_bound_are_cut_and_the_synonyms_fill_nothing()
    {
        var bounded = Policy.ApplySearchVocabularyLimits(
            WithDeskWords(
                searchTerms: ["one", "two", "three", "four", "five", "six", "seven", "eight", "nine"],
                synonyms: ["synonym"]));

        bounded.ComponentExpansions.Desk.SearchTerms.Should().Equal(
            ["one", "two", "three", "four", "five", "six", "seven", "eight"]);
        bounded.ComponentExpansions.Desk.Synonyms.Should().BeEmpty(
            "the bound is on the words together, and the primary terms are the ones that survive it");
    }

    [Fact]
    public void The_synonyms_fill_whatever_the_primary_terms_left()
    {
        var bounded = Policy.ApplySearchVocabularyLimits(
            WithDeskWords(
                searchTerms: ["one", "two", "three"],
                synonyms: ["four", "five", "six", "seven", "eight", "nine"]));

        bounded.ComponentExpansions.Desk.SearchTerms.Should().Equal(["one", "two", "three"]);
        bounded.ComponentExpansions.Desk.Synonyms.Should().Equal(["four", "five", "six", "seven", "eight"]);
    }

    [Fact]
    public void A_term_longer_than_the_bound_is_dropped_and_the_next_one_takes_its_place()
    {
        var tooLong = new string('a', 81);

        var bounded = Policy.ApplySearchVocabularyLimits(
            WithDeskWords(searchTerms: [tooLong, "keeper"], synonyms: []));

        bounded.ComponentExpansions.Desk.SearchTerms.Should().Equal(
            ["keeper"],
            "a term a tool would refuse must not be the term that spends a place");
    }

    [Fact]
    public void A_blank_term_is_dropped()
    {
        var bounded = Policy.ApplySearchVocabularyLimits(
            WithDeskWords(searchTerms: ["   ", "keeper"], synonyms: [string.Empty]));

        bounded.ComponentExpansions.Desk.SearchTerms.Should().Equal(["keeper"]);
        bounded.ComponentExpansions.Desk.Synonyms.Should().BeEmpty();
    }

    [Fact] // the seven are bounded one by one, and the enumeration is the only place that knows how many there are
    public void Every_component_category_is_bounded_and_not_only_the_first()
    {
        var everyCategoryOverItsBound = new[]
        {
            "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        };

        var bounded = Policy.ApplySearchVocabularyLimits(
            EveryCategoryWith(everyCategoryOverItsBound, []));

        bounded.ComponentExpansions.Every().Should().OnlyContain(
            component => component.Expansion.SearchTerms.Count
                == WorkspaceSuggestionWorkflowOptions.DefaultMaximumSearchTermCountPerComponent);
    }

    [Fact] // nothing but the words moves: the bound is not the place a reading is changed
    public void Nothing_else_on_the_expansion_is_changed()
    {
        var expansion = WithDeskWords(searchTerms: ["one", "two"], synonyms: ["three"]);

        var bounded = Policy.ApplySearchVocabularyLimits(expansion);

        bounded.Should().BeEquivalentTo(expansion);
        bounded.ComponentExpansions.Desk.RetrievalQuery.Should().Be(expansion.ComponentExpansions.Desk.RetrievalQuery);
        bounded.ComponentExpansions.Desk.MonthlyBudget.Should().Be(expansion.ComponentExpansions.Desk.MonthlyBudget);
        bounded.TotalMonthlyBudget.Should().Be(expansion.TotalMonthlyBudget);
        bounded.WorkspaceIntent.Should().Be(expansion.WorkspaceIntent);
    }

    /// <summary>The document with the words under test on the desk and nothing on any other component.</summary>
    private static WorkspaceRequirementExpansion WithDeskWords(
        IReadOnlyList<string> searchTerms,
        IReadOnlyList<string> synonyms)
    {
        var desk = Expansion(isRelevant: true, searchTerms, synonyms);

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: "a desk for a small room",
            WorkspaceIntent: new WorkspaceIntent([], [], [], []),
            TotalMonthlyBudget: new WorkspaceTotalMonthlyBudget(500_000, "IDR", IsExplicit: true),
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: desk,
                Chair: Expansion(isRelevant: false, [], []),
                Monitor: Expansion(isRelevant: false, [], []),
                Lamp: Expansion(isRelevant: false, [], []),
                Plant: Expansion(isRelevant: false, [], []),
                BeanBag: Expansion(isRelevant: false, [], []),
                CoffeeMachine: Expansion(isRelevant: false, [], [])));
    }

    /// <summary>The document with the same words on all seven components.</summary>
    private static WorkspaceRequirementExpansion EveryCategoryWith(
        IReadOnlyList<string> searchTerms,
        IReadOnlyList<string> synonyms)
    {
        var everyComponent = Expansion(isRelevant: true, searchTerms, synonyms);

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: "a workspace",
            WorkspaceIntent: new WorkspaceIntent([], [], [], []),
            TotalMonthlyBudget: new WorkspaceTotalMonthlyBudget(null, null, IsExplicit: false),
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: everyComponent,
                Chair: everyComponent,
                Monitor: everyComponent,
                Lamp: everyComponent,
                Plant: everyComponent,
                BeanBag: everyComponent,
                CoffeeMachine: everyComponent));
    }

    private static WorkspaceComponentExpansion Expansion(
        bool isRelevant,
        IReadOnlyList<string> searchTerms,
        IReadOnlyList<string> synonyms)
        => new(
            IsRelevant: isRelevant,
            RetrievalQuery: "a small, plain surface for one screen",
            SearchTerms: searchTerms,
            Synonyms: synonyms,
            SemanticConcepts: ["a compact home office"],
            MonthlyBudget: new WorkspaceComponentMonthlyBudget(200_000, "IDR", IsExplicit: false, IsDerived: true));
}
