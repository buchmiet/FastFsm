

using System;

namespace Abstractions.Attributes;

/// <summary>
/// Defines an internal transition (no state change)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InternalTransitionAttribute(object state, object trigger, string action) : Attribute
{
    public object State { get; } = state ?? throw new ArgumentNullException(nameof(state));
    public object Trigger { get; } = trigger ?? throw new ArgumentNullException(nameof(trigger));
    public string Guard { get; set; }
    public string Action { get; set; } = action ?? throw new ArgumentNullException(nameof(action));
    public int Priority { get; set; } = 0;
}
