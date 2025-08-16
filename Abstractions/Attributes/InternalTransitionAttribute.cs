

using System;

namespace Abstractions.Attributes;

/// <summary>
/// Defines an internal transition (no state change)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InternalTransitionAttribute : Attribute
{
    public InternalTransitionAttribute(object state, object trigger)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
    }

    // Backward-compatible overload: allows [InternalTransition(State, Trigger, nameof(Action))]
    public InternalTransitionAttribute(object state, object trigger, string action)
        : this(state, trigger)
    {
        Action = action;
    }

    public object State { get; }
    public object Trigger { get; }
    public string Guard { get; set; }
    public string Action { get; set; }
    public int Priority { get; set; } = 0;
}
