using AwesomeAssertions;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Usage;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>Counting the calls without changing them.</summary>
public sealed class UsageRecordingChatClientTests
{
    [Fact]
    public async Task An_unstreamed_call_is_counted_and_what_it_reported_is_recorded()
    {
        var inner = new UsageReportingChatClient(new UsageDetails { InputTokenCount = 40, OutputTokenCount = 4 });

        using var scope = RunUsageScope.Begin();

        await new UsageRecordingChatClient(inner).GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        scope.Recorder.ModelCalls.Should().Be(1);
        scope.Recorder.InputTokens.Should().Be(40);
        scope.Recorder.OutputTokens.Should().Be(4);
    }

    [Fact]
    public async Task A_streamed_call_reports_its_usage_inside_the_update_because_an_update_has_no_usage_of_its_own()
    {
        // Found by reading the API surface rather than assuming: ChatResponse has Usage, ChatResponseUpdate
        // does not, and the figure arrives as a UsageContent in the update's contents.
        var inner = new UsageReportingChatClient(new UsageDetails { InputTokenCount = 86_000, OutputTokenCount = 400 });

        using var scope = RunUsageScope.Begin();

        await foreach (var _ in new UsageRecordingChatClient(inner)
            .GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
        }

        scope.Recorder.ModelCalls.Should().Be(1);
        scope.Recorder.InputTokens.Should().Be(86_000);
        scope.Recorder.OutputTokens.Should().Be(400);
    }

    [Fact]
    public async Task The_call_and_the_stream_are_passed_through_untouched()
    {
        var inner = new UsageReportingChatClient(new UsageDetails { InputTokenCount = 1 });
        var client = new UsageRecordingChatClient(inner);

        using var scope = RunUsageScope.Begin();

        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            updates.Add(update);
        }

        updates.Should().ContainSingle();
        updates[0].Text.Should().Be("{\"status\":\"spec\"}");
        inner.Calls.Should().Be(1);
    }

    [Fact]
    public async Task With_no_run_in_flight_nothing_is_counted_and_nothing_throws()
    {
        // The client is a singleton, so it is wrapped once and used by requests that may not be counting.
        var client = new UsageRecordingChatClient(new UsageReportingChatClient(null));

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        response.Should().NotBeNull();

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
        }
    }

    [Fact]
    public async Task A_client_that_reports_nothing_still_has_its_call_counted()
    {
        var inner = new UsageReportingChatClient(null);

        using var scope = RunUsageScope.Begin();

        await new UsageRecordingChatClient(inner).GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        scope.Recorder.ModelCalls.Should().Be(1);
        scope.Recorder.InputTokens.Should().Be(0);
    }
}
