namespace Generator.Rules.Contexts;

/// <summary>
/// Context describing an invalid HSM hierarchy configuration for a given state.
/// </summary>
public class InvalidHierarchyConfigurationContext
{
    /// <summary>
    /// Gets the name of the composite state.
    /// </summary>
    public string CompositeStateName { get; }

    /// <summary>
    /// Gets a value indicating whether the state is composite.
    /// </summary>
    public bool IsComposite { get; }

    /// <summary>
    /// Gets a value indicating whether the composite state has an initial substate.
    /// </summary>
    public bool HasInitialSubstate { get; }

    /// <summary>
    /// Gets a value indicating whether the composite state has history.
    /// </summary>
    public bool HasHistory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidHierarchyConfigurationContext"/> class.
    /// </summary>
    /// <param name="compositeStateName">The name of the composite state.</param>
    /// <param name="isComposite">Indicates whether the state is composite.</param>
    /// <param name="hasInitialSubstate">Indicates whether the composite state has an initial substate.</param>
    /// <param name="hasHistory">Indicates whether the composite state has history.</param>
    public InvalidHierarchyConfigurationContext(string compositeStateName, bool isComposite, bool hasInitialSubstate, bool hasHistory)
    {
        CompositeStateName = compositeStateName;
        IsComposite = isComposite;
        HasInitialSubstate = hasInitialSubstate;
        HasHistory = hasHistory;
    }
}