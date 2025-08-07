

using System;

namespace Abstractions.Attributes;

/// <summary>
/// Marks a class as a fast state machine that should be generated
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateMachineAttribute : Attribute
{
    public Type StateType { get; }
    public Type TriggerType { get; }
    
    /// <summary>
    /// Controls whether to generate extensible variant
    /// </summary>
    public bool GenerateExtensibleVersion { get; set; } = true;
    
    /// <summary>
    /// Default payload type for all triggers (optional)
    /// </summary>
    public Type DefaultPayloadType { get; set; }
    /// <summary>
    /// When true, generates additional methods for structural analysis of the state machine (HasTransition, GetDefinedTriggers)
    /// </summary>
    public bool GenerateStructuralApi { get; set; } = false;
    public StateMachineAttribute(Type stateType, Type triggerType)
    {
        if (!stateType.IsEnum || !triggerType.IsEnum)
            throw new ArgumentException("State and Trigger types must be enums");
            
        StateType = stateType;
        TriggerType = triggerType;
    }
    /// <summary>
    /// Controls whether async continuations should be posted back to the original context.
    /// Only applicable for async state machines. Defaults to false for better performance.
    /// </summary>
    public bool ContinueOnCapturedContext { get; set; } = false;
    
    /// <summary>
    /// Enables hierarchical state machine features (composite states, history, etc.)
    /// Automatically enabled if any HSM attributes are used
    /// </summary>
    public bool EnableHierarchy { get; set; } = false;
}

