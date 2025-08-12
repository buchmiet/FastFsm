namespace Generator.Rules.Contexts;

/// <summary>
/// Context for FSM013 - Async callback in sync machine validation.
/// </summary>
public class AsyncCallbackInSyncMachineContext(
    string methodName,
    bool isCallbackAsync,
    bool isMachineAsync,
    bool isMachineEstablished)
{
    public string MethodName { get; } = methodName;
    public bool IsCallbackAsync { get; } = isCallbackAsync;
    public bool IsMachineAsync { get; } = isMachineAsync;
    public bool IsMachineEstablished { get; } = isMachineEstablished;
}