

using System;

namespace Abstractions.Attributes;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class StateAttribute(object state) : Attribute
{
    public object State { get; } = state ?? throw new ArgumentNullException(nameof(state));
    public string OnEntry { get; set; }
    public string OnExit { get; set; }
}
