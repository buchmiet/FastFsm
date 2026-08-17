using System;
using Microsoft.Extensions.Logging;

namespace FastFsm.Logging
{
    internal static class FsmLifecycleLog
    {
        // Reserved EventId range for lifecycle/diagnostics (1100-1199)
        private static readonly EventId UnhandledTriggerId        = new(1100, nameof(UnhandledTrigger));
        private static readonly EventId MachineStartedId          = new(1101, nameof(MachineStarted));
        private static readonly EventId MachineStoppedId          = new(1102, nameof(MachineStopped));
        private static readonly EventId TransitionStartedId       = new(1103, nameof(TransitionStarted));
        private static readonly EventId AsyncActionStartedId      = new(1104, nameof(AsyncActionStarted));
        private static readonly EventId AsyncActionCompletedId    = new(1105, nameof(AsyncActionCompleted));
        private static readonly EventId AsyncActionFailedId       = new(1106, nameof(AsyncActionFailed));
        private static readonly EventId CallbackExceptionId       = new(1107, nameof(CallbackException));

        // ----------- LoggerMessage delegates (zero-alloc) -----------

        private static readonly Action<ILogger, string, string, Exception?> _unhandledTrigger =
            LoggerMessage.Define<string, string>(
                LogLevel.Warning, UnhandledTriggerId,
                "Unhandled trigger: {Trigger} in state {State}");

        private static readonly Action<ILogger, string, Exception?> _machineStarted =
            LoggerMessage.Define<string>(
                LogLevel.Information, MachineStartedId,
                "State machine started at {InitialState}");

        private static readonly Action<ILogger, string, Exception?> _machineStopped =
            LoggerMessage.Define<string>(
                LogLevel.Information, MachineStoppedId,
                "State machine stopped at {FinalState}");

        private static readonly Action<ILogger, string, string, string, Exception?> _transitionStarted =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Debug, TransitionStartedId,
                "Transition started: {FromState} --({Trigger})--> {ToState}");

        private static readonly Action<ILogger, string, string, Exception?> _asyncActionStarted =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug, AsyncActionStartedId,
                "Async action started: {ActionName} (on {Context})");

        private static readonly Action<ILogger, string, string, double, Exception?> _asyncActionCompleted =
            LoggerMessage.Define<string, string, double>(
                LogLevel.Debug, AsyncActionCompletedId,
                "Async action completed: {ActionName} (on {Context}) in {ElapsedMs}ms");

        private static readonly Action<ILogger, string, string, string, Exception?> _asyncActionFailed =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Warning, AsyncActionFailedId,
                "Async action failed: {ActionName} (on {Context}) error: {ErrorType}");

        // Unified event for exceptions in callbacks (OnEntry/OnExit/Action/Guard)
        private static readonly Action<ILogger, string, string, string, Exception?> _callbackException =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Warning, CallbackExceptionId,
                "{CallbackKind} threw: {CallbackName} (on {Context})");

        // ----------- Public wrappers -----------

        [System.Diagnostics.Conditional("TRACE")]
        public static void UnhandledTrigger(ILogger logger, string state, string trigger)
            => _unhandledTrigger(logger, trigger, state, null);

        public static void MachineStarted(ILogger logger, string initialState)
            => _machineStarted(logger, initialState, null);

        public static void MachineStopped(ILogger logger, string finalState)
            => _machineStopped(logger, finalState, null);

        public static void TransitionStarted(ILogger logger, string fromState, string trigger, string toState)
            => _transitionStarted(logger, fromState, trigger, toState, null);

        public static void AsyncActionStarted(ILogger logger, string actionName, string context)
            => _asyncActionStarted(logger, actionName, context, null);

        public static void AsyncActionCompleted(ILogger logger, string actionName, string context, double elapsedMs)
            => _asyncActionCompleted(logger, actionName, context, elapsedMs, null);

        public static void AsyncActionFailed(ILogger logger, string actionName, string context, Exception ex)
            => _asyncActionFailed(logger, actionName, context, ex.GetType().Name, ex);

        // kind: "OnEntry" | "OnExit" | "Action" | "Guard"
        public static void CallbackException(ILogger logger, string kind, string callbackName, string context, Exception ex)
            => _callbackException(logger, kind, callbackName, context, ex);
    }
}