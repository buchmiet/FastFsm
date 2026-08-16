using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FastFsm.Contracts;
#if FSM_LOGGING_ENABLED
using Microsoft.Extensions.Logging;
#endif

namespace FastFsm.Runtime.Extensions
{
#if FSM_LOGGING_ENABLED
    internal static class ExtensionRunnerLog
{
    /// <summary>
    /// Logs an exception thrown by an extension.
    /// </summary>
    public static void ExtensionError(
        this ILogger logger,
        string extensionType,
        string methodName,
        string instanceId,
        string fromState,
        string trigger,
        string toState,
        Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(
                LogLevel.Error,
                new EventId(1001, nameof(ExtensionError)),
                exception,  // <- param 'exception'
                // ----------- zaktualizowany szablon -------------
                "Extension {ExtensionType} threw exception in {MethodName}. " +
                "ExceptionMessage={ExceptionMessage}. " +
                "InstanceId={InstanceId}, FromState={FromState}, Trigger={Trigger}, ToState={ToState}",
                // ---------------- parametry ---------------------
                extensionType,             // {ExtensionType}
                methodName,                // {MethodName}
                exception.Message,         // {ExceptionMessage}  <-- NOWE!
                instanceId,                // {InstanceId}
                fromState,                 // {FromState}
                trigger,                   // {Trigger}
                toState                    // {ToState}
            );
        }
    }
}
#endif

    /// <summary>
    /// Executes extension hooks and – when <c>FSM_LOGGING_ENABLED</c> is defined –
    /// logs errors to <see cref="ILogger"/>.
    /// </summary>
    internal sealed partial class ExtensionRunner
    {
        /// <summary>
        /// Common, logger-less instance for use where
        /// additional objects are not needed.
        /// </summary>
        public static ExtensionRunner Default { get; } = new();

#if FSM_LOGGING_ENABLED
        private readonly ILogger? _logger;

        public ExtensionRunner(ILogger? logger = null)
        {
            _logger = logger;
        }
#else
        public ExtensionRunner() { }
#endif

        private void SafeExecute<TContext>(
            IStateMachineExtension extension,
            TContext context,
            Action<IStateMachineExtension, TContext> action,
            string methodName)
            where TContext : IStateMachineContext
        {
            try
            {
                action(extension, context);
            }
            // Note: compilation-dependent catch header eliminating CS0168 when logging is disabled
#if FSM_LOGGING_ENABLED
            catch (Exception ex)
#else
            catch (Exception)
#endif
            {
#if FSM_LOGGING_ENABLED
                if (_logger?.IsEnabled(LogLevel.Error) == true && context is IStateSnapshot snap)
                {
                    _logger.ExtensionError(
                        extension.GetType().Name,
                        methodName,
                        context.InstanceId,
                        snap.FromState?.ToString() ?? "null",
                        snap.Trigger?.ToString() ?? "null",
                        snap.ToState?.ToString() ?? "null",
                        ex);
                }
#endif
                // Extension error should not interrupt state machine operation.
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnBeforeTransition"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunBeforeTransition<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    static (ext, ctx) => ext.OnBeforeTransition(ctx),
                    nameof(IStateMachineExtension.OnBeforeTransition));
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnAfterTransition"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunAfterTransition<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            bool success)
            where TContext : IStateMachineContext
        {
            // Heuristic: if transition succeeded and From == To, treat as internal
            if (success && context is IStateSnapshot snap && Equals(snap.FromState, snap.ToState))
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    SafeExecute(
                        extensions[i],
                        context,
                        (ext, ctx) => ext.OnInternalTransition(ctx),
                        nameof(IStateMachineExtension.OnInternalTransition));
                }
            }
            // OnTransitioned: successful transition, after effects
            if (success)
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    SafeExecute(
                        extensions[i],
                        context,
                        (ext, ctx) => ext.OnTransitioned(ctx),
                        nameof(IStateMachineExtension.OnTransitioned));
                }
            }
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnAfterTransition(ctx, success),
                    nameof(IStateMachineExtension.OnAfterTransition));
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnGuardEvaluation"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunGuardEvaluation<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            string guardName)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnGuardEvaluation(ctx, guardName),
                    nameof(IStateMachineExtension.OnGuardEvaluation));
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnGuardEvaluated"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunGuardEvaluated<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            string guardName,
            bool result)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnGuardEvaluated(ctx, guardName, result),
                    nameof(IStateMachineExtension.OnGuardEvaluated));
            }
        }

        // ------------------------------------------------------------
        // Experimental/Planned hooks – stub implementations
        // These are no-ops for now to allow generator call sites to compile.
        // Once the IStateMachineExtension interface is expanded, wire them
        // similarly to the existing Run* methods above.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUnhandledTrigger(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context)
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    static (ext, ctx) => ext.OnUnhandledTrigger(ctx),
                    nameof(IStateMachineExtension.OnUnhandledTrigger));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunInternalTransition(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context)
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnInternalTransition(ctx),
                    "OnInternalTransition");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunTransitioned(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context)
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    static (ext, ctx) => ext.OnTransitioned(ctx),
                    nameof(IStateMachineExtension.OnTransitioned));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunTransitionCompleted(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context)
        {
            // TODO: invoke extension.OnTransitionCompleted(context) when available
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunBubbleToParent<TState>(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context,
            TState handledAt)
            where TState : unmanaged, Enum
        {
            // TODO: invoke extension.OnBubbleToParent(context, handledAt) when available
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunInitialSubstateEntered<TState>(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext parentContext,
            TState child)
            where TState : unmanaged, Enum
        {
            // TODO: invoke extension.OnInitialSubstateEntered(parentContext, child) when available
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunHistoryRestore<TState>(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext parentContext,
            Abstractions.Attributes.HistoryMode mode,
            TState restoredState)
            where TState : unmanaged, Enum
        {
            // TODO: invoke extension.OnHistoryRestore(parentContext, mode, restoredState) when available
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunAncestorPathChanged<TState>(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context,
            ReadOnlySpan<TState> exitedPath,
            ReadOnlySpan<TState> enteredPath,
            TState lca)
            where TState : unmanaged, Enum
        {
            // TODO: invoke extension.OnAncestorPathChanged(context, exitedPath, enteredPath, lca) when available
        }
    }
}
