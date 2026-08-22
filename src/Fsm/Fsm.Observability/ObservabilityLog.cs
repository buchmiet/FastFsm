using System;
using Microsoft.Extensions.Logging;

namespace FastFsm.Observability;

internal static class ObservabilityLog
{
    private static readonly EventId AttemptStartingId = new(1200, nameof(AttemptStarting));
    private static readonly EventId TransitionMatchedId = new(1201, nameof(TransitionMatched));
    private static readonly EventId AttemptCompletedId = new(1202, nameof(AttemptCompleted));
    private static readonly EventId MachineStartedId = new(1203, nameof(MachineStarted));
    private static readonly EventId GuardEvaluatingId = new(1204, nameof(GuardEvaluating));
    private static readonly EventId GuardEvaluatedId = new(1205, nameof(GuardEvaluated));
    private static readonly EventId StateExitingId = new(1206, nameof(StateExiting));
    private static readonly EventId StateEnteredId = new(1207, nameof(StateEntered));
    private static readonly EventId CallbackExecutingId = new(1208, nameof(CallbackExecuting));
    private static readonly EventId CallbackFaultedId = new(1209, nameof(CallbackFaulted));

    private static readonly Action<ILogger, string, string, Exception?> AttemptStartingDelegate =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            AttemptStartingId,
            "Transition attempt starting: {SourceState} --({Trigger})-->");

    private static readonly Action<ILogger, string, string, string, string, Exception?> TransitionMatchedDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Debug,
            TransitionMatchedId,
            "Transition matched: {SourceState} --({Trigger})--> handled at {HandledAtState} ({TransitionKind})");

    private static readonly Action<ILogger, string, string, string, string, double, Exception?> AttemptCompletedDelegate =
        LoggerMessage.Define<string, string, string, string, double>(
            LogLevel.Information,
            AttemptCompletedId,
            "Transition attempt completed: {SourceState} --({Trigger})--> {FinalState} outcome={Outcome} durationMs={DurationMs}");

    private static readonly Action<ILogger, string, Exception?> MachineStartedDelegate =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            MachineStartedId,
            "State machine started at {InitialState}");

    private static readonly Action<ILogger, string, string, Exception?> GuardEvaluatingDelegate =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            GuardEvaluatingId,
            "Evaluating guard {GuardName} in {SourceState}");

    private static readonly Action<ILogger, string, bool, string, Exception?> GuardEvaluatedDelegate =
        LoggerMessage.Define<string, bool, string>(
            LogLevel.Debug,
            GuardEvaluatedId,
            "Guard {GuardName} returned {GuardResult} in {SourceState}");

    private static readonly Action<ILogger, string, string, Exception?> StateExitingDelegate =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            StateExitingId,
            "State exiting: {State} from {SourceState}");

    private static readonly Action<ILogger, string, string, Exception?> StateEnteredDelegate =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            StateEnteredId,
            "State entered: {State} from {SourceState}");

    private static readonly Action<ILogger, string, string, string, Exception?> CallbackExecutingDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            CallbackExecutingId,
            "Callback executing: {Stage} {CallbackName} in {SourceState}");

    private static readonly Action<ILogger, string, string, string, Exception?> CallbackFaultedDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            CallbackFaultedId,
            "Callback faulted: {Stage} {CallbackName} in {SourceState}");

    public static void AttemptStarting(ILogger logger, string sourceState, string trigger)
        => AttemptStartingDelegate(logger, sourceState, trigger, null);

    public static void TransitionMatched(
        ILogger logger,
        string sourceState,
        string trigger,
        string handledAtState,
        string transitionKind)
        => TransitionMatchedDelegate(logger, sourceState, trigger, handledAtState, transitionKind, null);

    public static void AttemptCompleted(
        ILogger logger,
        string sourceState,
        string trigger,
        string finalState,
        string outcome,
        double durationMs)
        => AttemptCompletedDelegate(logger, sourceState, trigger, finalState, outcome, durationMs, null);

    public static void MachineStarted(ILogger logger, string initialState)
        => MachineStartedDelegate(logger, initialState, null);

    public static void GuardEvaluating(ILogger logger, string guardName, string sourceState)
        => GuardEvaluatingDelegate(logger, guardName, sourceState, null);

    public static void GuardEvaluated(ILogger logger, string guardName, bool guardResult, string sourceState)
        => GuardEvaluatedDelegate(logger, guardName, guardResult, sourceState, null);

    public static void StateExiting(ILogger logger, string state, string sourceState)
        => StateExitingDelegate(logger, state, sourceState, null);

    public static void StateEntered(ILogger logger, string state, string sourceState)
        => StateEnteredDelegate(logger, state, sourceState, null);

    public static void CallbackExecuting(ILogger logger, string stage, string callbackName, string sourceState)
        => CallbackExecutingDelegate(logger, stage, callbackName, sourceState, null);

    public static void CallbackFaulted(
        ILogger logger,
        string stage,
        string callbackName,
        string sourceState,
        Exception exception)
        => CallbackFaultedDelegate(logger, stage, callbackName, sourceState, exception);
}
