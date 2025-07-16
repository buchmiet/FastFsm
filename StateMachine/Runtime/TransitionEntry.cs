

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace StateMachine.Runtime;

/// <summary>
/// Represents a single transition in the state machine.
/// Designed for optimal memory layout and access patterns.
/// </summary>
public readonly struct TransitionEntry<TState, TTrigger>(
    TState fromState,
    TTrigger trigger,
    TState toState,
    int guardIndex = -1,
    int actionIndex = -1)
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    public readonly TState FromState = fromState;
    public readonly TTrigger Trigger = trigger;
    public readonly TState ToState = toState;
    public readonly int GuardIndex = guardIndex;    // Index into guards array (-1 if none)
    public readonly int ActionIndex = actionIndex;   // Index into actions array (-1 if none)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(TState state, TTrigger trigger)
    {
        // Optimized comparison using generic constraints
        return EqualityComparer<TState>.Default.Equals(FromState, state) &&
               EqualityComparer<TTrigger>.Default.Equals(Trigger, trigger);
    }
}
