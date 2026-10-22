using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A retry correction may not become a budget the customer never stated.</summary>
public sealed class WorkspaceRequirementBudgetInvariantPolicyTests
{
    private readonly WorkspaceRequirementBudgetInvariantPolicy _policy = new();

    [Fact]
    public void No_total_and_no_ceiling_clears_every_amount_even_when_the_model_stated_one()
    {
        var result = _policy.ApplyBudgetInvariant(
            Expansion("a desk and a chair", totalAmount: 250_000, componentAmount: 279_000),
            applicationRecordedCeilingMonthly: null);

        result.TotalMonthlyBudget.Amount.Should().BeNull();
        result.ComponentExpansions.Every()
            .Should().OnlyContain(component => component.Expansion.MonthlyBudget.MaximumAmount == null);
        result.ComponentExpansions.Every()
            .Should().OnlyContain(component => !component.Expansion.MonthlyBudget.IsExplicit
                && !component.Expansion.MonthlyBudget.IsDerived);
    }

    [Fact]
    public void An_application_ceiling_becomes_an_explicit_total()
    {
        var result = _policy.ApplyBudgetInvariant(
            Expansion("a desk", totalAmount: null, componentAmount: 279_000),
            applicationRecordedCeilingMonthly: 500_000);

        result.TotalMonthlyBudget.Amount.Should().Be(500_000);
        result.TotalMonthlyBudget.IsExplicit.Should().BeTrue();
    }

    [Fact]
    public void The_sentence_budget_wins_over_a_model_total()
    {
        var result = _policy.ApplyBudgetInvariant(
            Expansion("a desk and a chair under 750000", totalAmount: 500_000, componentAmount: 300_000),
            applicationRecordedCeilingMonthly: null);

        result.TotalMonthlyBudget.Amount.Should().Be(750_000);
        result.TotalMonthlyBudget.IsExplicit.Should().BeTrue();
        result.ComponentExpansions.Desk.MonthlyBudget.MaximumAmount.Should().Be(300_000);
    }

    [Fact]
    public void A_screen_size_is_not_a_budget()
    {
        var result = _policy.ApplyBudgetInvariant(
            Expansion("a 27-inch gaming monitor and a desk", totalAmount: 250_000, componentAmount: 100_000),
            applicationRecordedCeilingMonthly: null);

        result.TotalMonthlyBudget.Amount.Should().BeNull();
    }

    private static WorkspaceRequirementExpansion Expansion(
        string query,
        int? totalAmount,
        int? componentAmount)
    {
        var budget = new WorkspaceComponentMonthlyBudget(
            componentAmount,
            "IDR",
            IsExplicit: false,
            IsDerived: true);
        var component = new WorkspaceComponentExpansion(
            IsRelevant: true,
            RetrievalQuery: "a component",
            SearchTerms: ["component"],
            Synonyms: [],
            SemanticConcepts: [],
            MonthlyBudget: budget);

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: query,
            WorkspaceIntent: new WorkspaceIntent([], [], [], []),
            TotalMonthlyBudget: new WorkspaceTotalMonthlyBudget(totalAmount, "IDR", IsExplicit: false),
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: component,
                Chair: component,
                Monitor: component,
                Lamp: component,
                Plant: component,
                BeanBag: component,
                CoffeeMachine: component));
    }
}
