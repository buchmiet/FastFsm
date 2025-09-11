using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.EdgeCases;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    // ====== KeywordState ======
    public class KeywordStateFluentWrapper : IStateMachineTestWrapper
    {
        private readonly KeywordStateMachineFluent _machine;
        
        public KeywordStateFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.KeywordState>(
                "KeywordState", initialStateName);
            var state = (NameCollisionTests.KeywordState)Enum.Parse(
                typeof(NameCollisionTests.KeywordState), resolvedName);
            _machine = new KeywordStateMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.KeywordTrigger)Enum.Parse(
                typeof(NameCollisionTests.KeywordTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.KeywordTrigger)Enum.Parse(
                typeof(NameCollisionTests.KeywordTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.KeywordTrigger)Enum.Parse(
                typeof(NameCollisionTests.KeywordTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    public class KeywordStateLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly KeywordStateMachineLegacy _machine;
        
        public KeywordStateLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<NameCollisionTests.KeywordState>(
                "KeywordState", initialStateName);
            var state = (NameCollisionTests.KeywordState)Enum.Parse(
                typeof(NameCollisionTests.KeywordState), resolvedName);
            _machine = new KeywordStateMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.KeywordTrigger)Enum.Parse(
                typeof(NameCollisionTests.KeywordTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (NameCollisionTests.KeywordTrigger)Enum.Parse(
                typeof(NameCollisionTests.KeywordTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (NameCollisionTests.KeywordTrigger)Enum.Parse(
                typeof(NameCollisionTests.KeywordTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) =>
            ValueTask.FromResult(TryFire(trigger, payload));
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
}