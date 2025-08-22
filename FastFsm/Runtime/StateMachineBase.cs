using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Abstractions.Attributes;
using FastFsm.Contracts;

namespace FastFsm.Runtime;

/// <summary>
/// Base class providing common functionality for generated state machines
/// </summary>
public abstract class StateMachineBase<TState, TTrigger>(TState initialState) : IStateMachineSync<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    protected TState _currentState = initialState;
    private bool _started;

    // HSM: domyślnie „płasko” — puste tablice (HSM je nadpisze w klasie generowanej)
    protected virtual int[] ParentArray => Array.Empty<int>();
    protected virtual int[] DepthArray => Array.Empty<int>();
    protected virtual int[] InitialChildArray => Array.Empty<int>();
    protected virtual HistoryMode[] HistoryArray => Array.Empty<HistoryMode>();

    // Czy jakikolwiek stan złożony używa historii (HSM nadpisze na true, jeśli trzeba)
    protected virtual bool HasHistory => false;

    // Ostatnio aktywne dzieci dla stanów z historią (alokowane tylko gdy HasHistory == true)
    protected int[]? _lastActiveChild;

    public TState CurrentState => _currentState;
    public bool IsStarted => _started;

    public virtual void Start()
    {
        if (_started) return;
        _started = true;

        // Alokuj bufor historii tylko wtedy, gdy faktycznie jest potrzebny
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
    /// Gets the active state path from root to current leaf state
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
    /// Updates the last active child tracking when exiting a state
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    
    /// <summary>
    /// Returns the active path from the root composite down to the current leaf state.
    /// Helper to diagnose hierarchy.
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
}
