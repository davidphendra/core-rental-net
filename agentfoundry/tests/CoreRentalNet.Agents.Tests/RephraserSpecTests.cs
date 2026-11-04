using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The rephraser's contract: a compact reading that names only relevant components, carries no product, says how
/// to look for each part of the workspace, and never invents a ceiling.
/// </summary>
/// <remarks>
/// Run against the roster's own rephraser over a fake chat client, so the profile, its embedded prompt and its
/// output format are the things under test. The mapper is exercised too, because the model-facing answer is now
/// sparse: an omitted category is a valid answer that must become an irrelevant domain category.
/// </remarks>
public sealed class RephraserSpecTests
{
    [Fact]
    public async Task A_vague_sentence_yields_a_reading_that_marks_what_was_asked_for()
    {
        var requirement = await Requirement(WorkspaceRequirementExpansionFixtures.Json());

        requirement.ComponentExpansions.Desk.IsRelevant.Should().BeTrue();
        requirement.ComponentExpansions.Chair.IsRelevant.Should().BeTrue();
        requirement.ComponentExpansions.Every().Should().HaveCount(7, "the domain still has all seven categories");
        requirement.ComponentExpansions.Monitor.IsRelevant.Should().BeFalse();
    }

    [Fact] // the words are the whole point of the expansion, and they survive the contract
    public async Task The_reading_carries_the_words_each_search_will_use()
    {
        var requirement = await Requirement(WorkspaceRequirementExpansionFixtures.Json());

        requirement.ComponentExpansions.Desk.SearchTerms.Should().Equal("computer desk", "writing desk");
        requirement.ComponentExpansions.Desk.Synonyms.Should().ContainSingle();
        requirement.ComponentExpansions.Desk.RetrievalQuery.Should()
            .Be("a wide, stable surface for one screen");
        requirement.ComponentExpansions.Desk.SemanticConcepts.Should().ContainSingle();
    }

    [Fact] // an omitted category is a valid compact answer and becomes irrelevant in the domain
    public async Task A_reading_that_omits_an_unnamed_category_is_mapped_as_irrelevant()
    {
        var requirement = await Requirement(OnlyDeskJson(), "a desk");

        requirement.ComponentExpansions.Desk.IsRelevant.Should().BeTrue();
        requirement.ComponentExpansions.Chair.IsRelevant.Should().BeFalse(
            "presence in the compact answer is relevance");
    }

    [Fact]
    public void A_reading_carrying_a_sku_a_price_or_a_product_name_is_rejected()
    {
        const string withDeskWords =
            """
            { "key": "desk", "retrieval_query": "a surface",
              "search_terms": ["desk"], "synonyms": [], "semantic_concepts": []%EXTRA% }
            """;

        var document = WorkspaceRequirementExpansionFixtures.Json(withDeskWords.Replace("%EXTRA%", string.Empty));

        Schemas.EvaluateRaw("workspace-retrieval-requirement.schema.json", document).IsValid.Should().BeTrue();

        foreach (var extra in new[] { ", \"sku\": \"DSKB08XN4JDR\"", ", \"price\": 266000", ", \"name\": \"HON Mod\"" })
        {
            Schemas.EvaluateRaw(
                    "workspace-retrieval-requirement.schema.json",
                    WorkspaceRequirementExpansionFixtures.Json(withDeskWords.Replace("%EXTRA%", extra))).IsValid
                .Should().BeFalse($"a reading may not carry {extra.Trim()}");
        }
    }

    [Fact]
    public async Task A_stated_budget_survives_and_an_absent_one_is_not_invented()
    {
        var stated = await Requirement(WorkspaceRequirementExpansionFixtures.Json());

        stated.TotalMonthlyBudget.Amount.Should().Be(500_000);
        stated.TotalMonthlyBudget.Currency.Should().Be("IDR");
        stated.TotalMonthlyBudget.IsExplicit.Should().BeTrue();
        stated.ComponentExpansions.Desk.MonthlyBudget.MaximumAmount.Should().Be(300_000);
        stated.ComponentExpansions.Desk.MonthlyBudget.IsDerived.Should().BeTrue(
            "a component's share of a total was worked out, not stated");

        var absent = await Requirement(NoBudgetJson(), "a desk and a chair");

        absent.TotalMonthlyBudget.Amount.Should().BeNull(
            "the agent must not invent a budget the customer never named");
        absent.ComponentExpansions.Every()
            .Should().OnlyContain(component => component.Expansion.MonthlyBudget.MaximumAmount == null);
    }

    [Fact] // the intent is cross-category, and it is what the reviewer holds a composed set against
    public async Task The_reading_carries_what_the_workspace_is_for()
    {
        var requirement = await Requirement(WorkspaceRequirementExpansionFixtures.Json());

        requirement.WorkspaceIntent.PurposePhrases.Should().ContainSingle();
        requirement.WorkspaceIntent.StylePhrases.Should().BeEmpty();
        requirement.WorkspaceIntent.ExperiencePhrases.Should().BeEmpty();
        requirement.WorkspaceIntent.UsagePhrases.Should().BeEmpty();
    }

    private static async Task<WorkspaceRequirementExpansion> Requirement(
        string reply,
        string query = "a desk and a chair under 500000")
    {
        var agent = AgentFactory.Build(WorkspaceSuggestionAgentRoster.Rephraser, new FixedChatClient(reply));

        AgentResponse<WorkspaceRequirementExpansionWire> response =
            await agent.RunAsync<WorkspaceRequirementExpansionWire>(query);

        return WorkspaceRequirementExpansionMapper.Map(response.Result, query);
    }

    private static string OnlyDeskJson()
        => $$"""
            { "workspace_intent": { "purpose": ["computer work"] },
              "total_budget": { "amount": null, "currency": "IDR", "is_explicit": false },
              "categories": [ {{WorkspaceRequirementExpansionFixtures.DeskExpansion}} ] }
            """;

    private static string NoBudgetJson()
        => $$"""
            { "workspace_intent": { "purpose": ["computer work"] },
              "total_budget": { "amount": null, "currency": "IDR", "is_explicit": false },
              "categories": [
                {{WorkspaceRequirementExpansionFixtures.DeskExpansion}},
                {{WorkspaceRequirementExpansionFixtures.ChairExpansion}}
              ] }
            """;
}
