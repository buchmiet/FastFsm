

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StateMachine.Contracts;


namespace StateMachine.Runtime;

/// <summary>
/// Base class providing common functionality for generated state machines
/// </summary>
public abstract class StateMachineBase<TState, TTrigger>(TState initialState) : IStateMachine<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    protected TState _currentState = initialState;
    
    public TState CurrentState => _currentState;

    public abstract bool TryFire(TTrigger trigger, object? payload = null);
    
    public virtual void Fire(TTrigger trigger, object? payload = null)
    {
        if (!TryFire(trigger, payload))
        {
            throw new InvalidOperationException(
                $"No transition from state '{_currentState}' on trigger '{trigger}'");
        }
    }
    
    public abstract bool CanFire(TTrigger trigger);
    
    public abstract IReadOnlyList<TTrigger> GetPermittedTriggers();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool SetState(TState newState)
    {
        _currentState = newState;
        return true;
    }
}
