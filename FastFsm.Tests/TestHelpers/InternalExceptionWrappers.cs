using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Machines;
using FastFsm.Tests.Features.Exceptions;
using FastFsm.Contracts;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for InternalTransitionMachineFluent
    /// </summary>
    public class InternalTransitionMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly InternalTransitionMachineFluent _machine;
        
        public InternalTransitionMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                StateCallbackTests.InternalState.Active : 
                (StateCallbackTests.InternalState)Enum.Parse(typeof(StateCallbackTests.InternalState), initialStateName);
            _machine = new InternalTransitionMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasInternalTransitions;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InternalTrigger)Enum.Parse(typeof(StateCallbackTests.InternalTrigger), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InternalTrigger)Enum.Parse(typeof(StateCallbackTests.InternalTrigger), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.InternalTrigger)Enum.Parse(typeof(StateCallbackTests.InternalTrigger), trigger.ToString()!);
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
    }
    
    /// <summary>
    /// Wrapper for InternalTransitionMachineLegacy
    /// </summary>
    public class InternalTransitionMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly InternalTransitionMachineLegacy _machine;
        
        public InternalTransitionMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                StateCallbackTests.InternalState.Active : 
                (StateCallbackTests.InternalState)Enum.Parse(typeof(StateCallbackTests.InternalState), initialStateName);
            _machine = new InternalTransitionMachineLegacy(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasInternalTransitions;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InternalTrigger)Enum.Parse(typeof(StateCallbackTests.InternalTrigger), trigger.ToString()!);
            return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.InternalTrigger)Enum.Parse(typeof(StateCallbackTests.InternalTrigger), trigger.ToString()!);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.InternalTrigger)Enum.Parse(typeof(StateCallbackTests.InternalTrigger), trigger.ToString()!);
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
    }
    
    /// <summary>
    /// Wrapper for ExceptionCallbackMachineFluent
    /// </summary>
    public class ExceptionCallbackMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly ExceptionCallbackMachineFluent _machine;
        
        public ExceptionCallbackMachineFluentWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                StateCallbackTests.ExceptionState.A : 
                (StateCallbackTests.ExceptionState)Enum.Parse(typeof(StateCallbackTests.ExceptionState), initialStateName);
            _machine = new ExceptionCallbackMachineFluent(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasAsync | ApiCapabilities.RequiresAsyncPath;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            // This machine has async actions, so sync path should throw or bridge to async
            try
            {
                return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("FSM204"))
            {
                // Async path required
                throw;
            }
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            try
            {
                if (payload == null)
                    _machine.Fire(typedTrigger);
                else
                    _machine.Fire(typedTrigger, payload);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("FSM204"))
            {
                // Async path required
                throw;
            }
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers()
        {
            return _machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        public async ValueTask StartAsync(CancellationToken ct = default)
        {
            if (_machine is IStateMachineAsync<StateCallbackTests.ExceptionState, StateCallbackTests.ExceptionTrigger> asyncMachine)
            {
                await asyncMachine.StartAsync(ct);
            }
            else
            {
                _machine.Start();
            }
        }
        
        public async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            if (_machine is IStateMachineAsync<StateCallbackTests.ExceptionState, StateCallbackTests.ExceptionTrigger> asyncMachine)
            {
                return await asyncMachine.TryFireAsync(typedTrigger, payload, ct);
            }
            
            return TryFire(trigger, payload);
        }
        
        public async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            if (_machine is IStateMachineAsync<StateCallbackTests.ExceptionState, StateCallbackTests.ExceptionTrigger> asyncMachine)
            {
                await asyncMachine.FireAsync(typedTrigger, payload, ct);
            }
            else
            {
                Fire(trigger, payload);
            }
        }
    }
    
    /// <summary>
    /// Wrapper for ExceptionCallbackMachine (Legacy)
    /// </summary>
    public class ExceptionCallbackMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly ExceptionCallbackMachine _machine;
        
        public ExceptionCallbackMachineLegacyWrapper(string initialStateName)
        {
            var state = string.IsNullOrEmpty(initialStateName) ? 
                StateCallbackTests.ExceptionState.A : 
                (StateCallbackTests.ExceptionState)Enum.Parse(typeof(StateCallbackTests.ExceptionState), initialStateName);
            _machine = new ExceptionCallbackMachine(state);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasAsync | ApiCapabilities.RequiresAsyncPath;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            try
            {
                return payload == null ? _machine.TryFire(typedTrigger) : _machine.TryFire(typedTrigger, payload);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("FSM204"))
            {
                // Async path required
                throw;
            }
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            try
            {
                if (payload == null)
                    _machine.Fire(typedTrigger);
                else
                    _machine.Fire(typedTrigger, payload);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("FSM204"))
            {
                // Async path required
                throw;
            }
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            return _machine.CanFire(typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers()
        {
            return _machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        public async ValueTask StartAsync(CancellationToken ct = default)
        {
            if (_machine is IStateMachineAsync<StateCallbackTests.ExceptionState, StateCallbackTests.ExceptionTrigger> asyncMachine)
            {
                await asyncMachine.StartAsync(ct);
            }
            else
            {
                _machine.Start();
            }
        }
        
        public async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            if (_machine is IStateMachineAsync<StateCallbackTests.ExceptionState, StateCallbackTests.ExceptionTrigger> asyncMachine)
            {
                return await asyncMachine.TryFireAsync(typedTrigger, payload, ct);
            }
            
            return TryFire(trigger, payload);
        }
        
        public async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var typedTrigger = (StateCallbackTests.ExceptionTrigger)Enum.Parse(typeof(StateCallbackTests.ExceptionTrigger), trigger.ToString()!);
            
            if (_machine is IStateMachineAsync<StateCallbackTests.ExceptionState, StateCallbackTests.ExceptionTrigger> asyncMachine)
            {
                await asyncMachine.FireAsync(typedTrigger, payload, ct);
            }
            else
            {
                Fire(trigger, payload);
            }
        }
    }
}