using System.Diagnostics;
using System.Diagnostics.Metrics;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Tests;

/// <summary>Captures the spans and metric points a real provider would receive, with no package.</summary>
/// <remarks>
/// Hand-written because the repo has no mocking library and needs none here: these listeners are the same
/// mechanism a real exporter uses, so a test observes exactly what production would.
/// </remarks>
internal sealed class TelemetryRecorder : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;

    public TelemetryRecorder()
    {
        Spans = [];
        Measurements = [];

        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WorkspaceTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = Spans.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == WorkspaceTelemetry.Name)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        _meterListener.SetMeasurementEventCallback<long>(RecordAsLong);
        _meterListener.SetMeasurementEventCallback<int>(RecordAsInt);
        _meterListener.SetMeasurementEventCallback<double>(RecordAsDouble);
        _meterListener.Start();
    }

    public List<Activity> Spans { get; }

    public List<RecordedMeasurement> Measurements { get; }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    private void RecordAsLong(Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        => Measurements.Add(new RecordedMeasurement(instrument.Name, value, tags.ToArray()));

    private void RecordAsInt(Instrument instrument, int value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        => Measurements.Add(new RecordedMeasurement(instrument.Name, value, tags.ToArray()));

    private void RecordAsDouble(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        => Measurements.Add(new RecordedMeasurement(instrument.Name, value, tags.ToArray()));
}
