using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Contracts;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Base class for state machine wrappers providing common functionality
    /// </summary>
    public abstract class StateMachineWrapperBase<TState, TTrigger, TMachine> : IStateMachineTestWrapper
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
        where TMachine : IStateMachineSync<TState, TTrigger>
    {
        protected readonly TMachine Machine;
        protected readonly string MachineName;
        private readonly StateMachineWrapperFactory.ApiType _apiType;
        
        protected StateMachineWrapperBase(TMachine machine, string machineName, StateMachineWrapperFactory.ApiType apiType)
        {
            Machine = machine ?? throw new ArgumentNullException(nameof(machine));
            MachineName = machineName ?? throw new ArgumentNullException(nameof(machineName));
            _apiType = apiType;
        }
        
        public object CurrentState => Machine.CurrentState;
        
        public abstract ApiCapabilities Caps { get; }
        
        #region State Conversion
        
        protected TState ToState(object state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
                
            if (state is TState typedState)
                return typedState;
                
            // Use EnumConverterV2 for conversion
            if (_apiType == StateMachineWrapperFactory.ApiType.Fluent)
            {
                return EnumConverterV2.ToFluent<TState>(state, MachineName);
            }
            else
            {
                return EnumConverterV2.ToLegacy<TState>(state, MachineName);
            }
        }
        
        protected TTrigger ToTrigger(object trigger)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
                
            if (trigger is TTrigger typedTrigger)
                return typedTrigger;
                
            // Use extension method for conversion
            var converted = trigger.ToConcreteTrigger(_apiType, MachineName);
            return (TTrigger)converted;
        }
        
        protected object? CoercePayload(object? payload, TTrigger trigger)
        {
            // Get transition shape to understand payload requirements
            var shape = TransitionIntrospection.GetTransitionShape(MachineName, trigger.ToString(), Machine.CurrentState.ToString());
            
            if (shape == null)
            {
                // No shape info - pass through as-is
                return payload;
            }
            
            // Check if async is required but sync path was called
            if (shape.IsAsync && !IsAsyncContext())
            {
                throw new InvalidOperationException(
                    $"Async path required for transition {trigger} (FSM204). " +
                    $"Use TryFireAsync/FireAsync methods instead. " +
                    $"Machine: {MachineName}, State: {Machine.CurrentState}");
            }
            
            // Perform payload coercion
            return TransitionIntrospection.CoercePayload(payload, shape);
        }
        
        #endregion
        
        #region Synchronous Methods
        
        public virtual void Start()
        {
            Machine.Start();
        }
        
        public virtual bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = ToTrigger(trigger);
            var shape = TransitionIntrospection.GetTransitionShape(MachineName, typedTrigger.ToString(), Machine.CurrentState.ToString());
            
            // If async is required, we need special handling
            if (shape?.IsAsync == true)
            {
                if (Caps.Has(ApiCapabilities.RequiresAsyncPath))
                {
                    // In test scenarios, we can bridge to async
                    return TryFireAsync(trigger, payload).GetAwaiter().GetResult();
                }
            }
            
            var coercedPayload = CoercePayload(payload, typedTrigger);
            
            if (coercedPayload == null)
                return Machine.TryFire(typedTrigger);
            else
                return Machine.TryFire(typedTrigger, coercedPayload);
        }
        
        public virtual void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = ToTrigger(trigger);
            var shape = TransitionIntrospection.GetTransitionShape(MachineName, typedTrigger.ToString(), Machine.CurrentState.ToString());
            
            // If async is required, we need special handling
            if (shape?.IsAsync == true)
            {
                if (Caps.Has(ApiCapabilities.RequiresAsyncPath))
                {
                    // In test scenarios, we can bridge to async
                    FireAsync(trigger, payload).GetAwaiter().GetResult();
                    return;
                }
            }
            
            var coercedPayload = CoercePayload(payload, typedTrigger);
            
            if (coercedPayload == null)
                Machine.Fire(typedTrigger);
            else
                Machine.Fire(typedTrigger, coercedPayload);
        }
        
        public virtual bool CanFire(object trigger)
        {
            var typedTrigger = ToTrigger(trigger);
            return Machine.CanFire(typedTrigger);
        }
        
        public virtual IReadOnlyList<object> GetPermittedTriggers()
        {
            return Machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        #endregion
        
        #region Asynchronous Methods
        
        public virtual async ValueTask StartAsync(CancellationToken ct = default)
        {
            // Check if machine has async Start
            if (Machine is IStateMachineAsync<TState, TTrigger> asyncMachine)
            {
                await asyncMachine.StartAsync(ct);
            }
            else
            {
                Machine.Start();
            }
        }
        
        public virtual async ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var typedTrigger = ToTrigger(trigger);
            SetAsyncContext(true);
            
            try
            {
                var coercedPayload = CoercePayload(payload, typedTrigger);
                
                // Check if machine supports async
                if (Machine is IStateMachineAsync<TState, TTrigger> asyncMachine)
                {
                    if (coercedPayload == null)
                        return await asyncMachine.TryFireAsync(typedTrigger, cancellationToken: ct);
                    else
                        return await asyncMachine.TryFireAsync(typedTrigger, coercedPayload, ct);
                }
                else
                {
                    // Fallback to sync for machines without async support
                    if (coercedPayload == null)
                        return Machine.TryFire(typedTrigger);
                    else
                        return Machine.TryFire(typedTrigger, coercedPayload);
                }
            }
            finally
            {
                SetAsyncContext(false);
            }
        }
        
        public virtual async ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var typedTrigger = ToTrigger(trigger);
            SetAsyncContext(true);
            
            try
            {
                var coercedPayload = CoercePayload(payload, typedTrigger);
                
                // Check if machine supports async
                if (Machine is IStateMachineAsync<TState, TTrigger> asyncMachine)
                {
                    if (coercedPayload == null)
                        await asyncMachine.FireAsync(typedTrigger, cancellationToken: ct);
                    else
                        await asyncMachine.FireAsync(typedTrigger, coercedPayload, ct);
                }
                else
                {
                    // Fallback to sync for machines without async support
                    if (coercedPayload == null)
                        Machine.Fire(typedTrigger);
                    else
                        Machine.Fire(typedTrigger, coercedPayload);
                }
            }
            finally
            {
                SetAsyncContext(false);
            }
        }
        
        #endregion
        
        #region Async Context Tracking
        
        [ThreadStatic]
        private static bool _isAsyncContext;
        
        private static bool IsAsyncContext() => _isAsyncContext;
        
        private static void SetAsyncContext(bool value) => _isAsyncContext = value;
        
        #endregion
    }
    
}