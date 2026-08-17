namespace Generator.Rules.Definitions;

public class TransitionDefinition(string fromState, string trigger, string? toState = null)
{
    public string FromState { get; } = fromState;
    public string Trigger { get; } = trigger;
    public string? ToState { get; } = toState;

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// For TransitionDefinition, equality for use in sets (like for duplicate detection)
    /// is based on FromState and Trigger only. ToState is not considered.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not TransitionDefinition other)
            return false;

        // For the purpose of duplicate transition detection (FSM001),
        // only FromState and Trigger are considered. ToState can differ.
        return FromState == other.FromState && Trigger == other.Trigger;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// The hash code is based on FromState and Trigger only, to align with Equals behavior.
    /// </summary>
    public override int GetHashCode()
    {
        return (FromState, Trigger).GetHashCode();
    }


    public bool EqualsIncludingToState(TransitionDefinition? other)
    {
        if (other is null)
            return false;

        return FromState == other.FromState &&
               Trigger == other.Trigger &&
               ToState == other.ToState;
    }
}