namespace Generator.Model;

/// <summary>
/// Represents the generation variant for a state machine
/// </summary>
public enum GenerationVariant
{
    /// <summary>
    /// Minimal implementation with no features
    /// </summary>
    Pure,
    
    /// <summary>
    /// Includes OnEntry/OnExit support
    /// </summary>
    Basic,
    
    /// <summary>
    /// Includes typed payload support
    /// </summary>
    WithPayload,
    
    /// <summary>
    /// Includes extension support
    /// </summary>
    WithExtensions,
    
    /// <summary>
    /// All features enabled
    /// </summary>
    Full
}