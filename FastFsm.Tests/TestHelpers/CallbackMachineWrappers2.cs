using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    // ====== ComplexCallback ======
    public class ComplexCallbackFluentWrapper : IStateMachineTestWrapper
    {
        private readonly ComplexCallbackMachineFluent _machine;
        
        public ComplexCallbackFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.ComplexCallbackState>(
                "ComplexCallback", initialStateName);
            var state = (StateCallbackTests.ComplexCallbackState)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackState), resolvedName);
            _machine = new ComplexCallbackMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ComplexCallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ComplexCallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.ComplexCallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackTrigger), trigger.ToString()!);
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
    
    public class ComplexCallbackLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly ComplexCallbackMachine _machine;
        
        public ComplexCallbackLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.ComplexCallbackState>(
                "ComplexCallback", initialStateName);
            var state = (StateCallbackTests.ComplexCallbackState)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackState), resolvedName);
            _machine = new ComplexCallbackMachine(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ComplexCallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ComplexCallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.ComplexCallbackTrigger)Enum.Parse(
                typeof(StateCallbackTests.ComplexCallbackTrigger), trigger.ToString()!);
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
    
    // ====== GuardedCallback ======
    public class GuardedCallbackFluentWrapper : IStateMachineTestWrapper
    {
        private readonly GuardedCallbackMachineFluent _machine;
        
        public GuardedCallbackFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.GuardedState>(
                "GuardedCallback", initialStateName);
            var state = (StateCallbackTests.GuardedState)Enum.Parse(
                typeof(StateCallbackTests.GuardedState), resolvedName);
            _machine = new GuardedCallbackMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.GuardedTrigger)Enum.Parse(
                typeof(StateCallbackTests.GuardedTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.GuardedTrigger)Enum.Parse(
                typeof(StateCallbackTests.GuardedTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.GuardedTrigger)Enum.Parse(
                typeof(StateCallbackTests.GuardedTrigger), trigger.ToString()!);
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
    
    public class GuardedCallbackLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly GuardedCallbackMachine _machine;
        
        public GuardedCallbackLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.GuardedState>(
                "GuardedCallback", initialStateName);
            var state = (StateCallbackTests.GuardedState)Enum.Parse(
                typeof(StateCallbackTests.GuardedState), resolvedName);
            _machine = new GuardedCallbackMachine(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.GuardedTrigger)Enum.Parse(
                typeof(StateCallbackTests.GuardedTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.GuardedTrigger)Enum.Parse(
                typeof(StateCallbackTests.GuardedTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.GuardedTrigger)Enum.Parse(
                typeof(StateCallbackTests.GuardedTrigger), trigger.ToString()!);
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
    
    // ====== SelfTransition ======
    public class SelfTransitionFluentWrapper : IStateMachineTestWrapper
    {
        private readonly SelfTransitionMachineFluent _machine;
        
        public SelfTransitionFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.SelfState>(
                "SelfTransition", initialStateName);
            var state = (StateCallbackTests.SelfState)Enum.Parse(
                typeof(StateCallbackTests.SelfState), resolvedName);
            _machine = new SelfTransitionMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.SelfTrigger)Enum.Parse(
                typeof(StateCallbackTests.SelfTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.SelfTrigger)Enum.Parse(
                typeof(StateCallbackTests.SelfTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.SelfTrigger)Enum.Parse(
                typeof(StateCallbackTests.SelfTrigger), trigger.ToString()!);
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
    
    public class SelfTransitionLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly SelfTransitionMachineLegacy _machine;
        
        public SelfTransitionLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<StateCallbackTests.SelfState>(
                "SelfTransition", initialStateName);
            var state = (StateCallbackTests.SelfState)Enum.Parse(
                typeof(StateCallbackTests.SelfState), resolvedName);
            _machine = new SelfTransitionMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.SelfTrigger)Enum.Parse(
                typeof(StateCallbackTests.SelfTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.SelfTrigger)Enum.Parse(
                typeof(StateCallbackTests.SelfTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.SelfTrigger)Enum.Parse(
                typeof(StateCallbackTests.SelfTrigger), trigger.ToString()!);
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