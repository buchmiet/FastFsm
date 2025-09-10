using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for CoreBenchmarkMachineFluent
    /// </summary>
    public class CoreBenchmarkFluentWrapper : IStateMachineTestWrapper
    {
        private readonly CoreBenchmarkMachineFluent _machine;
        
        public CoreBenchmarkFluentWrapper(string initialStateName)
        {
            var state = (BenchmarkTests.BenchmarkState)Enum.Parse(
                typeof(BenchmarkTests.BenchmarkState), initialStateName);
            _machine = new CoreBenchmarkMachineFluent(state);
        }
        
        public CoreBenchmarkFluentWrapper(BenchmarkTests.BenchmarkState initialState)
        {
            _machine = new CoreBenchmarkMachineFluent(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.None; // Simple sync-only machine
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Fluent, "CoreBenchmark");
            return payload == null 
                ? _machine.TryFire((BenchmarkTests.BenchmarkTrigger)typedTrigger) 
                : _machine.TryFire((BenchmarkTests.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Fluent, "CoreBenchmark");
            if (payload == null)
                _machine.Fire((BenchmarkTests.BenchmarkTrigger)typedTrigger);
            else
                _machine.Fire((BenchmarkTests.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Fluent, "CoreBenchmark");
            return _machine.CanFire((BenchmarkTests.BenchmarkTrigger)typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers()
        {
            return _machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            _machine.Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var result = TryFire(trigger, payload);
            return ValueTask.FromResult(result);
        }
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// Wrapper for CoreBenchmarkMachineLegacy
    /// </summary>
    public class CoreBenchmarkLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly CoreBenchmarkMachineLegacy _machine;
        
        public CoreBenchmarkLegacyWrapper(string initialStateName)
        {
            var state = (BenchmarkTestsLegacy.BenchmarkState)Enum.Parse(
                typeof(BenchmarkTestsLegacy.BenchmarkState), initialStateName);
            _machine = new CoreBenchmarkMachineLegacy(state);
        }
        
        public CoreBenchmarkLegacyWrapper(BenchmarkTestsLegacy.BenchmarkState initialState)
        {
            _machine = new CoreBenchmarkMachineLegacy(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.None; // Simple sync-only machine
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Legacy, "CoreBenchmark");
            return payload == null 
                ? _machine.TryFire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger) 
                : _machine.TryFire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Legacy, "CoreBenchmark");
            if (payload == null)
                _machine.Fire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger);
            else
                _machine.Fire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Legacy, "CoreBenchmark");
            return _machine.CanFire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers()
        {
            return _machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            _machine.Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var result = TryFire(trigger, payload);
            return ValueTask.FromResult(result);
        }
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
}