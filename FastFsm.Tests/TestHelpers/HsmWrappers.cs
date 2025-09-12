using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Hsm.Runtime;
using FastFsm.Contracts;

namespace FastFsm.Tests.TestHelpers
{
    // ====== SimpleParentChild Wrappers ======
    
    public class SimpleParentChildMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly SimpleParentChildMachineFluent _machine;
        
        public SimpleParentChildMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                SimpleParentChildMachineFluent.S.Idle : 
                (SimpleParentChildMachineFluent.S)Enum.Parse(typeof(SimpleParentChildMachineFluent.S), initialStateName);
            _machine = new SimpleParentChildMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        public List<string> EntryExitLog => _machine.EntryExitLog;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (SimpleParentChildMachineFluent.T)Enum.Parse(typeof(SimpleParentChildMachineFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (SimpleParentChildMachineFluent.T)Enum.Parse(typeof(SimpleParentChildMachineFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (SimpleParentChildMachineFluent.T)Enum.Parse(typeof(SimpleParentChildMachineFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
        
        // HSM-specific
        public bool IsInHierarchy(object state)
        {
            var typedState = (SimpleParentChildMachineFluent.S)Enum.Parse(typeof(SimpleParentChildMachineFluent.S), state.ToString()!);
            return _machine.IsInHierarchy(typedState);
        }
    }
    
    public class SimpleParentChildMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly SimpleParentChildMachineLegacy _machine;
        
        public SimpleParentChildMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                SimpleParentChildMachineFluent.S.Idle : 
                (SimpleParentChildMachineFluent.S)Enum.Parse(typeof(SimpleParentChildMachineFluent.S), initialStateName);
            _machine = new SimpleParentChildMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        public List<string> EntryExitLog => _machine.EntryExitLog;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (SimpleParentChildMachineFluent.T)Enum.Parse(typeof(SimpleParentChildMachineFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (SimpleParentChildMachineFluent.T)Enum.Parse(typeof(SimpleParentChildMachineFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (SimpleParentChildMachineFluent.T)Enum.Parse(typeof(SimpleParentChildMachineFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
        
        // HSM-specific
        public bool IsInHierarchy(object state)
        {
            var typedState = (SimpleParentChildMachineFluent.S)Enum.Parse(typeof(SimpleParentChildMachineFluent.S), state.ToString()!);
            return _machine.IsInHierarchy(typedState);
        }
    }
    
    // ====== DeepHistory Wrappers ======
    
    public class DeepHistoryMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly DeepHistoryTestsFluent.DeepHistoryMachineFluent _machine;
        
        public DeepHistoryMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                DeepHistoryTestsFluent.S.Out : 
                (DeepHistoryTestsFluent.S)Enum.Parse(typeof(DeepHistoryTestsFluent.S), initialStateName);
            _machine = new DeepHistoryTestsFluent.DeepHistoryMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (DeepHistoryTestsFluent.T)Enum.Parse(typeof(DeepHistoryTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (DeepHistoryTestsFluent.T)Enum.Parse(typeof(DeepHistoryTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (DeepHistoryTestsFluent.T)Enum.Parse(typeof(DeepHistoryTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    public class DeepHistoryMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly DeepHistoryTestsLegacy.DeepHistoryMachineLegacy _machine;
        
        public DeepHistoryMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                DeepHistoryTestsFluent.S.Out : 
                (DeepHistoryTestsFluent.S)Enum.Parse(typeof(DeepHistoryTestsFluent.S), initialStateName);
            _machine = new DeepHistoryTestsLegacy.DeepHistoryMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (DeepHistoryTestsFluent.T)Enum.Parse(typeof(DeepHistoryTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (DeepHistoryTestsFluent.T)Enum.Parse(typeof(DeepHistoryTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (DeepHistoryTestsFluent.T)Enum.Parse(typeof(DeepHistoryTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    // ====== ShallowHistory Wrappers ======
    
    public class ShallowHistoryMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly ShallowHistoryTestsFluent.ShallowHistoryMachineFluent _machine;
        
        public ShallowHistoryMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                ShallowHistoryTestsFluent.S.Outside : 
                (ShallowHistoryTestsFluent.S)Enum.Parse(typeof(ShallowHistoryTestsFluent.S), initialStateName);
            _machine = new ShallowHistoryTestsFluent.ShallowHistoryMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (ShallowHistoryTestsFluent.T)Enum.Parse(typeof(ShallowHistoryTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (ShallowHistoryTestsFluent.T)Enum.Parse(typeof(ShallowHistoryTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (ShallowHistoryTestsFluent.T)Enum.Parse(typeof(ShallowHistoryTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    public class ShallowHistoryMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly ShallowHistoryTestsLegacy.ShallowHistoryMachineLegacy _machine;
        
        public ShallowHistoryMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                ShallowHistoryTestsFluent.S.Outside : 
                (ShallowHistoryTestsFluent.S)Enum.Parse(typeof(ShallowHistoryTestsFluent.S), initialStateName);
            _machine = new ShallowHistoryTestsLegacy.ShallowHistoryMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (ShallowHistoryTestsFluent.T)Enum.Parse(typeof(ShallowHistoryTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (ShallowHistoryTestsFluent.T)Enum.Parse(typeof(ShallowHistoryTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (ShallowHistoryTestsFluent.T)Enum.Parse(typeof(ShallowHistoryTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    // ====== InitialChild Wrappers ======
    
    public class InitialChildMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly InitialChildTestsFluent.InitialChildMachineFluent _machine;
        
        public InitialChildMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                InitialChildTestsFluent.S.Outside : 
                (InitialChildTestsFluent.S)Enum.Parse(typeof(InitialChildTestsFluent.S), initialStateName);
            _machine = new InitialChildTestsFluent.InitialChildMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (InitialChildTestsFluent.T)Enum.Parse(typeof(InitialChildTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (InitialChildTestsFluent.T)Enum.Parse(typeof(InitialChildTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (InitialChildTestsFluent.T)Enum.Parse(typeof(InitialChildTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    public class InitialChildMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly InitialChildTestsLegacy.InitialChildMachineLegacy _machine;
        
        public InitialChildMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                InitialChildTestsFluent.S.Outside : 
                (InitialChildTestsFluent.S)Enum.Parse(typeof(InitialChildTestsFluent.S), initialStateName);
            _machine = new InitialChildTestsLegacy.InitialChildMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (InitialChildTestsFluent.T)Enum.Parse(typeof(InitialChildTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (InitialChildTestsFluent.T)Enum.Parse(typeof(InitialChildTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (InitialChildTestsFluent.T)Enum.Parse(typeof(InitialChildTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    // ====== InternalTransition Wrappers ======
    
    public class InternalTransitionHsmMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly InternalTransitionTestsFluent.InternalMachineFluent _machine;
        
        public InternalTransitionHsmMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                InternalTransitionTestsFluent.S.Parent : 
                (InternalTransitionTestsFluent.S)Enum.Parse(typeof(InternalTransitionTestsFluent.S), initialStateName);
            _machine = new InternalTransitionTestsFluent.InternalMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical | ApiCapabilities.HasInternalTransitions;
        public List<string> Log => _machine.Log;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (InternalTransitionTestsFluent.T)Enum.Parse(typeof(InternalTransitionTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (InternalTransitionTestsFluent.T)Enum.Parse(typeof(InternalTransitionTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (InternalTransitionTestsFluent.T)Enum.Parse(typeof(InternalTransitionTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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
    
    public class InternalTransitionHsmMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly InternalTransitionTestsLegacy.InternalMachineLegacy _machine;
        
        public InternalTransitionHsmMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                InternalTransitionTestsFluent.S.Parent : 
                (InternalTransitionTestsFluent.S)Enum.Parse(typeof(InternalTransitionTestsFluent.S), initialStateName);
            _machine = new InternalTransitionTestsLegacy.InternalMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical | ApiCapabilities.HasInternalTransitions;
        public List<string> Log => _machine.Log;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (InternalTransitionTestsFluent.T)Enum.Parse(typeof(InternalTransitionTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (InternalTransitionTestsFluent.T)Enum.Parse(typeof(InternalTransitionTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (InternalTransitionTestsFluent.T)Enum.Parse(typeof(InternalTransitionTestsFluent.T), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers() => 
            _machine.GetPermittedTriggers().Cast<object>().ToList();
        
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