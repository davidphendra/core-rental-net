using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// What the framework actually does with output that does not satisfy the contract.
/// </summary>
/// <remarks>
/// <para>
/// The typed result is deserialized <b>lazily</b>: <c>RunAsync&lt;T&gt;</c> returns without complaint and the
/// refusal surfaces when <c>Result</c> is read. Reading the value is therefore part of the contract rather than
/// an optimisation — a stage that never read it would pass a malformed answer downstream unnoticed.
/// </para>
/// <para>
/// <b>The refusal is also only as strong as C#'s deserializer: what cannot be parsed is refused, and what merely
/// violates the schema is accepted.</b> The tests below pin that gap on purpose, which is exactly why a
/// deterministic policy exists downstream of the rephraser — the number of words a search may carry is not a rule
/// the deserializer can be asked to keep. The one exception is a member the contract marks
/// <c>JsonRequired</c>: its absence is a parse failure rather than a default, which is why the seven categories
/// are declared that way.
/// </para>
/// </remarks>
public sealed class InvalidModelOutputTests
{
    [Fact]
    public async Task Prose_instead_of_the_contract_is_refused()
    {
        var act = async () =>
        {
            var response = await Tracer(new FixedChatClient("Sure! Here is a lovely requirement for you."))
                .RunAsync<WorkspaceRequirementExpansion>("a desk and a chair");

            _ = response.Result;
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact] // a member the contract requires, and the reason the seven categories are declared that way
    public async Task A_document_missing_a_required_member_is_refused()
    {
        var act = async () =>
        {
            var response = await Tracer(new FixedChatClient(WithoutTheChair()))
                .RunAsync<WorkspaceRequirementExpansion>("a desk and a chair");

            _ = response.Result;
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task Violations_that_are_structurally_valid_json_are_not_refused_here()
    {
        // The gap, pinned on purpose. A category whose `relevant` is absent would default to false and silently
        // drop a component — except that the member is required, which is the case above. What is still allowed
        // is a value the schema merely discourages: ten words where a search accepts eight. Nothing on the
        // agent's path rejects it, so the vocabulary-limit policy is what must. If the framework ever starts
        // enforcing the schema, this test fails, and the discovery should be told rather than lost.
        var response = await Tracer(new FixedChatClient(WithTenDeskTerms()))
            .RunAsync<WorkspaceRequirementExpansion>("a desk and a chair");

        response.Result.ComponentExpansions.Desk.SearchTerms.Should().HaveCount(10);
        response.Result.ComponentExpansions.Desk.SemanticConcepts.Should().HaveCount(2);
    }

    /// <summary>A complete-looking reading with one of the seven categories simply not there.</summary>
    private static string WithoutTheChair()
        => """
            { "original_query": "a desk",
              "workspace_intent": { "purpose": [], "style": [], "experience": [], "usage": [] },
              "total_budget": { "amount": null, "currency": null, "is_explicit": false },
              "categories": {
                "desk": { "relevant": true, "retrieval_query": "a desk", "search_terms": ["desk"],
                          "synonyms": [], "semantic_concepts": [],
                          "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } }
              } }
            """;

    private static string WithTenDeskTerms()
        => WorkspaceRequirementExpansionFixtures.Json(
            """
            { "relevant": true, "retrieval_query": "a surface",
              "search_terms": ["one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"],
              "synonyms": [], "semantic_concepts": ["a need", "another need"],
              "budget": { "max_amount": null, "currency": null, "is_explicit": false, "is_derived": false } }
            """);

    private static AIAgent Tracer(IChatClient client)
        => new ChatClientAgent(
            client,
            new ChatClientAgentOptions
            {
                Name = "tracer",
                ChatOptions = new ChatOptions
                {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<WorkspaceRequirementExpansion>(),
                },
            });
}
