using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.EdgeCases;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    // ====== InternalOnly ======
    public class InternalOnlyFluentWrapper : IStateMachineTestWrapper
    {
        private readonly InternalOnlyMachineFluent _machine;
        
        public InternalOnlyFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<EmptyMachineTests.InternalOnlyState>(
                "InternalOnly", initialStateName);
            var state = (EmptyMachineTests.InternalOnlyState)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyState), resolvedName);
            _machine = new InternalOnlyMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.InternalOnlyTrigger)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.InternalOnlyTrigger)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (EmptyMachineTests.InternalOnlyTrigger)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyTrigger), trigger.ToString()!);
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
    
    public class InternalOnlyLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly InternalOnlyMachineLegacy _machine;
        
        public InternalOnlyLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<EmptyMachineTests.InternalOnlyState>(
                "InternalOnly", initialStateName);
            var state = (EmptyMachineTests.InternalOnlyState)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyState), resolvedName);
            _machine = new InternalOnlyMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.InternalOnlyTrigger)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.InternalOnlyTrigger)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (EmptyMachineTests.InternalOnlyTrigger)Enum.Parse(
                typeof(EmptyMachineTests.InternalOnlyTrigger), trigger.ToString()!);
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
    
    // ====== Unreachable ======
    public class UnreachableFluentWrapper : IStateMachineTestWrapper
    {
        private readonly UnreachableMachineFluent _machine;
        
        public UnreachableFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<EmptyMachineTests.UnreachableState>(
                "Unreachable", initialStateName);
            var state = (EmptyMachineTests.UnreachableState)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableState), resolvedName);
            _machine = new UnreachableMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.UnreachableTrigger)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.UnreachableTrigger)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (EmptyMachineTests.UnreachableTrigger)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableTrigger), trigger.ToString()!);
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
    
    public class UnreachableLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly UnreachableMachineLegacy _machine;
        
        public UnreachableLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<EmptyMachineTests.UnreachableState>(
                "Unreachable", initialStateName);
            var state = (EmptyMachineTests.UnreachableState)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableState), resolvedName);
            _machine = new UnreachableMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.UnreachableTrigger)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.UnreachableTrigger)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (EmptyMachineTests.UnreachableTrigger)Enum.Parse(
                typeof(EmptyMachineTests.UnreachableTrigger), trigger.ToString()!);
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
    
    // ====== SingleState ======
    public class SingleStateFluentWrapper : IStateMachineTestWrapper
    {
        private readonly SingleStateMachineFluent _machine;
        
        public SingleStateFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<EmptyMachineTests.SingleState>(
                "SingleState", initialStateName);
            var state = (EmptyMachineTests.SingleState)Enum.Parse(
                typeof(EmptyMachineTests.SingleState), resolvedName);
            _machine = new SingleStateMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.SingleTrigger)Enum.Parse(
                typeof(EmptyMachineTests.SingleTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.SingleTrigger)Enum.Parse(
                typeof(EmptyMachineTests.SingleTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (EmptyMachineTests.SingleTrigger)Enum.Parse(
                typeof(EmptyMachineTests.SingleTrigger), trigger.ToString()!);
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
    
    public class SingleStateLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly SingleStateMachineLegacy _machine;
        
        public SingleStateLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<EmptyMachineTests.SingleState>(
                "SingleState", initialStateName);
            var state = (EmptyMachineTests.SingleState)Enum.Parse(
                typeof(EmptyMachineTests.SingleState), resolvedName);
            _machine = new SingleStateMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.None;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.SingleTrigger)Enum.Parse(
                typeof(EmptyMachineTests.SingleTrigger), trigger.ToString()!);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (EmptyMachineTests.SingleTrigger)Enum.Parse(
                typeof(EmptyMachineTests.SingleTrigger), trigger.ToString()!);
            _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (EmptyMachineTests.SingleTrigger)Enum.Parse(
                typeof(EmptyMachineTests.SingleTrigger), trigger.ToString()!);
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
    
    // ====== FullOrder ======  
    public class FullOrderFluentWrapper : IStateMachineTestWrapper
    {
        private readonly FullOrderMachineFluent _machine;
        
        public FullOrderFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<Machines.OrderState>(
                "FullOrder", initialStateName);
            var state = (Machines.OrderState)Enum.Parse(
                typeof(Machines.OrderState), resolvedName);
            _machine = new FullOrderMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.HasDefaultPayload;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.OrderTrigger)Enum.Parse(
                typeof(Machines.OrderTrigger), trigger.ToString()!);
            if (payload != null && payload is Features.Integration.AllFeaturesExtendedTests.OrderPayload orderPayload)
                return _machine.TryFire(typedTrigger, orderPayload);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.OrderTrigger)Enum.Parse(
                typeof(Machines.OrderTrigger), trigger.ToString()!);
            if (payload != null && payload is Features.Integration.AllFeaturesExtendedTests.OrderPayload orderPayload)
                _machine.Fire(typedTrigger, orderPayload);
            else
                _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Machines.OrderTrigger)Enum.Parse(
                typeof(Machines.OrderTrigger), trigger.ToString()!);
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
    
    public class FullOrderLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly FullOrderMachine _machine;
        
        public FullOrderLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<Machines.OrderState>(
                "FullOrder", initialStateName);
            var state = (Machines.OrderState)Enum.Parse(
                typeof(Machines.OrderState), resolvedName);
            _machine = new FullOrderMachine(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.HasDefaultPayload;
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.OrderTrigger)Enum.Parse(
                typeof(Machines.OrderTrigger), trigger.ToString()!);
            if (payload != null && payload is Features.Integration.AllFeaturesExtendedTests.OrderPayload orderPayload)
                return _machine.TryFire(typedTrigger, orderPayload);
            return _machine.TryFire(typedTrigger);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.OrderTrigger)Enum.Parse(
                typeof(Machines.OrderTrigger), trigger.ToString()!);
            if (payload != null && payload is Features.Integration.AllFeaturesExtendedTests.OrderPayload orderPayload)
                _machine.Fire(typedTrigger, orderPayload);
            else
                _machine.Fire(typedTrigger);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Machines.OrderTrigger)Enum.Parse(
                typeof(Machines.OrderTrigger), trigger.ToString()!);
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