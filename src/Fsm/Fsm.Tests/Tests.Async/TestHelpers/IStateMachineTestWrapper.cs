using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Async.TestHelpers
{
    public interface IStateMachineTestWrapper
    {
        object CurrentState { get; }
        ApiCapabilities Caps { get; }

        // Sync surface (bridged to async for async-only machines)
        void Start();
        bool TryFire(object trigger, object? payload = null);
        void Fire(object trigger, object? payload = null);
        bool CanFire(object trigger);
        IReadOnlyList<object> GetPermittedTriggers();

        // Async surface
        ValueTask StartAsync(CancellationToken ct = default);
        ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default);
        ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default);
    }
}

