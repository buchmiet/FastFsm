using System.Diagnostics;
using FastFsm.Observability;

namespace Benchmark;

/// <summary>
/// Benchmark-only <see cref="ActivityListener"/>.
/// <see cref="ActivityListener.ShouldListenTo"/> is evaluated once, when the listener is attached to an
/// existing <see cref="ActivitySource"/> (and again only for sources created later). A later flag change
/// does not re-run that filter, so sampling must not be gated behind a post-registration scope count.
/// </summary>
internal static class ObservabilityBenchmarkActivityListener
{
    public static IDisposable Activate()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == ObservabilityTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };

        // Force the static source to exist before AddActivityListener, so the runtime attaches now
        // rather than depending on a later source constructor.
        _ = ObservabilityTelemetry.ActivitySource;
        ActivitySource.AddActivityListener(listener);

        if (!ObservabilityTelemetry.ActivitySource.HasListeners())
        {
            listener.Dispose();
            throw new InvalidOperationException(
                "ActivityListener did not attach to the FastFsm ActivitySource. Sampled tracing would be a no-op.");
        }

        return listener;
    }
}
