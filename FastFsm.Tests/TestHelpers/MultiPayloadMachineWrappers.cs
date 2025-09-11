using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Payload;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for MultiPayloadMachineFluent
    /// </summary>
    public class MultiPayloadMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly MultiPayloadMachineFluent _machine;
        
        public MultiPayloadMachineFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<MultiState>(
                "FullMultiPayload", initialStateName);
            var state = (MultiState)Enum.Parse(typeof(MultiState), resolvedName);
            _machine = new MultiPayloadMachineFluent(state);
        }
        
        public MultiPayloadMachineFluentWrapper(MultiState initialState)
        {
            _machine = new MultiPayloadMachineFluent(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasMultiPayloads; // Different payloads per trigger
        
        public string CurrentSetting => _machine.CurrentSetting;
        public int ProcessedValue => _machine.ProcessedValue;
        public string LastErrorCode => _machine.LastErrorCode;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (MultiTrigger)Enum.Parse(typeof(MultiTrigger), trigger.ToString()!);
            
            // Get transition shape to determine expected payload type
            var shape = TransitionIntrospection.GetTransitionShape("FullMultiPayloadMachine", typedTrigger.ToString(), _machine.CurrentState.ToString());
            
            if (shape?.RequiresPayload == true && payload == null)
            {
                throw new InvalidOperationException(
                    $"Transition {typedTrigger} requires payload of type {shape.ExpectedPayloadType?.Name} " +
                    $"(machine: MultiPayloadMachine, state: {_machine.CurrentState})");
            }
            
            if (payload == null)
                return _machine.TryFire(typedTrigger);
            
            // Coerce payload based on trigger
            var coercedPayload = CoercePayloadForTrigger(typedTrigger, payload);
            return _machine.TryFire(typedTrigger, coercedPayload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (MultiTrigger)Enum.Parse(typeof(MultiTrigger), trigger.ToString()!);
            
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
            {
                var coercedPayload = CoercePayloadForTrigger(typedTrigger, payload);
                _machine.Fire(typedTrigger, coercedPayload);
            }
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (MultiTrigger)Enum.Parse(typeof(MultiTrigger), trigger.ToString()!);
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
        
        private object CoercePayloadForTrigger(MultiTrigger trigger, object payload)
        {
            // Based on trigger, determine expected type
            switch (trigger)
            {
                case MultiTrigger.Configure:
                    return CoerceToConfigPayload(payload);
                    
                case MultiTrigger.Process:
                    return CoerceToDataPayload(payload);
                    
                case MultiTrigger.Error:
                    return CoerceToErrorPayload(payload);
                    
                default:
                    return payload;
            }
        }
        
        private ConfigPayload CoerceToConfigPayload(object payload)
        {
            if (payload is ConfigPayload cp)
                return cp;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new ConfigPayload
                {
                    Setting = dict.ContainsKey("Setting") ? dict["Setting"].ToString() ?? "" : ""
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to ConfigPayload");
        }
        
        private DataPayload CoerceToDataPayload(object payload)
        {
            if (payload is DataPayload dp)
                return dp;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new DataPayload
                {
                    Value = dict.ContainsKey("Value") ? Convert.ToInt32(dict["Value"]) : 0
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to DataPayload");
        }
        
        private ErrorPayload CoerceToErrorPayload(object payload)
        {
            if (payload is ErrorPayload ep)
                return ep;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new ErrorPayload
                {
                    Code = dict.ContainsKey("Code") ? dict["Code"].ToString() ?? "" : "",
                    Message = dict.ContainsKey("Message") ? dict["Message"].ToString() ?? "" : ""
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to ErrorPayload");
        }
    }
    
    /// <summary>
    /// Wrapper for MultiPayloadMachineLegacy
    /// </summary>
    public class MultiPayloadMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly MultiPayloadMachineLegacy _machine;
        
        public MultiPayloadMachineLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<MultiState>(
                "FullMultiPayload", initialStateName);
            var state = (MultiState)Enum.Parse(typeof(MultiState), resolvedName);
            _machine = new MultiPayloadMachineLegacy(state);
        }
        
        public MultiPayloadMachineLegacyWrapper(MultiState initialState)
        {
            _machine = new MultiPayloadMachineLegacy(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasMultiPayloads; // Different payloads per trigger
        
        public string CurrentSetting => _machine.CurrentSetting;
        public int ProcessedValue => _machine.ProcessedValue;
        public string LastErrorCode => _machine.LastErrorCode;
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (MultiTrigger)Enum.Parse(typeof(MultiTrigger), trigger.ToString()!);
            
            // Get transition shape to determine expected payload type
            var shape = TransitionIntrospection.GetTransitionShape("FullMultiPayloadMachine", typedTrigger.ToString(), _machine.CurrentState.ToString());
            
            if (shape?.RequiresPayload == true && payload == null)
            {
                throw new InvalidOperationException(
                    $"Transition {typedTrigger} requires payload of type {shape.ExpectedPayloadType?.Name} " +
                    $"(machine: MultiPayloadMachine, state: {_machine.CurrentState})");
            }
            
            if (payload == null)
                return _machine.TryFire(typedTrigger);
            
            // Coerce payload based on trigger
            var coercedPayload = CoercePayloadForTrigger(typedTrigger, payload);
            return _machine.TryFire(typedTrigger, coercedPayload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (MultiTrigger)Enum.Parse(typeof(MultiTrigger), trigger.ToString()!);
            
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
            {
                var coercedPayload = CoercePayloadForTrigger(typedTrigger, payload);
                _machine.Fire(typedTrigger, coercedPayload);
            }
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (MultiTrigger)Enum.Parse(typeof(MultiTrigger), trigger.ToString()!);
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
        
        private object CoercePayloadForTrigger(MultiTrigger trigger, object payload)
        {
            // Based on trigger, determine expected type
            switch (trigger)
            {
                case MultiTrigger.Configure:
                    return CoerceToConfigPayload(payload);
                    
                case MultiTrigger.Process:
                    return CoerceToDataPayload(payload);
                    
                case MultiTrigger.Error:
                    return CoerceToErrorPayload(payload);
                    
                default:
                    return payload;
            }
        }
        
        private ConfigPayload CoerceToConfigPayload(object payload)
        {
            if (payload is ConfigPayload cp)
                return cp;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new ConfigPayload
                {
                    Setting = dict.ContainsKey("Setting") ? dict["Setting"].ToString() ?? "" : ""
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to ConfigPayload");
        }
        
        private DataPayload CoerceToDataPayload(object payload)
        {
            if (payload is DataPayload dp)
                return dp;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new DataPayload
                {
                    Value = dict.ContainsKey("Value") ? Convert.ToInt32(dict["Value"]) : 0
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to DataPayload");
        }
        
        private ErrorPayload CoerceToErrorPayload(object payload)
        {
            if (payload is ErrorPayload ep)
                return ep;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new ErrorPayload
                {
                    Code = dict.ContainsKey("Code") ? dict["Code"].ToString() ?? "" : "",
                    Message = dict.ContainsKey("Message") ? dict["Message"].ToString() ?? "" : ""
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to ErrorPayload");
        }
    }
}