using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>What the framework actually does with output that does not satisfy the compact contract.</summary>
/// <remarks>
/// The typed result is deserialized lazily: <c>RunAsync&lt;T&gt;</c> returns without complaint and the refusal
/// surfaces when <c>Result</c> is read. Reading the value is therefore part of the contract rather than an
/// optimisation.
/// </remarks>
public sealed class InvalidModelOutputTests
{
    [Fact]
    public async Task Prose_instead_of_the_contract_is_refused()
    {
        var act = async () =>
        {
            var response = await Tracer(new FixedChatClient("Sure! Here is a lovely requirement for you."))
                .RunAsync<WorkspaceRequirementExpansionWire>("a desk and a chair");

            _ = response.Result;
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact] // the categories member is required; a missing answer is refused rather than treated as empty
    public async Task A_document_missing_a_required_member_is_refused()
    {
        var act = async () =>
        {
            var response = await Tracer(new FixedChatClient(WithoutCategories()))
                .RunAsync<WorkspaceRequirementExpansionWire>("a desk and a chair");

            _ = response.Result;
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task Violations_that_are_structurally_valid_json_are_not_refused_here()
    {
        // The compact contract is structural only. A category with ten terms is valid JSON; the vocabulary
        // policy downstream is what bounds it. If the framework ever enforces more of the schema, this test
        // fails, and the discovery should be told rather than lost.
        var response = await Tracer(new FixedChatClient(WithTenDeskTerms()))
            .RunAsync<WorkspaceRequirementExpansionWire>("a desk and a chair");

        var expansion = WorkspaceRequirementExpansionMapper.Map(
            response.Result,
            "a desk and a chair");

        expansion.ComponentExpansions.Desk.SearchTerms.Should().HaveCount(10);
        expansion.ComponentExpansions.Desk.SemanticConcepts.Should().HaveCount(2);
    }

    private static string WithoutCategories()
        => """
            { "workspace_intent": { "purpose": ["computer work"] },
              "total_budget": { "amount": null, "currency": "IDR", "is_explicit": false } }
            """;

    private static string WithTenDeskTerms()
        => $$"""
            { "workspace_intent": { "purpose": ["computer work"] },
              "total_budget": { "amount": null, "currency": "IDR", "is_explicit": false },
              "categories": [
                { "key": "desk", "retrieval_query": "a surface",
                  "search_terms": ["one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"],
                  "synonyms": [], "semantic_concepts": ["a need", "another need"] },
                {{WorkspaceRequirementExpansionFixtures.ChairExpansion}}
              ] }
            """;

    private static AIAgent Tracer(IChatClient client)
        => new ChatClientAgent(
            client,
            new ChatClientAgentOptions
            {
                Name = "tracer",
                ChatOptions = new ChatOptions
                {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<WorkspaceRequirementExpansionWire>(),
                },
            });
}
