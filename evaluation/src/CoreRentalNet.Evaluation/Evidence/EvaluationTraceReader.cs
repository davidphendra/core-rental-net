using System.Text.Json;
using CoreRentalNet.Evaluation.Contracts;
using CoreRentalNet.Evaluation.Execution;

namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>Reads a run's raw JSONL trace into stage calls and tool exchanges.</summary>
public sealed class EvaluationTraceReader(string traceDirectory) : ITraceReader
{
    public async Task<RunTrace> ReadAsync(string runId, CancellationToken cancellationToken)
    {
        var path = Path.Combine(traceDirectory, $"{runId}.jsonl");

        if (!File.Exists(path))
        {
            return new RunTrace(runId, [], []);
        }

        var stageCalls = new List<TraceStageCall>();
        var toolExchanges = new List<TraceToolExchange>();
        var spans = new List<TraceSpan>();

        foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var record = JsonSerializer.Deserialize<EvaluationTraceRecordDto>(
                line, EvaluationJson.Options);

            if (record is null)
            {
                continue;
            }

            switch (record.Kind)
            {
                case "stageModelCall" when record.Stage is { } stage:
                    stageCalls.Add(new TraceStageCall(
                        stage,
                        record.Attempt,
                        record.ResponseText ?? string.Empty));
                    break;

                case "toolExchange" when record.Stage is { } toolStage
                                        && record.ToolName is { } toolName:
                    toolExchanges.Add(new TraceToolExchange(
                        toolStage,
                        record.Attempt,
                        record.CallId ?? string.Empty,
                        toolName,
                        record.Arguments ?? new Dictionary<string, object?>(),
                        record.ResultText ?? string.Empty));
                    break;
            }
        }

        var spansPath = Path.Combine(traceDirectory, "spans", $"{runId}.jsonl");

        if (File.Exists(spansPath))
        {
            foreach (var line in await File.ReadAllLinesAsync(spansPath, cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var span = JsonSerializer.Deserialize<TraceSpan>(line, EvaluationJson.Options);

                if (span is not null)
                {
                    spans.Add(span);
                }
            }
        }

        return new RunTrace(runId, stageCalls, toolExchanges)
        {
            Spans = spans,
        };
    }
}
