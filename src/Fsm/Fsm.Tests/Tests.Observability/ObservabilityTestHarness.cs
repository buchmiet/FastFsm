using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FastFsm.Observability;

namespace Tests.Observability;

public sealed class ObservabilityTestHarness : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly ConcurrentDictionary<string, long> _counterBaselines = new();
    private readonly ConcurrentDictionary<string, long> _counterTotals = new();
    private readonly ConcurrentBag<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)> _histogramMeasurements = [];

    public RecordingEventSink EventSink { get; } = new();

    public List<Activity> CompletedActivities { get; } = [];

    public ObservabilityTestHarness()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => CompletedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = static (instrument, listener) =>
            {
                if (instrument.Meter.Name == ObservabilityTelemetry.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>(OnCounterMeasurement);
        _meterListener.SetMeasurementEventCallback<double>(OnHistogramMeasurement);
        _meterListener.Start();
    }

    public ObservabilityExtension<TState, TTrigger> CreateExtension<TState, TTrigger>(
        Action<FastFsmObservabilityOptions.Builder>? configure = null)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        var options = FastFsmObservabilityOptions.Create(configure);
        return new ObservabilityExtension<TState, TTrigger>(options, eventSink: EventSink);
    }

    public void CaptureMetricBaseline()
    {
        _counterBaselines.Clear();
        foreach (var pair in _counterTotals)
            _counterBaselines[pair.Key] = pair.Value;
    }

    public long GetCounterDelta(string instrumentName)
    {
        _counterTotals.TryGetValue(instrumentName, out var total);
        _counterBaselines.TryGetValue(instrumentName, out var baseline);
        return total - baseline;
    }

    public IReadOnlyList<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)> GetHistogramMeasurements()
        => [.. _histogramMeasurements];

    public void ClearHistogramMeasurements() => _histogramMeasurements.Clear();

    public void ClearCompletedActivities() => CompletedActivities.Clear();

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    private void OnCounterMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => _counterTotals.AddOrUpdate(instrument.Name, measurement, (_, current) => current + measurement);

    private void OnHistogramMeasurement(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        var tagDictionary = new Dictionary<string, object?>();
        foreach (var pair in tags)
            tagDictionary[pair.Key] = pair.Value;

        _histogramMeasurements.Add((instrument.Name, measurement, tagDictionary));
    }
}

public sealed class RecordingEventSink : IObservabilityEventSink
{
    public List<ObservabilityEvent> Events { get; } = [];

    public bool ThrowOnEvent { get; set; }

    public void OnEvent(in ObservabilityEvent evt)
    {
        if (ThrowOnEvent)
            throw new InvalidOperationException("Observability sink failure.");

        Events.Add(evt);
    }
}
