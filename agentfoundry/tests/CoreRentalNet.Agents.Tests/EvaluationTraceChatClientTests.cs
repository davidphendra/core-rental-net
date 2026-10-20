using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Evaluation;
using Microsoft.Extensions.AI;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class EvaluationTraceChatClientTests
{
    [Fact]
    public void Lifecycle_delegates_and_opens_the_trace()
    {
        var inner = new FakeTelemetryChatClient();
        var sink = new RecordingEvaluationTraceSink();
        var client = new EvaluationTraceChatClient(inner, sink);

        client.StartRun("run-1");
        client.CompleteRun("success");

        inner.Lifecycle.Should().Equal("start:run-1", "complete:success");
        sink.Lifecycle.Should().Equal("begin:run-1:gpt-test:prompt-test", "complete:success");
    }

    [Fact]
    public async Task A_stage_call_records_the_stage_name_and_answer()
    {
        var inner = new FakeTelemetryChatClient();
        var sink = new RecordingEvaluationTraceSink();
        var client = new EvaluationTraceChatClient(inner, sink);

        var options = new ChatOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ITelemetryChatClient.AgentName] = "workspace-request-verifier",
            },
        };

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], options);

        sink.StageCalls.Should().ContainSingle();
        sink.StageCalls[0].Stage.Should().Be("workspace-request-verifier");
        sink.StageCalls[0].ResponseText.Should().Be("answer");
    }

    [Fact]
    public async Task A_tool_result_in_a_follow_up_call_is_recorded_once()
    {
        var inner = new FakeTelemetryChatClient();
        var sink = new RecordingEvaluationTraceSink();
        var client = new EvaluationTraceChatClient(inner, sink);

        var messages = new List<ChatMessage>
        {
            new(
                ChatRole.Assistant,
                [new FunctionCallContent("call-1", "search_catalogue", new Dictionary<string, object?>
                {
                    ["category"] = "desk",
                    ["terms"] = "desk",
                })]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent("call-1", "{\"matches\":{\"value\":[{\"sku\":\"D1\"}]}}")]),
        };

        await client.GetResponseAsync(messages);
        await client.GetResponseAsync(messages);

        sink.ToolExchanges.Should().ContainSingle();
        sink.ToolExchanges[0].ToolName.Should().Be("search_catalogue");
        sink.ToolExchanges[0].ResultText.Should().Contain("D1");
    }
}
