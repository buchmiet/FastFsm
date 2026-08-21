using System.Diagnostics;
using System.Threading;

namespace Benchmark;

internal static class ObservabilityBenchmarkActivityListener
{
    private static int _activeScopes;
    private static ActivityListener? _listener;

    public static IDisposable Activate()
    {
        EnsureRegistered();
        Interlocked.Increment(ref _activeScopes);
        return new Scope();
    }

    private static void EnsureRegistered()
    {
        if (_listener is not null)
        {
            return;
        }

        _listener = new ActivityListener
        {
            ShouldListenTo = source =>
                Volatile.Read(ref _activeScopes) > 0 &&
                source.Name == FastFsm.Observability.ObservabilityTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() => Interlocked.Decrement(ref _activeScopes);
    }
}
