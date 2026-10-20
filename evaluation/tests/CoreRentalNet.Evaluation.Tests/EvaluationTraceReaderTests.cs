using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evidence;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class EvaluationTraceReaderTests
{
    [Fact]
    public async Task Stage_calls_and_tool_exchanges_are_read_from_jsonl()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"evaluation-trace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "run-1.jsonl");

        await File.WriteAllLinesAsync(path,
        [
            """{"kind":"stageModelCall","runId":"run-1","sequence":1,"at":"2026-10-20T12:00:00Z","stage":"catalogue-candidate-reranker","attempt":1,"responseText":"{\"categories\":{}}"}""",
            """{"kind":"toolExchange","runId":"run-1","sequence":2,"at":"2026-10-20T12:00:01Z","stage":"catalogue-product-retriever","attempt":1,"callId":"call-1","toolName":"search_catalogue","arguments":{"category":"desk"},"resultText":"{}"}""",
        ]);

        var trace = await new EvaluationTraceReader(directory).ReadAsync("run-1", CancellationToken.None);

        trace.StageCalls.Should().ContainSingle();
        trace.StageCalls[0].Stage.Should().Be(WorkspaceStageNames.Reranker);
        trace.ToolExchanges.Should().ContainSingle();
        trace.ToolExchanges[0].ToolName.Should().Be(WorkspaceToolNames.NameSearch);
    }

    [Fact]
    public async Task A_missing_trace_is_an_empty_trace()
    {
        var trace = await new EvaluationTraceReader(Path.GetTempPath())
            .ReadAsync($"missing-{Guid.NewGuid():N}", CancellationToken.None);

        trace.StageCalls.Should().BeEmpty();
        trace.ToolExchanges.Should().BeEmpty();
    }
}
