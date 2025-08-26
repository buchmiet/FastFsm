using Microsoft.Extensions.Logging;

namespace FastFsm.Runtime.Logging
{
    internal static class LogAdapter
    {
        public static void TransitionSucceeded(this ILogger logger, string instanceId, string fromState, string toState, string trigger)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.Log(LogLevel.Information, new EventId(1, nameof(TransitionSucceeded)),
                    "State machine {InstanceId} transitioned from {FromState} to {ToState} on trigger {Trigger}",
                    instanceId, fromState, toState, trigger);
        }

        public static void GuardFailed(this ILogger logger, string instanceId, string guardName, string fromState, string toState, string trigger)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.Log(LogLevel.Warning, new EventId(2, nameof(GuardFailed)),
                    "State machine {InstanceId} guard {GuardName} prevented transition from {FromState} to {ToState} on trigger {Trigger}",
                    instanceId, guardName, fromState, toState, trigger);
        }

        public static void TransitionFailed(this ILogger logger, string instanceId, string fromState, string trigger)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.Log(LogLevel.Warning, new EventId(3, nameof(TransitionFailed)),
                    "State machine {InstanceId} failed to transition from {FromState} on trigger {Trigger} - no valid transition found",
                    instanceId, fromState, trigger);
        }

        public static void OnEntryExecuted(this ILogger logger, string instanceId, string methodName, string state)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(4, nameof(OnEntryExecuted)),
                    "State machine {InstanceId} executed OnEntry {MethodName} for state {State}",
                    instanceId, methodName, state);
        }

        public static void OnExitExecuted(this ILogger logger, string instanceId, string methodName, string state)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(5, nameof(OnExitExecuted)),
                    "State machine {InstanceId} executed OnExit {MethodName} for state {State}",
                    instanceId, methodName, state);
        }

        public static void ActionExecuted(this ILogger logger, string instanceId, string actionName, string fromState, string toState, string trigger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(6, nameof(ActionExecuted)),
                    "State machine {InstanceId} executed action {ActionName} during transition from {FromState} to {ToState} on trigger {Trigger}",
                    instanceId, actionName, fromState, toState, trigger);
        }

        public static void PayloadValidationFailed(this ILogger logger, string instanceId, string trigger, string expectedType, string actualType)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.Log(LogLevel.Warning, new EventId(7, nameof(PayloadValidationFailed)),
                    "State machine {InstanceId} payload validation failed for trigger {Trigger} - expected {ExpectedType}, got {ActualType}",
                    instanceId, trigger, expectedType, actualType);
        }

        public static void InternalTransitionOnAncestor(this ILogger logger, string instanceId, string ancestorState, string currentState, string trigger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(10, nameof(InternalTransitionOnAncestor)),
                    "State machine {InstanceId} internal transition on ancestor {AncestorState} from state {CurrentState} on trigger {Trigger}",
                    instanceId, ancestorState, currentState, trigger);
        }

        public static void HierarchicalTransition(this ILogger logger, string instanceId, string fromState, string toState, string lcaState, int exitCount, int entryCount)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(11, nameof(HierarchicalTransition)),
                    "State machine {InstanceId} hierarchical transition from {FromState} to {ToState} via LCA {LcaState} - exiting {ExitCount} states, entering {EntryCount} states",
                    instanceId, fromState, toState, lcaState, exitCount, entryCount);
        }

        public static void CompositeStateEntry(this ILogger logger, string instanceId, string compositeState, string resolvedTarget, string resolutionMethod)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(12, nameof(CompositeStateEntry)),
                    "State machine {InstanceId} entering composite state {CompositeState}, resolved to {ResolvedTarget} using {ResolutionMethod}",
                    instanceId, compositeState, resolvedTarget, resolutionMethod);
        }

        public static void HistoryRestored(this ILogger logger, string instanceId, string compositeState, string restoredState, string historyType)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.Log(LogLevel.Debug, new EventId(13, nameof(HistoryRestored)),
                    "State machine {InstanceId} restored {HistoryType} history for composite {CompositeState} to state {RestoredState}",
                    instanceId, historyType, compositeState, restoredState);
        }

        public static void ActivePath(this ILogger logger, string instanceId, string path)
        {
            if (logger.IsEnabled(LogLevel.Trace))
                logger.Log(LogLevel.Trace, new EventId(14, nameof(ActivePath)),
                    "State machine {InstanceId} active path: {Path}",
                    instanceId, path);
        }
    }
}
