using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FastFsm.Contracts;
using FastFsm.Exceptions;
#if FSM_LOGGING_ENABLED
using Microsoft.Extensions.Logging;
#endif

namespace FastFsm.Runtime.Extensions;

internal sealed class ExtensionSet<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    private ExtensionSet(
        IStateMachineExtension<TState, TTrigger>[] items,
        ExtensionHooks hooks)
    {
        Items = items;
        PublicItems = Array.AsReadOnly(items);
        Hooks = hooks;
    }

    public IStateMachineExtension<TState, TTrigger>[] Items { get; }
    public IReadOnlyList<IStateMachineExtension<TState, TTrigger>> PublicItems { get; }
    public ExtensionHooks Hooks { get; }

    public static ExtensionSet<TState, TTrigger> Create(
        IEnumerable<IStateMachineExtension<TState, TTrigger>>? extensions)
    {
        if (extensions is null)
            return new ExtensionSet<TState, TTrigger>([], ExtensionHooks.None);

        var items = new List<IStateMachineExtension<TState, TTrigger>>();
        var hooks = ExtensionHooks.None;
        foreach (var extension in extensions)
        {
            if (extension is null)
                throw new ArgumentException("The extension collection cannot contain null items.", nameof(extensions));

            items.Add(extension);
            hooks |= extension.Hooks;
        }

        return new ExtensionSet<TState, TTrigger>(items.ToArray(), hooks);
    }
}

internal sealed class ExtensionRunner
{
#if FSM_LOGGING_ENABLED
    private readonly ILogger? _logger;

    public ExtensionRunner(ILogger? logger = null)
    {
        _logger = logger;
    }
#else
    public ExtensionRunner() { }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunAttemptStarting<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Transitions) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnAttemptStarting(in attempt); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnAttemptStarting), in attempt, attempt.SourceState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTransitionMatched<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> matched)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Transitions) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnTransitionMatched(in attempt, in matched); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnTransitionMatched), in attempt, attempt.SourceState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunAttemptCompleted<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionResult<TState> result)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Transitions) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnAttemptCompleted(in attempt, in result); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnAttemptCompleted), in attempt, result.FinalState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunGuardEvaluating<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Guards) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnGuardEvaluating(in attempt, in candidate, guardName); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnGuardEvaluating), in attempt, attempt.SourceState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunGuardEvaluated<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        in TransitionInfo<TState> candidate,
        string guardName,
        bool result)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Guards) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnGuardEvaluated(in attempt, in candidate, guardName, result); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnGuardEvaluated), in attempt, attempt.SourceState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunStateExiting<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TState state)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.States) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnStateExiting(in attempt, state); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnStateExiting), in attempt, state, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunStateEntered<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TState state)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.States) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnStateEntered(in attempt, state); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnStateEntered), in attempt, state, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunCallbackExecuting<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TransitionStage stage,
        string callbackName)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Callbacks) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnCallbackExecuting(in attempt, stage, callbackName); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnCallbackExecuting), in attempt, attempt.SourceState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunCallbackFaulted<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TransitionStage stage,
        string callbackName,
        Exception exception)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Callbacks) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnCallbackFaulted(in attempt, stage, callbackName, exception); }
            catch (Exception ex) { Report(set.Items[i], nameof(IStateMachineExtension<TState, TTrigger>.OnCallbackFaulted), in attempt, attempt.SourceState, ex); }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunMachineStarted<TState, TTrigger>(
        ExtensionSet<TState, TTrigger> set,
        Guid instanceId,
        TState initialState)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        if ((set.Hooks & ExtensionHooks.Lifecycle) == 0) return;
        for (var i = 0; i < set.Items.Length; i++)
        {
            try { set.Items[i].OnMachineStarted(instanceId, initialState); }
#if FSM_LOGGING_ENABLED
            catch (Exception ex)
            {
                if (_logger?.IsEnabled(LogLevel.Error) == true)
                {
                    _logger.LogError(
                        ex,
                        "Extension {ExtensionType} threw exception in {MethodName}. InstanceId={InstanceId}, InitialState={InitialState}",
                        set.Items[i].GetType().Name,
                        nameof(IStateMachineExtension<TState, TTrigger>.OnMachineStarted),
                        instanceId,
                        initialState);
                }
            }
#else
            catch (Exception) { }
#endif
        }
    }

    private void Report<TState, TTrigger>(
        IStateMachineExtension<TState, TTrigger> extension,
        string methodName,
        in TransitionAttemptContext<TState, TTrigger> attempt,
        TState finalState,
        Exception exception)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
#if FSM_LOGGING_ENABLED
        if (_logger?.IsEnabled(LogLevel.Error) == true)
        {
            _logger.LogError(
                exception,
                "Extension {ExtensionType} threw exception in {MethodName}. InstanceId={InstanceId}, SourceState={SourceState}, Trigger={Trigger}, FinalState={FinalState}",
                extension.GetType().Name,
                methodName,
                attempt.InstanceId,
                attempt.SourceState,
                attempt.Trigger,
                finalState);
        }
#endif
    }
}