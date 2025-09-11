using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for GuardPermittedMachineFluent
    /// </summary>
    public class GuardPermittedFluentWrapper : IStateMachineTestWrapper
    {
        private readonly GuardPermittedMachineFluent _machine;
        
        public GuardPermittedFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<State>(
                "GuardPermitted", initialStateName);
            var state = (State)Enum.Parse(typeof(State), resolvedName);
            _machine = new GuardPermittedMachineFluent(state);
        }
        
        public bool Allow 
        { 
            get => _machine.Allow; 
            set => _machine.Allow = value; 
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.None; // Guards are sync-only
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Trigger)EnumConverterV2.ConvertTrigger(trigger, "GuardPermitted", StateMachineWrapperFactory.ApiType.Fluent);
            return payload == null 
                ? _machine.TryFire(typedTrigger) 
                : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Trigger)EnumConverterV2.ConvertTrigger(trigger, "GuardPermitted", StateMachineWrapperFactory.ApiType.Fluent);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Trigger)EnumConverterV2.ConvertTrigger(trigger, "GuardPermitted", StateMachineWrapperFactory.ApiType.Fluent);
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
    /// Wrapper for GuardPermittedMachineLegacy
    /// </summary>
    public class GuardPermittedLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly GuardPermittedMachineLegacy _machine;
        
        public GuardPermittedLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<State>(
                "GuardPermitted", initialStateName);
            var state = (State)Enum.Parse(typeof(State), resolvedName);
            _machine = new GuardPermittedMachineLegacy(state);
        }
        
        public bool Allow 
        { 
            get => _machine.Allow; 
            set => _machine.Allow = value; 
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.None; // Guards are sync-only
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Trigger)EnumConverterV2.ConvertTrigger(trigger, "GuardPermitted", StateMachineWrapperFactory.ApiType.Legacy);
            return payload == null 
                ? _machine.TryFire(typedTrigger) 
                : _machine.TryFire(typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Trigger)EnumConverterV2.ConvertTrigger(trigger, "GuardPermitted", StateMachineWrapperFactory.ApiType.Legacy);
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
                _machine.Fire(typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Trigger)EnumConverterV2.ConvertTrigger(trigger, "GuardPermitted", StateMachineWrapperFactory.ApiType.Legacy);
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
}
