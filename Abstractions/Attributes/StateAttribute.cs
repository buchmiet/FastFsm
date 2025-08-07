

using System;

namespace Abstractions.Attributes;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class StateAttribute(object state) : Attribute
{
    public object State { get; } = state ?? throw new ArgumentNullException(nameof(state));
    public string OnEntry { get; set; }
    public string OnExit { get; set; }
    
    // HSM support - hierarchical states
    /// <summary>
    /// Optional parent state for hierarchical state machines
    /// </summary>
    public object Parent { get; set; }
    
    /// <summary>
    /// History mode for composite states (None by default)
    /// </summary>
    public HistoryMode History { get; set; } = HistoryMode.None;
    
    /// <summary>
    /// Marks this state as the initial substate of its parent
    /// </summary>
    public bool IsInitial { get; set; } = false;
}
