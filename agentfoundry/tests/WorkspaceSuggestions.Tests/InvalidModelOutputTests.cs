using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Contracts;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// AIWB-03: what the framework actually does with output that does not satisfy the contract.
/// </summary>
/// <remarks>
/// <para>
/// The tracer found that the typed result is deserialized <b>lazily</b>: <c>RunAsync&lt;T&gt;</c> returns
/// without complaint and the refusal surfaces when <c>Result</c> is read. Reading the value is therefore
/// part of the contract rather than an optimisation — an agent that never reads it would pass a
/// malformed answer downstream unnoticed.
/// </para>
/// <para>
/// It also found that the refusal is only as strong as C#'s deserializer: what cannot be parsed is
/// refused, and what merely violates the schema is accepted. The last test below pins that gap
/// deliberately, and it is why the application's validation has to carry <c>quantity ≥ 1</c> and why a
/// stray property is simply never read.
/// </para>
/// </remarks>
public sealed class InvalidModelOutputTests
{
    [Fact]
    public async Task Prose_instead_of_the_contract_is_refused()
    {
        var act = async () =>
        {
            var response = await Tracer(new FixedChatClient("Sure! Here are three lovely setups for you."))
                .RunAsync<SuggestionResult>("a desk and a chair");

            _ = response.Result;
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task A_slot_outside_the_vocabulary_is_refused()
    {
        var act = async () =>
        {
            var response = await Tracer(new FixedChatClient(ResultWithSlot("Sofa")))
                .RunAsync<SuggestionResult>("a desk and a chair");

            _ = response.Result;
        };

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task Violations_that_are_structurally_valid_json_are_not_refused_here()
    {
        // The gap, pinned on purpose. A zero quantity is what the schema forbids and the deserializer
        // allows, so nothing on the agent's path rejects it: the application's validation is what must.
        // If the framework ever starts enforcing the schema, this test fails, and the story should be
        // told rather than the discovery being lost.
        var response = await Tracer(new FixedChatClient(ResultWithQuantity(0)))
            .RunAsync<SuggestionResult>("a desk and a chair");

        response.Result.Options[0].Lines[0].Quantity.Should().Be(0);
    }

    private static string ResultWithSlot(string slot)
        => $$"""
            { "status": "suggested", "reason": null,
              "options": [ { "lines": [ { "slot": "{{slot}}", "sku": "X", "quantity": 1, "why": "w" } ], "rationale": "r" } ] }
            """;

    private static string ResultWithQuantity(int quantity)
        => $$"""
            { "status": "suggested", "reason": null,
              "options": [ { "lines": [ { "slot": "Desk", "sku": "X", "quantity": {{quantity}}, "why": "w" } ], "rationale": "r" } ] }
            """;

    private static AIAgent Tracer(IChatClient client)
        => new ChatClientAgent(
            client,
            new ChatClientAgentOptions
            {
                Name = "tracer",
                ChatOptions = new ChatOptions
                {
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<SuggestionResult>(),
                },
            });
}
