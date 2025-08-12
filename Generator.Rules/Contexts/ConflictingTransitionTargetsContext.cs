namespace Generator.Rules.Contexts;

/// <summary>
/// Context for FSM105 - Conflicting transition targets validation.
/// </summary>
public class ConflictingTransitionTargetsContext(
    string compositeStateName,
    string actualTarget,
    string reason)
{
    public string CompositeStateName { get; } = compositeStateName;
    public string ActualTarget { get; } = actualTarget;
    public string Reason { get; } = reason; // "history", "initial", or "first-child"
}