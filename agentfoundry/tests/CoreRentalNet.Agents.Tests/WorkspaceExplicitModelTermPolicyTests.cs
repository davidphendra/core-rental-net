using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A model number the customer typed survives the rephrasing.</summary>
public sealed class WorkspaceExplicitModelTermPolicyTests
{
    private readonly WorkspaceExplicitModelTermPolicy _policy = new();

    [Fact]
    public void A_model_number_is_added_to_the_nearest_category()
    {
        var result = _policy.Apply(Expansion(
            "a Dell 3007WFP-HC 30-inch monitor and a desk",
            monitorTerms: ["Dell 30-inch monitor"]));

        result.ComponentExpansions.Monitor.SearchTerms.Should().Contain("3007WFP-HC");
        result.ComponentExpansions.Desk.SearchTerms.Should().NotContain("3007WFP-HC");
    }

    [Fact]
    public void A_term_already_present_is_not_duplicated()
    {
        var result = _policy.Apply(Expansion(
            "a Dell 3007WFP-HC monitor",
            monitorTerms: ["Dell 3007WFP-HC"]));

        result.ComponentExpansions.Monitor.SearchTerms.Should().Contain("3007WFP-HC");
    }

    [Fact]
    public void No_model_number_changes_nothing()
    {
        var expansion = Expansion("a desk and a chair", monitorTerms: ["monitor"]);

        _policy.Apply(expansion).Should().BeEquivalentTo(expansion);
    }

    private static WorkspaceRequirementExpansion Expansion(string query, IReadOnlyList<string> monitorTerms)
    {
        var irrelevant = new WorkspaceComponentExpansion(
            IsRelevant: false,
            RetrievalQuery: "",
            SearchTerms: [],
            Synonyms: [],
            SemanticConcepts: [],
            MonthlyBudget: new WorkspaceComponentMonthlyBudget(null, null, false, false));
        var monitor = irrelevant with
        {
            IsRelevant = true,
            RetrievalQuery = "a monitor",
            SearchTerms = monitorTerms,
        };

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: query,
            WorkspaceIntent: new WorkspaceIntent([], [], [], []),
            TotalMonthlyBudget: new WorkspaceTotalMonthlyBudget(null, null, false),
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: irrelevant,
                Chair: irrelevant,
                Monitor: monitor,
                Lamp: irrelevant,
                Plant: irrelevant,
                BeanBag: irrelevant,
                CoffeeMachine: irrelevant));
    }
}
