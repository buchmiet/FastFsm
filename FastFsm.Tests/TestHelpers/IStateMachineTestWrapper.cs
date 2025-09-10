using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Unified interface for testing both Fluent and Legacy API state machines
    /// </summary>
    public interface IStateMachineTestWrapper
    {
        // Properties
        object CurrentState { get; }
        ApiCapabilities Caps { get; }
        
        // Synchronous methods
        void Start();
        bool TryFire(object trigger, object? payload = null);
        void Fire(object trigger, object? payload = null);
        bool CanFire(object trigger);
        IReadOnlyList<object> GetPermittedTriggers();
        
        // Asynchronous methods
        ValueTask StartAsync(CancellationToken ct = default);
        ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default);
        ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default);
    }
}