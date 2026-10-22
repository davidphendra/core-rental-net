using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A room type never turns into an unrequested accessory.</summary>
public sealed class WorkspaceAccessoryRelevancePolicyTests
{
    private readonly WorkspaceAccessoryRelevancePolicy _policy = new();

    [Fact]
    public void A_home_office_does_not_keep_a_monitor_lamp_or_plant()
    {
        var result = _policy.ApplyRequestedAccessories(
            Expansion("a desk and a chair for my home office"));

        result.ComponentExpansions.Monitor.IsRelevant.Should().BeFalse();
        result.ComponentExpansions.Lamp.IsRelevant.Should().BeFalse();
        result.ComponentExpansions.Plant.IsRelevant.Should().BeFalse();
        result.ComponentExpansions.Monitor.SearchTerms.Should().BeEmpty();
        result.ComponentExpansions.Desk.IsRelevant.Should().BeTrue();
        result.ComponentExpansions.Chair.IsRelevant.Should().BeTrue();
    }

    [Fact]
    public void Explicit_accessories_are_kept()
    {
        var result = _policy.ApplyRequestedAccessories(
            Expansion("a desk, a chair, a monitor, a lamp, and a succulent trio"));

        result.ComponentExpansions.Monitor.IsRelevant.Should().BeTrue();
        result.ComponentExpansions.Lamp.IsRelevant.Should().BeTrue();
        result.ComponentExpansions.Plant.IsRelevant.Should().BeTrue();
    }

    [Fact]
    public void A_screen_word_keeps_the_monitor()
    {
        var result = _policy.ApplyRequestedAccessories(
            Expansion("a quiet corner for one screen"));

        result.ComponentExpansions.Monitor.IsRelevant.Should().BeTrue();
    }

    [Fact]
    public void A_coffee_machine_is_kept_but_an_unmentioned_bean_bag_is_not()
    {
        var result = _policy.ApplyRequestedAccessories(
            Expansion("a desk, a chair, and a coffee machine"));

        result.ComponentExpansions.CoffeeMachine.IsRelevant.Should().BeTrue();
        result.ComponentExpansions.BeanBag.IsRelevant.Should().BeFalse();
    }

    private static WorkspaceRequirementExpansion Expansion(string query)
    {
        var budget = new WorkspaceComponentMonthlyBudget(null, "IDR", false, false);
        var relevant = new WorkspaceComponentExpansion(
            IsRelevant: true,
            RetrievalQuery: "a component",
            SearchTerms: ["component"],
            Synonyms: [],
            SemanticConcepts: [],
            MonthlyBudget: budget);

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: query,
            WorkspaceIntent: new WorkspaceIntent([], [], [], []),
            TotalMonthlyBudget: new WorkspaceTotalMonthlyBudget(null, "IDR", false),
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: relevant,
                Chair: relevant,
                Monitor: relevant,
                Lamp: relevant,
                Plant: relevant,
                BeanBag: relevant,
                CoffeeMachine: relevant));
    }
}
