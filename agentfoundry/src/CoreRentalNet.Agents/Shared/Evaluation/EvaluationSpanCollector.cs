using System.Diagnostics;
using System.Globalization;
using CoreRentalNet.Agents.Shared.Serialization;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>Records the activity tree the agent already emits, without touching feature code.</summary>
/// <remarks>
/// This listens to the existing workflow-node and stage-model sources. The node spans already carry their
/// duration; the collector only persists what was already being measured. It is disabled with the rest of the
/// evaluation trace when no directory is configured.
/// </remarks>
internal sealed class EvaluationSpanCollector : IDisposable
{
    private readonly string _directory;
    private readonly ActivityListener _listener;
    private readonly Lock _gate = new();
    private readonly HashSet<string> _opened = new(StringComparer.Ordinal);

    public EvaluationSpanCollector(string directory)
    {
        _directory = directory;
        _listener = new ActivityListener
        {
            ShouldListenTo = source =>
                string.Equals(source.Name, WorkspaceTelemetry.Name, StringComparison.Ordinal)
                || string.Equals(source.Name, WorkspaceTelemetry.StageModelCallSourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = Record,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    private void Record(Activity activity)
    {
        if (activity.GetTagItem(WorkspaceTelemetry.RunId) is not string runId
            || string.IsNullOrWhiteSpace(runId))
        {
            return;
        }

        var record = new EvaluationSpanRecord(
            RunId: runId,
            TraceId: activity.TraceId.ToHexString(),
            SpanId: activity.SpanId.ToHexString(),
            ParentSpanId: activity.ParentSpanId == default ? null : activity.ParentSpanId.ToHexString(),
            Name: activity.DisplayName,
            Kind: activity.Kind.ToString(),
            StartedAt: activity.StartTimeUtc,
            DurationMs: activity.Duration.TotalMilliseconds,
            Tags: activity.TagObjects.ToDictionary(
                tag => tag.Key,
                tag => Convert.ToString(tag.Value, CultureInfo.InvariantCulture),
                StringComparer.Ordinal));

        lock (_gate)
        {
            var spansDirectory = Path.Combine(_directory, "spans");
            Directory.CreateDirectory(spansDirectory);
            var path = Path.Combine(spansDirectory, $"{runId}.jsonl");

            if (_opened.Add(runId))
            {
                File.WriteAllText(path, string.Empty);
            }

            File.AppendAllText(path, ContractJson.Serialize(record) + Environment.NewLine);
        }
    }
}
