using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for PayloadStateMachineFluent
    /// </summary>
    public class PayloadStateMachineFluentWrapper : IStateMachineTestWrapper
    {
        private readonly PayloadStateMachineFluent _machine;
        
        public PayloadStateMachineFluentWrapper(string initialStateName)
        {
            var state = (Machines.TestState)Enum.Parse(typeof(Machines.TestState), initialStateName);
            _machine = new PayloadStateMachineFluent(state);
        }
        
        public PayloadStateMachineFluentWrapper(Machines.TestState initialState)
        {
            _machine = new PayloadStateMachineFluent(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasDefaultPayload; // Has default payload type
        
        public Machines.TestPayload? LastPayload => _machine.LastPayload;
        
        public bool GuardResult
        {
            get => _machine.GuardResult;
            set => _machine.GuardResult = value;
        }
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.TestTrigger)Enum.Parse(typeof(Machines.TestTrigger), trigger.ToString()!);
            
            // Get transition shape
            var shape = TransitionIntrospection.GetTransitionShape("PayloadStateMachine", typedTrigger.ToString(), _machine.CurrentState.ToString());
            
            // Coerce payload if needed
            if (shape != null && shape.RequiresPayload && payload == null)
            {
                // This machine has DefaultPayloadType, so we need a payload
                throw new InvalidOperationException(
                    $"Transition {typedTrigger} requires payload of type TestPayload " +
                    $"(machine: PayloadStateMachine, state: {_machine.CurrentState})");
            }
            
            if (payload == null)
                return _machine.TryFire(typedTrigger);
            
            // Coerce payload to TestPayload
            var coercedPayload = CoercePayloadToTestPayload(payload);
            return _machine.TryFire(typedTrigger, coercedPayload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.TestTrigger)Enum.Parse(typeof(Machines.TestTrigger), trigger.ToString()!);
            
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
            {
                var coercedPayload = CoercePayloadToTestPayload(payload);
                _machine.Fire(typedTrigger, coercedPayload);
            }
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Machines.TestTrigger)Enum.Parse(typeof(Machines.TestTrigger), trigger.ToString()!);
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
        
        private Machines.TestPayload CoercePayloadToTestPayload(object payload)
        {
            if (payload is Machines.TestPayload tp)
                return tp;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new Machines.TestPayload
                {
                    Id = dict.ContainsKey("Id") ? Convert.ToInt32(dict["Id"]) : 0,
                    Data = dict.ContainsKey("Data") ? dict["Data"].ToString() ?? "" : ""
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to TestPayload. " +
                $"Expected TestPayload or IDictionary<string,object>");
        }
    }
    
    /// <summary>
    /// Wrapper for PayloadStateMachine (Legacy)
    /// </summary>
    public class PayloadStateMachineLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly PayloadStateMachine _machine;
        
        public PayloadStateMachineLegacyWrapper(string initialStateName)
        {
            var state = (Machines.TestState)Enum.Parse(typeof(Machines.TestState), initialStateName);
            _machine = new PayloadStateMachine(state);
        }
        
        public PayloadStateMachineLegacyWrapper(Machines.TestState initialState)
        {
            _machine = new PayloadStateMachine(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.HasDefaultPayload; // Has default payload type
        
        public Machines.TestPayload? LastPayload => _machine.LastPayload;
        
        public bool GuardResult
        {
            get => _machine.GuardResult;
            set => _machine.GuardResult = value;
        }
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.TestTrigger)Enum.Parse(typeof(Machines.TestTrigger), trigger.ToString()!);
            
            // Get transition shape
            var shape = TransitionIntrospection.GetTransitionShape("PayloadStateMachine", typedTrigger.ToString(), _machine.CurrentState.ToString());
            
            // Coerce payload if needed
            if (shape != null && shape.RequiresPayload && payload == null)
            {
                // This machine has DefaultPayloadType, so we need a payload
                throw new InvalidOperationException(
                    $"Transition {typedTrigger} requires payload of type TestPayload " +
                    $"(machine: PayloadStateMachine, state: {_machine.CurrentState})");
            }
            
            if (payload == null)
                return _machine.TryFire(typedTrigger);
            
            // Coerce payload to TestPayload
            var coercedPayload = CoercePayloadToTestPayload(payload);
            return _machine.TryFire(typedTrigger, coercedPayload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = (Machines.TestTrigger)Enum.Parse(typeof(Machines.TestTrigger), trigger.ToString()!);
            
            if (payload == null)
                _machine.Fire(typedTrigger);
            else
            {
                var coercedPayload = CoercePayloadToTestPayload(payload);
                _machine.Fire(typedTrigger, coercedPayload);
            }
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = (Machines.TestTrigger)Enum.Parse(typeof(Machines.TestTrigger), trigger.ToString()!);
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
        
        private Machines.TestPayload CoercePayloadToTestPayload(object payload)
        {
            if (payload is Machines.TestPayload tp)
                return tp;
                
            if (payload is IDictionary<string, object> dict)
            {
                return new Machines.TestPayload
                {
                    Id = dict.ContainsKey("Id") ? Convert.ToInt32(dict["Id"]) : 0,
                    Data = dict.ContainsKey("Data") ? dict["Data"].ToString() ?? "" : ""
                };
            }
            
            throw new InvalidOperationException(
                $"Cannot coerce payload from {payload.GetType().Name} to TestPayload. " +
                $"Expected TestPayload or IDictionary<string,object>");
        }
    }
}