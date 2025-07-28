using System;

namespace Abstractions.Attributes;

/// <summary>
/// Specifies the payload type for a state machine or specific triggers
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PayloadTypeAttribute : Attribute
{
    /// <summary>
    /// The default payload type for all triggers
    /// </summary>
    public Type DefaultPayloadType { get; set; }
    
    /// <summary>
    /// Specific trigger this payload type applies to
    /// </summary>
    public object Trigger { get; }
    
    /// <summary>
    /// The payload type for the specific trigger
    /// </summary>
    public Type PayloadType { get; }
    
    /// <summary>
    /// Constructor for default payload type
    /// </summary>
    public PayloadTypeAttribute(Type defaultPayloadType)
    {
        DefaultPayloadType = defaultPayloadType ?? throw new ArgumentNullException(nameof(defaultPayloadType));
    }
    
    /// <summary>
    /// Constructor for trigger-specific payload type
    /// </summary>
    public PayloadTypeAttribute(object trigger, Type payloadType)
    {
        Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        PayloadType = payloadType ?? throw new ArgumentNullException(nameof(payloadType));
    }
}
