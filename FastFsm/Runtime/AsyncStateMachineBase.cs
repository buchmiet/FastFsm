using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using FastFsm.Exceptions;

namespace FastFsm.Runtime
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
        private bool _started;

        // HSM: domyślnie „płasko” — puste tablice (HSM je nadpisze w klasie generowanej)
        protected virtual int[] ParentArray => Array.Empty<int>();
        protected virtual int[] DepthArray => Array.Empty<int>();
        protected virtual int[] InitialChildArray => Array.Empty<int>();
        protected virtual HistoryMode[] HistoryArray => Array.Empty<HistoryMode>();

        // Czy którakolwiek kompozycja używa historii (HSM nadpisze na true, jeśli trzeba)
        protected virtual bool HasHistory => false;

        // Ostatnio aktywne dzieci dla stanów z historią (alokowane tylko gdy HasHistory == true)
        protected int[]? _lastActiveChild;


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

                // Alokuj bufor historii tylko wtedy, gdy faktycznie potrzebny (HSM + jakiekolwiek composite)
                if (HasHistory)
                {
                    var initial = InitialChildArray;
                    if (initial.Length > 0)
                    {
                        _lastActiveChild = new int[initial.Length];
                        for (int i = 0; i < _lastActiveChild.Length; i++)
                            _lastActiveChild[i] = -1;
                    }
                }

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
            var parentArray = ParentArray;
            if (parentArray == null || parentArray.Length == 0)
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
            var parentIndex = parentArray[currentIndex];
            while (parentIndex >= 0)
            {
                if (parentIndex == targetIndex)
                    return true;
                parentIndex = parentArray[parentIndex];
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the active state path from root to current leaf state (synchronous version)
        /// </summary>
        public virtual IReadOnlyList<TState> GetActivePath()
        {
            // For non-hierarchical machines, return single element
            var parentArray = ParentArray;
            if (parentArray == null || parentArray.Length == 0)
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
                currentIndex = parentArray[currentIndex];
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
            var parentArray = ParentArray;
            if (parentArray == null || _lastActiveChild == null) return;
            
            var parentIndex = parentArray[childIndex];
            if (parentIndex >= 0)
            {
                _lastActiveChild[parentIndex] = childIndex;
            }
        }
        
        /// <summary>
        /// Resolves the actual leaf to enter for a composite state, using Initial/History semantics.
        /// </summary>
        protected int GetCompositeEntryTarget(int compositeIndex)
        {
            var historyArray = HistoryArray;
            var initialChildArray = InitialChildArray;
            var parentArray = ParentArray;
            
            int idx = compositeIndex;
            while (true)
            {
                // Check if this is a leaf (no initial child)
                if ((uint)idx >= (uint)initialChildArray.Length || initialChildArray[idx] < 0)
                    return idx;

                // Check for history
                var mode = historyArray[idx];
                int child = -1;

                if (mode != HistoryMode.None && _lastActiveChild != null && _lastActiveChild[idx] >= 0)
                {
                    int remembered = _lastActiveChild[idx];

                    if (mode == HistoryMode.Shallow)
                    {
                        // Map remembered LEAF up to the IMMEDIATE child of 'idx'
                        int immediate = remembered;
                        while (immediate >= 0 && parentArray[immediate] != idx)
                            immediate = parentArray[immediate];

                        // Fallback to initial if something went wrong
                        child = immediate >= 0 ? immediate : initialChildArray[idx];
                    }
                    else // Deep
                    {
                        child = remembered;
                    }
                }
                else
                {
                    child = initialChildArray[idx];
                }

                if (child < 0) return idx; // Safety check
                idx = child; // Descend
            }
        }
        
        /// <summary>
        /// Records the current leaf state in all ancestor composite states that have history enabled.
        /// </summary>
        protected void RecordHistoryForCurrentPath()
        {
            var parentArray = ParentArray;
            var historyArray = HistoryArray;
            
            if (_lastActiveChild == null || parentArray == null || historyArray == null) return;
            
            int leafLeaf = Convert.ToInt32(_currentState); // remember the deepest active leaf
            int cursor = leafLeaf;
            int parent = (uint)cursor < (uint)parentArray.Length ? parentArray[cursor] : -1;

            while (parent >= 0)
            {
                if (historyArray[parent] != HistoryMode.None)
                    _lastActiveChild[parent] = leafLeaf; // Always record the original leaf, not cursor

                cursor = parent;
                parent = parentArray[cursor];
            }
        }
        
        /// <summary>
        /// If CurrentState is composite, resolves and assigns the leaf according to Initial/History.
        /// </summary>
        protected void DescendToInitialIfComposite()
        {
            var initialChildArray = InitialChildArray;
            if (initialChildArray == null) return;
            
            int currentIdx = Convert.ToInt32(_currentState);
            if ((uint)currentIdx >= (uint)initialChildArray.Length) return;
            int initialChild = initialChildArray[currentIdx];
            if (initialChild < 0) return; // Already a leaf
            int resolved = GetCompositeEntryTarget(currentIdx);
            _currentState = (TState)Enum.ToObject(typeof(TState), resolved);
        }
        
        /// <summary>
        /// Returns true if the current state lies in the hierarchy of the given ancestor.
        /// </summary>
        /// <param name="ancestor">The potential ancestor state to check</param>
        /// <returns>True if ancestor is the current state or any of its parents, false otherwise</returns>
        public bool IsInHierarchy(TState ancestor)
        {
            const int NO_PARENT = -1;
            var parentArray = ParentArray;
            if (parentArray == null || parentArray.Length == 0) 
                return EqualityComparer<TState>.Default.Equals(_currentState, ancestor);
                
            int idx = Convert.ToInt32(_currentState);
            int ancIdx = Convert.ToInt32(ancestor);
            
            // Bounds check
            if ((uint)idx >= (uint)parentArray.Length) return false;
            if ((uint)ancIdx >= (uint)parentArray.Length) return false;
            
            // Check if ancestor is current state
            if (idx == ancIdx) return true;
            
            // Walk up parent chain
            while (true)
            {
                int parent = parentArray[idx];
                if (parent == NO_PARENT) return false;
                if (parent == ancIdx) return true;
                idx = parent;
            }
        }
        
#if DEBUG
        /// <summary>
        /// Returns the active path from the root composite down to the current leaf state.
        /// DEBUG-only helper to diagnose hierarchy.
        /// </summary>
        public string DumpActivePath()
        {
            const int NO_PARENT = -1;
            var parentArray = ParentArray;
            
            if (parentArray == null || parentArray.Length == 0)
                return _currentState.ToString();
            
            var sb = new System.Text.StringBuilder(64);
            var current = _currentState;
            int idx = Convert.ToInt32(current);
            
            // Seed with leaf
            sb.Insert(0, current.ToString());
            
            // Walk up to root
            while (true)
            {
                if ((uint)idx >= (uint)parentArray.Length) break;
                int parent = parentArray[idx];
                if (parent == NO_PARENT) break;
                
                // Cast parent index back to enum
                current = (TState)Enum.ToObject(typeof(TState), parent);
                sb.Insert(0, " / ");
                sb.Insert(0, current.ToString());
                idx = parent;
            }
            
            return sb.ToString();
        }
#endif
        
        #endregion
    }
}
