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
    /// Wrapper for SimpleParentChildMachineLegacy
    /// </summary>
    public class SimpleParentChildMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly Features.Hsm.CompileTime.HsmParsingCompilationTestsLegacy.SimpleParentChildMachineLegacy _machine;
        
        public SimpleParentChildMachineLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<Features.Hsm.CompileTime.HsmState>(
                "SimpleParentChild", initialStateName);
            var state = (Features.Hsm.CompileTime.HsmState)Enum.Parse(typeof(Features.Hsm.CompileTime.HsmState), resolvedName);
            _machine = new Features.Hsm.CompileTime.HsmParsingCompilationTestsLegacy.SimpleParentChildMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public List<string> EntryExitLog => new List<string>(); // Placeholder for compatibility
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Features.Hsm.CompileTime.HsmTrigger)Enum.Parse(typeof(Features.Hsm.CompileTime.HsmTrigger), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Features.Hsm.CompileTime.HsmTrigger)Enum.Parse(typeof(Features.Hsm.CompileTime.HsmTrigger), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Features.Hsm.CompileTime.HsmTrigger)Enum.Parse(typeof(Features.Hsm.CompileTime.HsmTrigger), trigger.ToString()!);
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
            var typedState = (Features.Hsm.CompileTime.HsmState)Enum.Parse(typeof(Features.Hsm.CompileTime.HsmState), state.ToString()!);
            return _machine.IsIn(typedState);
        }
        
        public IReadOnlyList<object> GetActivePath()
        {
            return _machine.GetActivePath().Cast<object>().ToList();
        }
    }
    
    /// <summary>
    /// Wrapper for DeepHistoryTestMachineFluent
    /// </summary>
    public class DeepHistoryTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly Features.Hsm.Runtime.DeepHistoryTestsFluent.DeepHistoryMachineFluent _machine;
        
        public DeepHistoryTestMachineFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<Features.Hsm.Runtime.DeepHistoryTestsFluent.S>(
                "DeepHistory", initialStateName);
            var state = (Features.Hsm.Runtime.DeepHistoryTestsFluent.S)Enum.Parse(typeof(Features.Hsm.Runtime.DeepHistoryTestsFluent.S), resolvedName);
            _machine = new Features.Hsm.Runtime.DeepHistoryTestsFluent.DeepHistoryMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        public ApiCapabilities Caps => ApiCapabilities.IsHierarchical;
        
        public void Start() => _machine.Start();
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Features.Hsm.Runtime.DeepHistoryTestsFluent.T)Enum.Parse(typeof(Features.Hsm.Runtime.DeepHistoryTestsFluent.T), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Features.Hsm.Runtime.DeepHistoryTestsFluent.T)Enum.Parse(typeof(Features.Hsm.Runtime.DeepHistoryTestsFluent.T), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Features.Hsm.Runtime.DeepHistoryTestsFluent.T)Enum.Parse(typeof(Features.Hsm.Runtime.DeepHistoryTestsFluent.T), trigger.ToString()!);
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
    
    /// <summary>
    /// Wrapper for ShallowHistoryTestMachineFluent
    /// </summary>
    public class ShallowHistoryTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly ShallowHistoryTestsFluent.ShallowHistoryMachineFluent _machine;
        
        public ShallowHistoryTestMachineFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<ShallowHistoryTestsFluent.S>(
                "ShallowHistory", initialStateName);
            var state = (ShallowHistoryTestsFluent.S)Enum.Parse(typeof(ShallowHistoryTestsFluent.S), resolvedName);
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
    
    /// <summary>
    /// Wrapper for InitialChildTestMachineFluent
    /// </summary>
    public class InitialChildTestMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly InitialChildTestsFluent.InitialChildMachineFluent _machine;
        
        public InitialChildTestMachineFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<InitialChildTestsFluent.S>(
                "InitialChild", initialStateName);
            var state = (InitialChildTestsFluent.S)Enum.Parse(typeof(InitialChildTestsFluent.S), resolvedName);
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