using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastFsm.Logging.Tests.TestHelpers
{
    public interface IStateMachineTestWrapper
    {
        object CurrentState { get; }
        ApiCapabilities Caps { get; }

        void Start();
        bool TryFire(object trigger, object? payload = null);
        void Fire(object trigger, object? payload = null);
        bool CanFire(object trigger);
        IReadOnlyList<object> GetPermittedTriggers();

        ValueTask StartAsync(CancellationToken ct = default);
        ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default);
        ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default);
    }
}

