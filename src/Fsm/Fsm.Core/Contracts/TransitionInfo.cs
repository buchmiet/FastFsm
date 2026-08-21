using System;

namespace FastFsm.Contracts;

public readonly struct TransitionInfo<TState>
    where TState : unmanaged, Enum
{
    public TransitionInfo(TState handledAtState, TState? declaredTarget, TransitionKind kind)
    {
        HandledAtState = handledAtState;
        DeclaredTarget = declaredTarget;
        Kind = kind;
    }

    public TState HandledAtState { get; }
    public TState? DeclaredTarget { get; }
    public TransitionKind Kind { get; }
}

public enum TransitionKind
{
    External,
    Internal
}