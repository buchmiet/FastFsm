using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Hsm.Runtime;
using FastFsm.Contracts;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for SimpleParentChildMachineFluent
    /// </summary>
    public class SimpleParentChildMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly SimpleParentChildMachineFluent _machine;
        
        public SimpleParentChildMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                HsmStateFluent.Idle : 
                (HsmStateFluent)Enum.Parse(typeof(HsmStateFluent), initialStateName);
            _machine = new SimpleParentChildMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public List<string> EntryExitLog => _machine.EntryExitLog;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (HsmTriggerFluent)Enum.Parse(typeof(HsmTriggerFluent), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (HsmTriggerFluent)Enum.Parse(typeof(HsmTriggerFluent), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (HsmTriggerFluent)Enum.Parse(typeof(HsmTriggerFluent), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
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
        
        // HSM-specific methods
        public bool IsInHierarchy(object state)
        {
            var typedState = (HsmStateFluent)Enum.Parse(typeof(HsmStateFluent), state.ToString()!);
            return _machine.IsIn(typedState);
        }
        
        public IReadOnlyList<object> GetActivePath()
        {
            return _machine.GetActivePath().Cast<object>().ToList();
        }
    }
    
    /// <summary>
    /// Wrapper for SimpleParentChildMachineLegacy (to be created)
    /// </summary>
    public class SimpleParentChildMachineLegacyWrapper : IStateMachineTestWrapper
    {
        // TODO: Implement when Legacy machine is created
        public object CurrentState => throw new NotImplementedException();
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public SimpleParentChildMachineLegacyWrapper(string initialStateName)
        {
            // TODO: Implement
        }
        
        public void Start() => throw new NotImplementedException();
        public bool TryFire(object trigger, object? payload = null) => throw new NotImplementedException();
        public void Fire(object trigger, object? payload = null) => throw new NotImplementedException();
        public bool CanFire(object trigger) => throw new NotImplementedException();
        public IReadOnlyList<object> GetPermittedTriggers() => throw new NotImplementedException();
        public ValueTask StartAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
    }
    
    /// <summary>
    /// Wrapper for DeepHistoryTestMachineFluent
    /// </summary>
    public class DeepHistoryTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        // TODO: Implement after checking the machine structure
        public object CurrentState => throw new NotImplementedException();
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public DeepHistoryTestMachineFluentWrapper(string initialStateName)
        {
            // TODO: Implement
        }
        
        public void Start() => throw new NotImplementedException();
        public bool TryFire(object trigger, object? payload = null) => throw new NotImplementedException();
        public void Fire(object trigger, object? payload = null) => throw new NotImplementedException();
        public bool CanFire(object trigger) => throw new NotImplementedException();
        public IReadOnlyList<object> GetPermittedTriggers() => throw new NotImplementedException();
        public ValueTask StartAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
    }
    
    /// <summary>
    /// Wrapper for ShallowHistoryTestMachineFluent
    /// </summary>
    public class ShallowHistoryTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        // TODO: Implement after checking the machine structure
        public object CurrentState => throw new NotImplementedException();
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public ShallowHistoryTestMachineFluentWrapper(string initialStateName)
        {
            // TODO: Implement
        }
        
        public void Start() => throw new NotImplementedException();
        public bool TryFire(object trigger, object? payload = null) => throw new NotImplementedException();
        public void Fire(object trigger, object? payload = null) => throw new NotImplementedException();
        public bool CanFire(object trigger) => throw new NotImplementedException();
        public IReadOnlyList<object> GetPermittedTriggers() => throw new NotImplementedException();
        public ValueTask StartAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
    }
    
    /// <summary>
    /// Wrapper for InitialChildTestMachineFluent
    /// </summary>
    public class InitialChildTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        // TODO: Implement after checking the machine structure
        public object CurrentState => throw new NotImplementedException();
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public InitialChildTestMachineFluentWrapper(string initialStateName)
        {
            // TODO: Implement
        }
        
        public void Start() => throw new NotImplementedException();
        public bool TryFire(object trigger, object? payload = null) => throw new NotImplementedException();
        public void Fire(object trigger, object? payload = null) => throw new NotImplementedException();
        public bool CanFire(object trigger) => throw new NotImplementedException();
        public IReadOnlyList<object> GetPermittedTriggers() => throw new NotImplementedException();
        public ValueTask StartAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
    }
    
    /// <summary>
    /// Wrapper for HsmIsInHierarchyTestMachineFluent
    /// </summary>
    public class HsmIsInHierarchyTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        // TODO: Implement after checking the machine structure
        public object CurrentState => throw new NotImplementedException();
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public HsmIsInHierarchyTestMachineFluentWrapper(string initialStateName)
        {
            // TODO: Implement
        }
        
        public void Start() => throw new NotImplementedException();
        public bool TryFire(object trigger, object? payload = null) => throw new NotImplementedException();
        public void Fire(object trigger, object? payload = null) => throw new NotImplementedException();
        public bool CanFire(object trigger) => throw new NotImplementedException();
        public IReadOnlyList<object> GetPermittedTriggers() => throw new NotImplementedException();
        public ValueTask StartAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
    }
}