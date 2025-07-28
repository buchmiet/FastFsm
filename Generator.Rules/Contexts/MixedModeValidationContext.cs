namespace Generator.Rules.Contexts;

/// <summary>
/// Context for validating that a state machine does not mix synchronous and asynchronous callbacks.
/// </summary>
public class MixedModeValidationContext(
    string methodName,
    string callbackMode,
    string machineMode)
{
    /// <summary>
    /// The name of the method that caused the conflict.
    /// </summary>
    public string MethodName { get; } = methodName;

    /// <summary>
    /// The mode of the conflicting callback (e.g., "asynchronous").
    /// </summary>
    public string CallbackMode { get; } = callbackMode;

    /// <summary>
    /// The established mode of the state machine (e.g., "synchronous").
    /// </summary>
    public string MachineMode { get; } = machineMode;
}