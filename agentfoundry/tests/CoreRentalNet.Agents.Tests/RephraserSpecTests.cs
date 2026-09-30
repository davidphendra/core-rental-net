using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The rephraser's contract: a reading that names only real components, carries no product, says how to look for
/// each part of the workspace, and never invents a ceiling.
/// </summary>
/// <remarks>
/// Run against the roster's own rephraser over a fake chat client, so the profile, its embedded prompt and its
/// output format are the things under test — not the model's judgement, which the evaluation tier grades. The
/// verdict that the sentence is not about a workspace is no longer this agent's: the verifier owns it, and its
/// contract is asserted in <see cref="WorkspaceRequestVerificationContractTests"/>.
/// </remarks>
public sealed class RephraserSpecTests
{
    [Fact]
    public async Task A_vague_sentence_yields_a_reading_that_marks_what_was_asked_for()
    {
        var requirement = await Requirement(WorkspaceRequirementExpansionFixtures.Json());

        requirement.ComponentExpansions.Desk.IsRelevant.Should().BeTrue();
        requirement.ComponentExpansions.Chair.IsRelevant.Should().BeTrue();
        requirement.ComponentExpansions.Every().Should().HaveCount(7, "all seven categories are always present");
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

    [Fact] // the guarantee the seven required properties exist for
    public async Task A_reading_that_omits_a_category_is_refused_rather_than_defaulted()
    {
        const string withoutTheChair =
            """
            { "original_query": "a desk",
              "workspace_intent": { "purpose": [], "style": [], "experience": [], "usage": [] },
              "total_budget": { "amount": null, "currency": null, "is_explicit": false },
              "categories": {
                "desk": { "relevant": true, "retrieval_query": "a desk", "search_terms": ["desk"],
                          "synonyms": [], "semantic_concepts": [],
                          "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } }
              } }
            """;

        var act = async () => await Requirement(withoutTheChair);

        await act.Should().ThrowAsync<JsonException>(
            "a missing category would otherwise default to not-requested and drop what the customer asked for");
    }

    [Fact]
    public void A_reading_carrying_a_sku_a_price_or_a_product_name_is_rejected()
    {
        const string withDeskWords =
            """
            { "relevant": true, "retrieval_query": "a surface",
              "search_terms": ["desk"], "synonyms": [], "semantic_concepts": [],
              "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false }%EXTRA% }
            """;

        // The reading is about what the customer wants and how to look for it, never which product supplies it,
        // so none of these properties exists on it and the schema refuses any of them outright.
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

        var absent = await Requirement(WorkspaceRequirementExpansionFixtures.Json(
            WorkspaceRequirementExpansionFixtures.DeskNotRequested)
            .Replace(
                """{ "amount": 500000, "currency": "IDR", "is_explicit": true }""",
                """{ "amount": null, "currency": null, "is_explicit": false }""",
                StringComparison.Ordinal));

        absent.TotalMonthlyBudget.Amount.Should().BeNull(
            "the agent must not invent a budget the customer never named");
        absent.ComponentExpansions.Desk.IsRelevant.Should().BeFalse();
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

    private static async Task<WorkspaceRequirementExpansion> Requirement(string reply)
    {
        var agent = AgentFactory.Build(WorkspaceSuggestionAgentRoster.Rephraser, new FixedChatClient(reply));

        AgentResponse<WorkspaceRequirementExpansion> response =
            await agent.RunAsync<WorkspaceRequirementExpansion>("a quiet corner for two screens");

        return response.Result;
    }
}
