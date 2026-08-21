using System;
using FastFsm.Exceptions;

namespace FastFsm.Contracts;

public readonly struct TransitionResult<TState>
    where TState : unmanaged, Enum
{
    public TransitionResult(
        TransitionOutcome outcome,
        TState finalState,
        TState? resolvedTarget = null,
        TransitionInfo<TState>? matchedTransition = null,
        TransitionStage? stage = null,
        Exception? exception = null)
    {
        Outcome = outcome;
        FinalState = finalState;
        ResolvedTarget = resolvedTarget;
        MatchedTransition = matchedTransition;
        Stage = stage;
        Exception = exception;
    }

    public TransitionOutcome Outcome { get; }
    public TState FinalState { get; }
    public TState? ResolvedTarget { get; }
    public TransitionInfo<TState>? MatchedTransition { get; }
    public TransitionStage? Stage { get; }
    public Exception? Exception { get; }
}

public enum TransitionOutcome
{
    Succeeded,
    GuardRejected,
    UnhandledTrigger,
    InvalidPayload,
    Canceled,
    Faulted
}