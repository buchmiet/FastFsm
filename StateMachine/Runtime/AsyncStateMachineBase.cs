using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using StateMachine.Contracts;
using StateMachine.Exceptions;

namespace StateMachine.Runtime
{
    /// <summary>
    /// Provides the base implementation for generated asynchronous state machines,
    /// handling thread-safety and providing a clear async-only API.
    /// </summary>
    public abstract class AsyncStateMachineBase<TState, TTrigger> : IStateMachineAsync<TState, TTrigger>
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        // Serializes all transition attempts (TryFireAsync/FireAsync).
        private readonly SemaphoreSlim _gate = new(1, 1);

        protected readonly bool _continueOnCapturedContext;
        protected TState _currentState;
        private bool _started = false;
        
        // Hierarchical state machine support fields (populated by generator)
        protected static int[]? s_parent;  // Parent index for each state (-1 for root)
        protected static int[]? s_depth;   // Depth in hierarchy for each state
        protected static int[]? s_initialChild;  // Initial child state index for composites (-1 if not composite)
        protected static HistoryMode[]? s_history;  // History mode for each state
        protected int[]? _lastActiveChild;  // Last active child for each composite state (instance-specific)

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
        
        public bool IsStarted => _started;

        #region Blocked Synchronous API

        private const string ErrorMessage =
            "State machine '{0}' is configured for async operation. Use the '{1}Async' method instead.";

        // Sync Start() removed - async machines only support StartAsync()
        
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
        
        public virtual async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (_started) return;
            
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(_continueOnCapturedContext);
            try
            {
                if (_started) return; // Double-check after acquiring lock
                _started = true;
                await OnInitialEntryAsync(cancellationToken).ConfigureAwait(_continueOnCapturedContext);
            }
            finally
            {
                _gate.Release();
            }
        }
        
        protected virtual ValueTask OnInitialEntryAsync(CancellationToken cancellationToken = default)
        {
            // Override in generated code to dispatch initial OnEntry
            return ValueTask.CompletedTask;
        }
        
        protected void EnsureStarted()
        {
            if (!_started)
                throw new InvalidOperationException(
                    $"{GetType().Name} is not started. Await StartAsync(...) first.");
        }

        /// <summary>
        /// Asynchronously tries to fire a trigger. Returns true if a transition occurred.
        /// This operation is thread-safe (serialized).
        /// </summary>
        public async ValueTask<bool> TryFireAsync(
            TTrigger trigger,
            object? payload = null,
            CancellationToken cancellationToken = default)
        {
            EnsureStarted();
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
        public async ValueTask<bool> CanFireAsync(
            TTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            EnsureStarted();
            return await CanFireInternalAsync(trigger, cancellationToken).ConfigureAwait(_continueOnCapturedContext);
        }
        
        protected abstract ValueTask<bool> CanFireInternalAsync(
            TTrigger trigger,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all triggers that can be fired from the current state,
        /// including evaluation of asynchronous guards.
        /// </summary>
        public async ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(
            CancellationToken cancellationToken = default)
        {
            EnsureStarted();
            return await GetPermittedTriggersInternalAsync(cancellationToken).ConfigureAwait(_continueOnCapturedContext);
        }
        
        protected abstract ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersInternalAsync(
            CancellationToken cancellationToken = default);

        #endregion

        /// <summary>
        /// Implemented in generated classes; contains the core logic for an asynchronous transition.
        /// </summary>
        protected abstract ValueTask<bool> TryFireInternalAsync(
            TTrigger trigger,
            object? payload,
            CancellationToken cancellationToken);
            
        #region Hierarchical State Machine Support
        
        /// <summary>
        /// Checks if the given state is in the active path
        /// </summary>
        public virtual bool IsIn(TState state)
        {
            // For non-hierarchical machines, just check equality
            if (s_parent == null || s_parent.Length == 0)
            {
                return EqualityComparer<TState>.Default.Equals(_currentState, state);
            }
            
            // For hierarchical machines, walk up the parent chain
            var currentIndex = Convert.ToInt32(_currentState);
            var targetIndex = Convert.ToInt32(state);
            
            // If checking current state
            if (currentIndex == targetIndex)
                return true;
            
            // Walk up the parent chain from current state
            var parentIndex = s_parent[currentIndex];
            while (parentIndex >= 0)
            {
                if (parentIndex == targetIndex)
                    return true;
                parentIndex = s_parent[parentIndex];
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the active state path from root to current leaf state (synchronous version)
        /// </summary>
        public virtual IReadOnlyList<TState> GetActivePath()
        {
            // For non-hierarchical machines, return single element
            if (s_parent == null || s_parent.Length == 0)
            {
                return new[] { _currentState };
            }
            
            // For hierarchical machines, build the path from leaf to root, then reverse
            var path = new List<TState>();
            var currentIndex = Convert.ToInt32(_currentState);
            
            // Add current state and walk up to root
            while (currentIndex >= 0)
            {
                path.Add((TState)Enum.ToObject(typeof(TState), currentIndex));
                currentIndex = s_parent[currentIndex];
            }
            
            // Reverse to get root-to-leaf order
            path.Reverse();
            return path;
        }
        
        /// <summary>
        /// Asynchronously gets the active state path from root to current leaf state
        /// </summary>
        public virtual ValueTask<IReadOnlyList<TState>> GetActivePathAsync(CancellationToken cancellationToken = default)
        {
            // For now, just return the synchronous result wrapped in a ValueTask
            // The generator can override this if needed for thread-safety
            return new ValueTask<IReadOnlyList<TState>>(GetActivePath());
        }
        
        /// <summary>
        /// Updates the last active child tracking when exiting a state
        /// </summary>
        protected void UpdateLastActiveChild(int childIndex)
        {
            if (s_parent == null || _lastActiveChild == null) return;
            
            var parentIndex = s_parent[childIndex];
            if (parentIndex >= 0)
            {
                _lastActiveChild[parentIndex] = childIndex;
            }
        }
        
        /// <summary>
        /// Gets the target state when entering a composite, considering history
        /// </summary>
        protected int GetCompositeEntryTarget(int compositeIndex)
        {
            if (s_history == null || s_initialChild == null || _lastActiveChild == null)
                return compositeIndex;
            
            var historyMode = s_history[compositeIndex];
            
            // Check for history
            if (historyMode != HistoryMode.None && _lastActiveChild[compositeIndex] >= 0)
            {
                var lastChild = _lastActiveChild[compositeIndex];
                
                if (historyMode == HistoryMode.Shallow)
                {
                    return lastChild;
                }
                else if (historyMode == HistoryMode.Deep)
                {
                    // For deep history, recursively find the deepest last active state
                    return GetDeepHistoryTarget(lastChild);
                }
            }
            
            // No history or first entry - use initial child
            var initial = s_initialChild[compositeIndex];
            return initial >= 0 ? initial : compositeIndex;
        }
        
        /// <summary>
        /// Recursively finds the deepest historical state for deep history mode
        /// </summary>
        private int GetDeepHistoryTarget(int stateIndex)
        {
            if (_lastActiveChild == null || s_initialChild == null)
                return stateIndex;
                
            // If this state has a last active child, recurse
            if (_lastActiveChild[stateIndex] >= 0)
            {
                return GetDeepHistoryTarget(_lastActiveChild[stateIndex]);
            }
            
            // Otherwise check if it has an initial child
            var initial = s_initialChild[stateIndex];
            return initial >= 0 ? initial : stateIndex;
        }
        
        #endregion
    }
}
