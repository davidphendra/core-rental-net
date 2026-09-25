using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Agents.AI;
using WorkspaceSuggestions.Agents;
using WorkspaceSuggestions.Contracts;
using WorkspaceSuggestions.Workflows;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// AIWB-09 to AIWB-12: the pipeline — rephraser then suggestor, in that order, with the rephraser's
/// specification still in front of the suggestor.
/// </summary>
/// <remarks>
/// <para>
/// Both agents share one scripted client, so the script order <i>is</i> the call order: the first reply is the
/// specification, the second the result. If the workflow ran them the other way round, the first reply would
/// be deserialized as the wrong contract and fail.
/// </para>
/// <para>
/// <b>Found here, and it matters downstream:</b> a sequential workflow's response carries <b>both</b> agents'
/// answers, in order — the specification first, the result last. <c>RunAsync&lt;T&gt;</c> deserializes the
/// <b>first</b> top-level object, so it would return the specification and fail. The terminal answer has to be
/// read as the <b>last</b> object, which is what <see cref="ReadLast{T}"/> does and what the application's
/// adapter has to do for itself in <c>e05s05</c> — it cannot share this code, because the two trees have no
/// reference to each other.
/// </para>
/// </remarks>
public sealed class WorkflowTests
{
    private const string Spec =
        """
        { "status": "spec", "reason": null, "ceilingMonthly": 1500000,
          "slots": [ { "slot": "Desk", "quantity": 1, "purpose": "a wide, stable surface" } ],
          "constraints": [ "a small room" ] }
        """;

    private const string Result =
        """
        { "status": "suggested", "reason": null,
          "options": [ { "lines": [ { "slot": "Desk", "sku": "DSKB08XN4JDR", "name": "Sit-Stand Desk", "quantity": 1, "amount": 4200000, "why": "a stable surface" } ],
                         "rationale": "A calm, focused setup for a small room." } ] }
        """;

    /// <summary>The request as the application sends it: the sentence and the slot rules, and no catalogue.</summary>
    private const string Request =
        """
        { "runId": "run-1", "query": "a quiet corner for one screen", "currency": "IDR", "ceilingMonthly": 1500000,
          "slots": [ { "slot": "Desk", "capacity": 1 }, { "slot": "Monitor", "capacity": 3 } ] }
        """;

    [Fact] // AIWB-09
    public async Task The_workflow_runs_rephraser_then_suggestor_and_ends_with_the_result()
    {
        var (agent, client) = Build();

        var response = await agent.RunAsync(Request);
        var text = response.Text!;

        // In order: the specification, then the result. The index comparison is what proves the order.
        var spec = text.IndexOf("\"spec\"", StringComparison.Ordinal);
        var suggested = text.IndexOf("\"suggested\"", StringComparison.Ordinal);

        spec.Should().BeGreaterThanOrEqualTo(0, "the rephraser ran");
        suggested.Should().BeGreaterThan(spec, "and the suggestor ran after it");

        var result = ReadLast<SuggestionResult>(text);

        result.Status.Should().Be(SuggestionStatus.Suggested);
        result.Options.Should().HaveCount(1);
        result.Options[0].Lines[0].Sku.Should().Be("DSKB08XN4JDR");

        // Two calls, in order: the specification first, then the composition.
        client.Requests.Should().HaveCount(2);
        client.Requests[0].Should().Contain("a quiet corner for one screen");
    }

    [Fact] // AIWB-10
    public async Task The_suggestor_still_sees_the_specification_the_rephraser_produced()
    {
        var (agent, client) = Build();

        _ = await agent.RunAsync(Request);

        // The second call is the suggestor's. Default chaining is what keeps the rephraser's specification in
        // front of it; chainOnlyAgentResponses would have handed it nothing. The catalogue is no longer part of
        // this — the suggestor searches it through its tools.
        client.Requests[1].Should().Contain("a wide, stable surface", "the rephraser's specification is chained through");
    }

    [Fact] // AIWB-11
    public void A_slot_the_request_says_nothing_about_may_be_left_empty()
    {
        // One line for a two-slot request is a valid result: nothing requires every slot to be filled, which
        // is what lets the agent leave coffee out of a request that never mentioned it.
        Schemas.EvaluateRaw("suggestion.result.schema.json", Result).IsValid.Should().BeTrue();

        var spec = JsonSerializer.Deserialize<WorkspaceSpec>(Spec)!;
        spec.Slots.Should().HaveCount(1, "the rephraser left the Monitor slot out because the sentence did not ask for one");
    }

    [Fact] // AIWB-12
    public void Both_executor_identities_are_stable()
    {
        // Pinned as literals, because these names are what a checkpoint records: renaming one is a
        // resume-compatibility decision rather than a rename.
        AgentRoster.Rephraser.Name.Should().Be("rephraser");
        AgentRoster.Suggestor.Name.Should().Be("suggestor");

        AgentRoster.All.Select(profile => profile.Name).Should().Equal("rephraser", "suggestor");
    }

    /// <summary>The last top-level JSON object of a response that carries several, in order.</summary>
    private static T ReadLast<T>(string text)
    {
        var reader = new Utf8JsonReader(
            Encoding.UTF8.GetBytes(text),
            new JsonReaderOptions { AllowMultipleValues = true });

        var last = default(JsonElement);

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.StartObject)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                last = document.RootElement.Clone();
            }
        }

        return last.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private static (AIAgent Agent, ScriptedChatClient Client) Build()
    {
        // One client for both agents, so the script order is the call order. No catalogue tools: these tests
        // are about the pipeline, not retrieval.
        var client = new ScriptedChatClient(Spec, Result);

        return (new WorkspaceSuggestionWorkflow(client).AsAIAgent([]), client);
    }
}
