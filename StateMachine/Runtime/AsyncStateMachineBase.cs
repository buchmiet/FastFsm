using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StateMachine.Contracts;
using StateMachine.Exceptions;

namespace StateMachine.Runtime
{
    /// <summary>
    /// Provides the base implementation for generated asynchronous state machines,
    /// handling thread-safety and providing a clear async-only API.
    /// </summary>
    public abstract class AsyncStateMachineBase<TState, TTrigger> : IAsyncStateMachine<TState, TTrigger>
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        // Serializes all transition attempts (TryFireAsync/FireAsync).
        private readonly SemaphoreSlim _gate = new(1, 1);

        protected readonly bool _continueOnCapturedContext;
        protected TState _currentState;

        /// <summary>
        /// Initializes a new instance of the asynchronous state machine base class.
        /// </summary>
        /// <param name="initialState">The initial state of the machine.</param>
        /// <param name="continueOnCapturedContext">
        /// If true, async continuations will be posted back to the original context.
        /// Defaults to false for better performance in server-side scenarios.
        /// </param>
        protected AsyncStateMachineBase(TState initialState, bool continueOnCapturedContext = false)
        {
            _currentState = initialState;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>
        /// Current state read. For most scenarios non-blocking read is fine because
        /// transitions are serialized; if you need stronger publication semantics,
        /// you can switch to a volatile read strategy (requires controlling writes too).
        /// </summary>
        public TState CurrentState => _currentState;

        #region Blocked Synchronous API

        private const string ErrorMessage =
            "State machine '{0}' is configured for async operation. Use the '{1}Async' method instead.";

        /// <summary>
        /// Not supported in async machines. Use <see cref="TryFireAsync"/> instead.
        /// </summary>
        public virtual bool TryFire(TTrigger trigger, object? payload = null) =>
            throw new SyncCallOnAsyncMachineException(string.Format(ErrorMessage, GetType().Name, nameof(TryFire)));

        /// <summary>
        /// Not supported in async machines. Use <see cref="FireAsync"/> instead.
        /// </summary>
        public virtual void Fire(TTrigger trigger, object? payload = null) =>
            throw new SyncCallOnAsyncMachineException(string.Format(ErrorMessage, GetType().Name, nameof(Fire)));

        /// <summary>
        /// Not supported in async machines. Use <see cref="CanFireAsync"/> instead.
        /// </summary>
        public virtual bool CanFire(TTrigger trigger) =>
            throw new SyncCallOnAsyncMachineException(string.Format(ErrorMessage, GetType().Name, nameof(CanFire)));

        /// <summary>
        /// Not supported in async machines. Use <see cref="GetPermittedTriggersAsync"/> instead.
        /// </summary>
        public virtual IReadOnlyList<TTrigger> GetPermittedTriggers() =>
            throw new SyncCallOnAsyncMachineException(string.Format(ErrorMessage, GetType().Name, nameof(GetPermittedTriggers)));

        #endregion

        #region Public Asynchronous API

        /// <summary>
        /// Asynchronously tries to fire a trigger. Returns true if a transition occurred.
        /// This operation is thread-safe (serialized).
        /// </summary>
        public async ValueTask<bool> TryFireAsync(
            TTrigger trigger,
            object? payload = null,
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(_continueOnCapturedContext);
            try
            {
                return await TryFireInternalAsync(trigger, payload, cancellationToken)
                    .ConfigureAwait(_continueOnCapturedContext);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Asynchronously fires a trigger. Throws an <see cref="InvalidOperationException"/>
        /// if the transition is not valid. This operation is thread-safe (serialized).
        /// </summary>
        public async ValueTask FireAsync(
            TTrigger trigger,
            object? payload = null,
            CancellationToken cancellationToken = default)
        {
            if (!await TryFireAsync(trigger, payload, cancellationToken).ConfigureAwait(_continueOnCapturedContext))
            {
                throw new InvalidOperationException(
                    $"No valid transition from state '{CurrentState}' on trigger '{trigger}'.");
            }
        }

        /// <summary>
        /// Asynchronously checks if a trigger can be fired from the current state,
        /// including evaluation of asynchronous guards.
        /// </summary>
        public abstract ValueTask<bool> CanFireAsync(
            TTrigger trigger,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all triggers that can be fired from the current state,
        /// including evaluation of asynchronous guards.
        /// </summary>
        public abstract ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(
            CancellationToken cancellationToken = default);

        #endregion

        /// <summary>
        /// Implemented in generated classes; contains the core logic for an asynchronous transition.
        /// </summary>
        protected abstract ValueTask<bool> TryFireInternalAsync(
            TTrigger trigger,
            object? payload,
            CancellationToken cancellationToken);
    }
}
