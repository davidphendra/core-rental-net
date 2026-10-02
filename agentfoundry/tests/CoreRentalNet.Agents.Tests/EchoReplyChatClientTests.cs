using AwesomeAssertions;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Features.EchoReply.ChatClient;
using CoreRentalNet.Agents.Shared.ChatClients;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The echo stage's client, which is the stage: it answers with the caller's own message and costs nothing.
/// </summary>
/// <remarks>
/// The assertions are deliberately exact. A deterministic client is not bound by a prompt the way a model is, so
/// "repeats the message verbatim, adds nothing" is only true because a test says so — and this is the test that
/// fails if anyone later adds a prefix, wraps the reply in JSON, or trims it.
/// </remarks>
public sealed class EchoReplyChatClientTests
{
    private static readonly ChatMessage[] Messages =
        [new(ChatRole.System, "instructions the client must not echo"), new(ChatRole.User, "hello there")];

    private static EchoReplyChatClient Client() => new(NoOpChatClient.Instance);

    [Fact]
    public async Task The_reply_is_the_callers_message_exactly()
    {
        var response = await Client().GetResponseAsync(Messages);

        response.Text.Should().Be("hello there", "a pure echo adds nothing, not even a prefix");
    }

    [Fact]
    public async Task The_reply_streams_as_the_same_text()
    {
        var fragments = new List<string>();

        await foreach (var update in Client().GetStreamingResponseAsync(Messages))
        {
            fragments.Add(update.Text ?? string.Empty);
        }

        string.Concat(fragments).Should().Be("hello there");
    }

    [Fact]
    public async Task The_system_prompt_is_not_echoed()
    {
        var response = await Client().GetResponseAsync(Messages);

        response.Text.Should().NotContain("instructions", "the echo is the caller's message, not the run's setup");
    }

    [Fact]
    public async Task A_run_with_no_user_message_answers_empty()
    {
        var response = await Client()
            .GetResponseAsync([new(ChatRole.System, "instructions")]);

        response.Text.Should().BeEmpty();
    }

    [Fact]
    public async Task The_reply_is_not_a_document()
    {
        // The profile declares ChatResponseFormat.Text on purpose: the reply is the query, not an envelope.
        var response = await Client().GetResponseAsync(Messages);

        response.Text.Should().NotStartWith("{").And.NotStartWith("[");
    }
}
