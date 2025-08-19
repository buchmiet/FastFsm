

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Abstractions.Attributes;
using StateMachine.Contracts;


namespace StateMachine.Runtime;

/// <summary>
/// Base class providing common functionality for generated state machines
/// </summary>
public abstract class StateMachineBase<TState, TTrigger>(TState initialState) : IStateMachineSync<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    protected TState _currentState = initialState;
    private bool _started = false;
    
    // Hierarchical state machine support fields (populated by generator)
    protected static int[]? s_parent;  // Parent index for each state (-1 for root)
    protected static int[]? s_depth;   // Depth in hierarchy for each state
    protected static int[]? s_initialChild;  // Initial child state index for composites (-1 if not composite)
    protected static HistoryMode[]? s_history;  // History mode for each state
    protected int[]? _lastActiveChild;  // Last active child for each composite state (instance-specific)
    
    public TState CurrentState => _currentState;
    
    public bool IsStarted => _started;

    public virtual void Start()
    {
        if (_started) return;
        _started = true;
        OnInitialEntry();
    }
    
    protected virtual void OnInitialEntry()
    {
        // Override in generated code to dispatch initial OnEntry
    }
    
    protected void EnsureStarted()
    {
        if (!_started)
            throw new InvalidOperationException(
                $"{GetType().Name} is not started. Call Start() before using the state machine.");
    }
    
    public virtual bool TryFire(TTrigger trigger, object? payload = null)
    {
        EnsureStarted();
        return TryFireInternal(trigger, payload);
    }
    
    protected abstract bool TryFireInternal(TTrigger trigger, object? payload = null);
    
    public virtual void Fire(TTrigger trigger, object? payload = null)
    {
        EnsureStarted();
        if (!TryFireInternal(trigger, payload))
        {
            throw new InvalidOperationException(
                $"No transition from state '{_currentState}' on trigger '{trigger}'");
        }
    }
    
    public bool CanFire(TTrigger trigger)
    {
        EnsureStarted();
        return CanFireInternal(trigger);
    }
    
    protected abstract bool CanFireInternal(TTrigger trigger);
    
    public IReadOnlyList<TTrigger> GetPermittedTriggers()
    {
        EnsureStarted();
        return GetPermittedTriggersInternal();
    }
    
    protected abstract IReadOnlyList<TTrigger> GetPermittedTriggersInternal();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool SetState(TState newState)
    {
        _currentState = newState;
        return true;
    }
    
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
    /// Gets the active state path from root to current leaf state
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
    /// Updates the last active child tracking when exiting a state
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}
