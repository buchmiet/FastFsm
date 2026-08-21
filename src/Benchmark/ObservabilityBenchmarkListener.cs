using System.Diagnostics;
using System.Threading;

namespace Benchmark;

/// <summary>
/// Benchmark-only ActivityListener. <see cref="ActivityListener.ShouldListenTo"/> is evaluated when the
/// listener is registered; gating sampled work happens in <see cref="ActivityListener.Sample"/>.
/// </summary>
internal static class ObservabilityBenchmarkActivityListener
{
    private static int _activeScopes;
    private static ActivityListener? _listener;
    private static readonly object Gate = new();

    public static IDisposable Activate()
    {
        Interlocked.Increment(ref _activeScopes);
        EnsureRegistered();
        return new Scope();
    }

    public static void EnsureRegistered()
    {
        if (_listener is not null)
        {
            return;
        }

        lock (Gate)
        {
            if (_listener is not null)
            {
                return;
            }

            _listener = new ActivityListener
            {
                ShouldListenTo = source =>
                    source.Name == FastFsm.Observability.ObservabilityTelemetry.ActivitySourceName,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                    Volatile.Read(ref _activeScopes) > 0
                        ? ActivitySamplingResult.AllData
                        : ActivitySamplingResult.None
            };
            ActivitySource.AddActivityListener(_listener);
        }
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() => Interlocked.Decrement(ref _activeScopes);
    }
}
