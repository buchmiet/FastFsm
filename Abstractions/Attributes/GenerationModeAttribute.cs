using System;

namespace Abstractions.Attributes;

/// <summary>
/// Controls the generation mode for the state machine
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerationModeAttribute(GenerationMode mode) : Attribute
{
    /// <summary>
    /// The generation mode to use
    /// </summary>
    public GenerationMode Mode { get; } = mode;

    /// <summary>
    /// Force specific variant regardless of usage analysis
    /// </summary>
    public bool Force { get; set; }
}

public enum GenerationMode
{
    /// <summary>
    /// Automatically detect based on usage
    /// </summary>
    Auto = 0,
    
    /// <summary>
    /// Generate only the pure variant (no features)
    /// </summary>
    Pure = 1,
    
    /// <summary>
    /// Generate with OnEntry/OnExit support
    /// </summary>
    Basic = 2,
    
    /// <summary>
    /// Generate with typed payload support
    /// </summary>
    WithPayload = 3,
    
    /// <summary>
    /// Generate with extension support
    /// </summary>
    WithExtensions = 4,
    
    /// <summary>
    /// Generate with all features
    /// </summary>
    Full = 5
}
