using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StateMachine.Contracts;

#if FSM_LOGGING_ENABLED
using Microsoft.Extensions.Logging;
#endif

namespace StateMachine.Runtime.Extensions
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
    /// zapisuje błędy do <see cref="ILogger"/>.
    /// </summary>
    public sealed partial class ExtensionRunner
    {
        /// <summary>
        /// Wspólna, bez-loggerowa instancja do użycia tam,
        /// gdzie dodatkowe obiekty nie są potrzebne.
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
            catch (Exception ex)
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
                // Błąd w rozszerzeniu nie powinien przerwać działania maszyny.
            }
        }

        /// <summary>
        /// Wywołuje <see cref="IStateMachineExtension.OnBeforeTransition"/> dla wszystkich rozszerzeń.
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
        /// Wywołuje <see cref="IStateMachineExtension.OnAfterTransition"/> dla wszystkich rozszerzeń.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunAfterTransition<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            bool success)
            where TContext : IStateMachineContext
        {
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
        /// Wywołuje <see cref="IStateMachineExtension.OnGuardEvaluation"/> dla wszystkich rozszerzeń.
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
        /// Wywołuje <see cref="IStateMachineExtension.OnGuardEvaluated"/> dla wszystkich rozszerzeń.
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
    }
}
