using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>Writes one JSON object per line, one file per run.</summary>
/// <remarks>
/// The writer is scoped to the run that owns it, so no two runs can interleave records. It is disabled only by
/// never being constructed: a deployment that does not configure a trace path registers no sink and no decorator.
/// </remarks>
internal sealed class JsonlEvaluationTraceSink : IEvaluationTraceSink, IDisposable
{
    private readonly string _directory;
    private readonly Lock _gate = new();
    private StreamWriter? _writer;
    private string _runId = string.Empty;
    private long _sequence;

    public JsonlEvaluationTraceSink(EvaluationTraceOptions options)
        => _directory = options.Directory;

    public void BeginRun(string runId, string model, string promptVersions)
    {
        Directory.CreateDirectory(_directory);

        _runId = runId;
        _writer = new StreamWriter(
            Path.Combine(_directory, $"{SafeFileName(runId)}.jsonl"),
            append: false);

        Write(new EvaluationRunStarted(model, promptVersions));
    }

    public void RecordStageModelCall(EvaluationStageModelCall call) => Write(call);

    public void RecordToolExchange(EvaluationToolExchange exchange) => Write(exchange);

    public void CompleteRun(string runStatus) => Write(new EvaluationRunCompleted(runStatus));

    private void Write(EvaluationTraceRecord record)
    {
        if (_writer is null)
        {
            return;
        }

        lock (_gate)
        {
            _writer.WriteLine(ContractJson.Serialize(record with
            {
                RunId = _runId,
                Sequence = ++_sequence,
                At = DateTimeOffset.UtcNow,
            }));

            _writer.Flush();
        }
    }

    private static string SafeFileName(string value)
        => string.Concat(value.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    public void Dispose() => _writer?.Dispose();
}
