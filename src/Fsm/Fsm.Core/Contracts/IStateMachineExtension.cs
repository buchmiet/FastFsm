using System;
using FastFsm.Exceptions;

namespace FastFsm.Contracts;

/// <summary>
/// Receives typed, synchronous notifications from a state machine.
/// </summary>
public interface IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Declares the notifications consumed by this extension.
    /// </summary>
    ExtensionHooks Hooks => ExtensionHooks.Transitions;

    void OnAttemptStarting(in TransitionAttemptContext<TState, TTrigger> attempt) { }

    void OnTransitionMatched(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> matched) { }

    void OnAttemptCompleted(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionResult<TState> result) { }

    void OnGuardEvaluating(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName) { }

    void OnGuardEvaluated(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName,
        bool result) { }

    void OnStateExiting(in TransitionAttemptContext<TState, TTrigger> attempt, TState state) { }

    void OnStateEntered(in TransitionAttemptContext<TState, TTrigger> attempt, TState state) { }

    void OnCallbackExecuting(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TransitionStage stage,
        string callbackName) { }

    void OnCallbackFaulted(
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TransitionStage stage,
        string callbackName,
        Exception exception) { }

    void OnMachineStarted(Guid instanceId, TState initialState) { }
}
