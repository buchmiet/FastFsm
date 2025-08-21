namespace FastFsm.Contracts;

/// <summary>
/// Provides a non-generic, reflection-free snapshot of a transition's key properties.
/// Used for safe logging and diagnostics.
/// </summary>
public interface IStateSnapshot
{
    /// <summary>
    /// The state the machine was in before the trigger.
    /// </summary>
    object FromState { get; }

    /// <summary>
    /// The trigger that caused the transition attempt.
    /// </summary>
    object Trigger { get; }

    /// <summary>
    /// The destination state of the transition.
    /// </summary>
    object ToState { get; }
}