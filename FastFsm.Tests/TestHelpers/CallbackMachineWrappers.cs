using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    // ====== MultipleCallbacks ======
    public class MultipleCallbacksFluentWrapper : IStateMachineTestWrapper
    {
        private readonly MultipleCallbacksMachineFluent _machine;
        
        public MultipleCallbacksFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.MultiState>(
                "MultipleCallbacks", initialStateName);
            var state = (StateCallbackTests.MultiState)Enum.Parse(
                typeof(StateCallbackTests.MultiState), resolvedName);
            _machine = new MultipleCallbacksMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.MultiTrigger)Enum.Parse(
                typeof(StateCallbackTests.MultiTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.MultiTrigger)Enum.Parse(
                typeof(StateCallbackTests.MultiTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.MultiTrigger)Enum.Parse(
                typeof(StateCallbackTests.MultiTrigger), trigger.ToString()!);
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
    
    public class MultipleCallbacksLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly MultipleCallbacksMachineLegacy _machine;
        
        public MultipleCallbacksLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.MultiState>(
                "MultipleCallbacks", initialStateName);
            var state = (StateCallbackTests.MultiState)Enum.Parse(
                typeof(StateCallbackTests.MultiState), resolvedName);
            _machine = new MultipleCallbacksMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.MultiTrigger)Enum.Parse(
                typeof(StateCallbackTests.MultiTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.MultiTrigger)Enum.Parse(
                typeof(StateCallbackTests.MultiTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.MultiTrigger)Enum.Parse(
                typeof(StateCallbackTests.MultiTrigger), trigger.ToString()!);
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
    
    // ====== InitialState ======
    public class InitialStateFluentWrapper : IStateMachineTestWrapper
    {
        private readonly InitialStateMachineFluent _machine;
        
        public InitialStateFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.InitialState>(
                "InitialState", initialStateName);
            var state = (StateCallbackTests.InitialState)Enum.Parse(
                typeof(StateCallbackTests.InitialState), resolvedName);
            _machine = new InitialStateMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InitialTrigger)Enum.Parse(
                typeof(StateCallbackTests.InitialTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InitialTrigger)Enum.Parse(
                typeof(StateCallbackTests.InitialTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.InitialTrigger)Enum.Parse(
                typeof(StateCallbackTests.InitialTrigger), trigger.ToString()!);
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
    
    public class InitialStateLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly InitialStateMachine _machine;
        
        public InitialStateLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.InitialState>(
                "InitialState", initialStateName);
            var state = (StateCallbackTests.InitialState)Enum.Parse(
                typeof(StateCallbackTests.InitialState), resolvedName);
            _machine = new InitialStateMachine(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InitialTrigger)Enum.Parse(
                typeof(StateCallbackTests.InitialTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InitialTrigger)Enum.Parse(
                typeof(StateCallbackTests.InitialTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.InitialTrigger)Enum.Parse(
                typeof(StateCallbackTests.InitialTrigger), trigger.ToString()!);
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
    
    // ====== CallbackOrder ======
    public class CallbackOrderFluentWrapper : IStateMachineTestWrapper
    {
        private readonly CallbackOrderMachineFluent _machine;
        
        public CallbackOrderFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.CallbackState>(
                "CallbackOrder", initialStateName);
            var state = (StateCallbackTests.CallbackState)Enum.Parse(
                typeof(StateCallbackTests.CallbackState), resolvedName);
            _machine = new CallbackOrderMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.CallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.CallbackTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.CallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.CallbackTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.CallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.CallbackTrigger), trigger.ToString()!);
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
    
    public class CallbackOrderLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly CallbackOrderMachineLegacy _machine;
        
        public CallbackOrderLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.CallbackState>(
                "CallbackOrder", initialStateName);
            var state = (StateCallbackTests.CallbackState)Enum.Parse(
                typeof(StateCallbackTests.CallbackState), resolvedName);
            _machine = new CallbackOrderMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.CallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.CallbackTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.CallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.CallbackTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.CallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.CallbackTrigger), trigger.ToString()!);
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