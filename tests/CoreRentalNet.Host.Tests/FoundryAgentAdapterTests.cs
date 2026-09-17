using System.Runtime.CompilerServices;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Carrying the agent's streamed JSON across the boundary into the port the application speaks.
/// </summary>
/// <remarks>
/// The stream is a hand-written fake, so what is asserted is the parsing and the boundary rather than a
/// client's behaviour: a stage, a result, a message split across chunks, and a message that cannot be
/// read at all.
/// </remarks>
public sealed class FoundryAgentAdapterTests
{
    // One JSON object per line, terminated: the delimiter is the contract, and a message without one
    // was not delivered. See the class's own remarks.
    private const string Stage = """
        {"kind":"stage","stage":"verifying","attempt":1}

        """;

    private const string Result = """
        {"kind":"result","status":"ok","attempts":1,"options":[{"tier":"low","lines":[{"slot":"Monitor","sku":"MON0001","quantity":2}],"criteria":["slot:Monitor"],"unevaluated":[{"phrase":"reliable","reason":"not_in_catalogue"}],"pinnedSlots":[]}]}

        """;

    [Fact] // AIB-06
    public async Task A_stage_arrives_before_the_result_and_carries_its_attempt()
    {
        var messages = await CollectAsync(Stage, Result);

        messages.Should().HaveCount(2);
        messages[0].Kind.Should().Be("stage");
        messages[0].Stage.Should().Be("verifying");
        messages[0].Attempt.Should().Be(1);
        messages[0].Result.Should().BeNull();

        messages[1].Kind.Should().Be("result");
        messages[1].Result.Should().NotBeNull();
    }

    [Fact] // AIB-06
    public async Task The_result_carries_the_candidates_the_agent_composed()
    {
        var messages = await CollectAsync(Result);

        var answer = messages.Single().Result!;
        answer.Status.Should().Be(SuggestionStatus.Ok);

        var option = answer.Options.Single();
        option.Tier.Should().Be("low");
        option.Criteria.Should().Equal("slot:Monitor");
        option.Unevaluated.Should().Equal("reliable");

        var line = option.Lines.Single();
        line.Slot.Should().Be("Monitor");
        line.Sku.Should().Be("MON0001");
        line.Quantity.Should().Be(2);
    }

    [Fact] // AIB-06
    public async Task A_message_split_across_two_updates_is_read_as_one()
    {
        // The platform streams chunks, not messages: a long result arrives in pieces, and treating each
        // piece as a message fails on exactly the runs that produced the most output.
        var half = Result.Length / 2;

        var messages = await CollectAsync(Result[..half], Result[half..]);

        messages.Should().ContainSingle().Which.Result.Should().NotBeNull();
    }

    [Fact] // AIB-06
    public async Task A_message_that_cannot_be_read_is_skipped_and_the_rest_still_arrives()
    {
        var messages = await CollectAsync("not json at all\n", "\n", Stage, Result);

        messages.Should().HaveCount(2);
        messages[0].Stage.Should().Be("verifying");
        messages[1].Result.Should().NotBeNull();
    }

    [Fact] // AIB-06
    public async Task A_message_the_stream_never_finished_is_not_read_as_though_it_had()
    {
        // The delimiter is the contract. Reading a truncated object as though it were whole is how a
        // partial result becomes a result - and the result is the one message that must not be guessed.
        var messages = await CollectAsync(Stage, """{"kind":"result","status":"ok","opt""");

        messages.Should().ContainSingle().Which.Kind.Should().Be("stage");
    }

    [Fact] // AIB-09
    public void An_agent_with_no_name_is_not_configured()
    {
        _ = new FoundryAgentAdapter(new FakeStream([]), agentName: null).IsConfigured.Should().BeFalse();
        _ = new FoundryAgentAdapter(new FakeStream([]), agentName: "  ").IsConfigured.Should().BeFalse();
        _ = new FoundryAgentAdapter(new FakeStream([]), agentName: "agentfoundry-workspace-suggestions")
            .IsConfigured.Should().BeTrue();
    }

    private static async Task<List<AgentSuggestionMessage>> CollectAsync(params string[] chunks)
    {
        var messages = new List<AgentSuggestionMessage>();

        await foreach (var message in new FoundryAgentAdapter(
            new FakeStream(chunks),
            "agentfoundry-workspace-suggestions").AskAsync("a desk with two monitors"))
        {
            messages.Add(message);
        }

        return messages;
    }

    /// <summary>The stream, said out loud instead of reached. No mocking library is used in this project.</summary>
    private sealed class FakeStream(IReadOnlyList<string> chunks) : IAgentTextStream
    {
        public async IAsyncEnumerable<string> AskAsync(
            string query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;

                await Task.Yield();
            }
        }
    }
}
