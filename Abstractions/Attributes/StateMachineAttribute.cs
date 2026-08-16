using System;

namespace Abstractions.Attributes;

/// <summary>
/// Marks a class for state-machine source generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateMachineAttribute : Attribute
{
    public Type StateType { get; }
    public Type TriggerType { get; }

    /// <summary>
    /// Controls whether the generated machine includes <c>IStateMachineExtension</c> support.
    /// </summary>
    public bool GenerateExtensibleVersion { get; set; } = true;

    /// <summary>
    /// Gets or sets the default payload type for triggers that do not declare a trigger-specific payload type.
    /// </summary>
    public Type DefaultPayloadType { get; set; }

    /// <summary>
    /// Controls whether structural query methods such as <c>HasTransition</c> and <c>GetDefinedTriggers</c> are generated.
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
    /// Controls whether asynchronous continuations capture the current synchronization context.
    /// Applies to asynchronous state machines. The default is <see langword="false"/>.
    /// </summary>
    public bool ContinueOnCapturedContext { get; set; } = false;

    /// <summary>
    /// Controls whether hierarchical state-machine support is enabled.
    /// The generator can also enable hierarchy when hierarchical metadata is present.
    /// </summary>
    public bool EnableHierarchy { get; set; } = false;
}
