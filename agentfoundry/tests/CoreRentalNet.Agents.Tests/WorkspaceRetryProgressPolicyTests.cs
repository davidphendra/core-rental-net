using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A retry that repeats its predecessor's words is not a new question.</summary>
public sealed class WorkspaceRetryProgressPolicyTests
{
    private readonly WorkspaceRetryProgressPolicy _policy = new();

    [Fact]
    public void A_first_attempt_has_no_predecessor_and_is_allowed()
        => _policy.WouldChangeSearch(null, Expansion(["desk"]), Searches(0)).Should().BeTrue();

    [Fact]
    public void Repeating_every_search_term_is_not_progress()
        => _policy.WouldChangeSearch(Expansion(["desk"]), Expansion(["desk"]), Searches(0)).Should().BeFalse();

    [Fact]
    public void Changing_a_search_term_is_progress()
        => _policy.WouldChangeSearch(Expansion(["desk"]), Expansion(["standing desk"]), Searches(0))
            .Should().BeTrue();

    [Fact]
    public void Adding_a_synonym_is_progress()
    {
        var previous = Expansion(["desk"]);
        var current = Expansion(["desk"], ["workstation"]);

        _policy.WouldChangeSearch(previous, current, Searches(0)).Should().BeTrue();
    }


    [Fact]
    public void A_retry_after_a_found_search_is_allowed_even_with_the_same_words()
        => _policy.WouldChangeSearch(Expansion(["desk"]), Expansion(["desk"]), Searches(1))
            .Should().BeTrue();

    private static WorkspaceRequirementExpansion Expansion(
        IReadOnlyList<string> terms,
        IReadOnlyList<string>? synonyms = null)
    {
        var relevant = new WorkspaceComponentExpansion(
            IsRelevant: true,
            RetrievalQuery: "a desk",
            SearchTerms: terms,
            Synonyms: synonyms ?? [],
            SemanticConcepts: [],
            MonthlyBudget: new WorkspaceComponentMonthlyBudget(null, null, false, false));
        var irrelevant = relevant with { IsRelevant = false, SearchTerms = [], Synonyms = [] };

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: "a desk",
            WorkspaceIntent: new WorkspaceIntent([], [], [], []),
            TotalMonthlyBudget: new WorkspaceTotalMonthlyBudget(null, null, false),
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: relevant,
                Chair: irrelevant,
                Monitor: irrelevant,
                Lamp: irrelevant,
                Plant: irrelevant,
                BeanBag: irrelevant,
                CoffeeMachine: irrelevant));
    }

    private static CatalogueProductRetrievalResult Searches(int found)
        => new(
            IsAvailable: true,
            UnavailableReason: null,
            [new WorkspaceComponentSearchOutcome(
                "search_catalogue", WorkspaceComponentCategory.Desk, found, null)]);
}
