using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Hsm.Runtime;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Legacy wrapper implementations for HSM machines - placeholders for now
    /// These are simplified implementations to allow tests to compile and run
    /// </summary>
    
    public class DeepHistoryTestMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly DeepHistoryTestsLegacy.DeepHistoryMachineLegacy _machine;
        
        public DeepHistoryTestMachineLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<DeepHistoryTestsLegacy.S>(
                "DeepHistory", initialStateName);
            var state = (DeepHistoryTestsLegacy.S)Enum.Parse(typeof(DeepHistoryTestsLegacy.S), resolvedName);
            _machine = new DeepHistoryTestsLegacy.DeepHistoryMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (DeepHistoryTestsLegacy.T)Enum.Parse(typeof(DeepHistoryTestsLegacy.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (DeepHistoryTestsLegacy.T)Enum.Parse(typeof(DeepHistoryTestsLegacy.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        public bool CanFire(object trigger)
        {
            var typedTrigger = (DeepHistoryTestsLegacy.T)Enum.Parse(typeof(DeepHistoryTestsLegacy.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();
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
    
    public class ShallowHistoryTestMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly ShallowHistoryTestsLegacy.ShallowHistoryMachineLegacy _machine;
        
        public ShallowHistoryTestMachineLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<ShallowHistoryTestsLegacy.S>(
                "ShallowHistory", initialStateName);
            var state = (ShallowHistoryTestsLegacy.S)Enum.Parse(typeof(ShallowHistoryTestsLegacy.S), resolvedName);
            _machine = new ShallowHistoryTestsLegacy.ShallowHistoryMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (ShallowHistoryTestsLegacy.T)Enum.Parse(typeof(ShallowHistoryTestsLegacy.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (ShallowHistoryTestsLegacy.T)Enum.Parse(typeof(ShallowHistoryTestsLegacy.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        public bool CanFire(object trigger)
        {
            var typedTrigger = (ShallowHistoryTestsLegacy.T)Enum.Parse(typeof(ShallowHistoryTestsLegacy.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();
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
    
    public class InitialChildTestMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly InitialChildTestsLegacy.InitialChildMachineLegacy _machine;
        
        public InitialChildTestMachineLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<InitialChildTestsLegacy.S>(
                "InitialChild", initialStateName);
            var state = (InitialChildTestsLegacy.S)Enum.Parse(typeof(InitialChildTestsLegacy.S), resolvedName);
            _machine = new InitialChildTestsLegacy.InitialChildMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (InitialChildTestsLegacy.T)Enum.Parse(typeof(InitialChildTestsLegacy.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (InitialChildTestsLegacy.T)Enum.Parse(typeof(InitialChildTestsLegacy.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        public bool CanFire(object trigger)
        {
            var typedTrigger = (InitialChildTestsLegacy.T)Enum.Parse(typeof(InitialChildTestsLegacy.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        public IReadOnlyList<object> GetPermittedTriggers() => _machine.GetPermittedTriggers().Cast<object>().ToList();
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