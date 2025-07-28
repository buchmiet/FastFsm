
using System;

namespace Abstractions.Attributes;

/// <summary>
/// Defines a state transition
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TransitionAttribute(object fromState, object trigger, object toState) : Attribute
{
    public object FromState { get; } = fromState ?? throw new ArgumentNullException(nameof(fromState));
    public object Trigger { get; } = trigger ?? throw new ArgumentNullException(nameof(trigger));
    public object ToState { get; } = toState ?? throw new ArgumentNullException(nameof(toState));
    public string Guard { get; set; }
    public string Action { get; set; }
}
