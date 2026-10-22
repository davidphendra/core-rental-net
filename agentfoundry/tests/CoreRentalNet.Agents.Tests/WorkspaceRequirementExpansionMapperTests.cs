using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The compact model answer becomes the dense domain expansion, or is refused.</summary>
/// <remarks>
/// Omission is the compact contract's normal way to say "irrelevant", so the mapper must fill the missing
/// categories. It must not do the same for a category the customer named, and it must not let an allocation
/// escape the set of categories the answer returned.
/// </remarks>
public sealed class WorkspaceRequirementExpansionMapperTests
{
    [Fact]
    public void A_sparse_answer_fills_every_missing_category_as_irrelevant()
    {
        var result = WorkspaceRequirementExpansionMapper.Map(
            Wire(categories: [Desk(), Chair()]),
            "a desk and a chair");

        result.ComponentExpansions.Every().Should().HaveCount(7);
        result.ComponentExpansions.Monitor.IsRelevant.Should().BeFalse();
        result.ComponentExpansions.Monitor.SearchTerms.Should().BeEmpty();
        result.ComponentExpansions.Desk.IsRelevant.Should().BeTrue();
    }

    [Fact]
    public void The_customer_sentence_is_copied_rather_than_asked_of_the_model()
    {
        var result = WorkspaceRequirementExpansionMapper.Map(
            Wire(categories: [Desk()]),
            "a desk for a home office");

        result.OriginalCustomerQuery.Should().Be("a desk for a home office");
    }

    [Fact]
    public void Omitted_intent_lists_become_empty_lists()
    {
        var result = WorkspaceRequirementExpansionMapper.Map(
            Wire(categories: [Desk()], intent: new WorkspaceIntentWire(PurposePhrases: ["computer work"])),
            "a desk");

        result.WorkspaceIntent.PurposePhrases.Should().Equal("computer work");
        result.WorkspaceIntent.StylePhrases.Should().BeEmpty();
        result.WorkspaceIntent.ExperiencePhrases.Should().BeEmpty();
        result.WorkspaceIntent.UsagePhrases.Should().BeEmpty();
    }

    [Fact]
    public void Allocations_become_derived_component_budgets()
    {
        var result = WorkspaceRequirementExpansionMapper.Map(
            Wire(
                categories: [Desk(), Chair()],
                allocations:
                [
                    new WorkspaceComponentAllocationWire(WorkspaceComponentCategory.Desk, 300_000),
                    new WorkspaceComponentAllocationWire(WorkspaceComponentCategory.Chair, 200_000),
                ]),
            "a desk and a chair under 500000");

        result.ComponentExpansions.Desk.MonthlyBudget.MaximumAmount.Should().Be(300_000);
        result.ComponentExpansions.Desk.MonthlyBudget.Currency.Should().Be("IDR");
        result.ComponentExpansions.Desk.MonthlyBudget.IsDerived.Should().BeTrue();
        result.ComponentExpansions.Desk.MonthlyBudget.IsExplicit.Should().BeFalse();
    }

    [Fact]
    public void A_category_without_an_allocation_keeps_a_null_budget()
    {
        var result = WorkspaceRequirementExpansionMapper.Map(
            Wire(categories: [Desk(), Chair()], allocations: []),
            "a desk and a chair");

        result.ComponentExpansions.Desk.MonthlyBudget.MaximumAmount.Should().BeNull();
    }

    [Fact]
    public void A_repeated_category_is_refused()
    {
        var act = () => WorkspaceRequirementExpansionMapper.Map(
            Wire(categories: [Desk(), Desk()]),
            "a desk");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void An_allocation_for_an_omitted_category_is_refused()
    {
        var act = () => WorkspaceRequirementExpansionMapper.Map(
            Wire(
                categories: [Desk()],
                allocations: [new WorkspaceComponentAllocationWire(
                    WorkspaceComponentCategory.Chair, 100_000)]),
            "a desk");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void An_allocation_without_a_total_is_ignored()
    {
        var result = WorkspaceRequirementExpansionMapper.Map(
            Wire(
                categories: [Desk()],
                total: new WorkspaceTotalMonthlyBudget(null, "IDR", IsExplicit: false),
                allocations: [new WorkspaceComponentAllocationWire(
                    WorkspaceComponentCategory.Desk, 100_000)]),
            "a desk");

        result.TotalMonthlyBudget.Amount.Should().BeNull();
        result.ComponentExpansions.Desk.MonthlyBudget.MaximumAmount.Should().BeNull(
            "a model that echoed the allocation example must not invent a budget");
    }

    [Fact]
    public void An_allocation_sum_above_the_total_is_refused()
    {
        var act = () => WorkspaceRequirementExpansionMapper.Map(
            Wire(
                categories: [Desk(), Chair()],
                allocations:
                [
                    new WorkspaceComponentAllocationWire(WorkspaceComponentCategory.Desk, 400_000),
                    new WorkspaceComponentAllocationWire(WorkspaceComponentCategory.Chair, 200_000),
                ]),
            "a desk and a chair under 500000");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void An_explicitly_named_category_cannot_be_omitted()
    {
        var act = () => WorkspaceRequirementExpansionMapper.Map(
            Wire(categories: [Desk()]),
            "a desk and a chair");

        act.Should().Throw<JsonException>();
    }

    private static WorkspaceRequirementExpansionWire Wire(
        IReadOnlyList<WorkspaceComponentExpansionWire> categories,
        WorkspaceIntentWire? intent = null,
        WorkspaceTotalMonthlyBudget? total = null,
        IReadOnlyList<WorkspaceComponentAllocationWire>? allocations = null)
        => new(
            WorkspaceIntent: intent ?? new WorkspaceIntentWire(),
            TotalMonthlyBudget: total ?? new WorkspaceTotalMonthlyBudget(500_000, "IDR", IsExplicit: true),
            ComponentExpansions: categories,
            Allocations: allocations);

    private static WorkspaceComponentExpansionWire Desk()
        => new(
            WorkspaceComponentCategory.Desk,
            "a desk for a home office",
            SearchTerms: ["desk"],
            Synonyms: ["workstation"],
            SemanticConcepts: ["surface for daily work"]);

    private static WorkspaceComponentExpansionWire Chair()
        => new(
            WorkspaceComponentCategory.Chair,
            "a chair for a home office",
            SearchTerms: ["chair"],
            Synonyms: ["task chair"],
            SemanticConcepts: ["seating for daily work"]);
}
