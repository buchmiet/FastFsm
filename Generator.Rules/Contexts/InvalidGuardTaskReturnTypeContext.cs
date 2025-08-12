namespace Generator.Rules.Contexts;

/// <summary>
/// Context for FSM012 - Invalid async guard return type validation.
/// </summary>
public class InvalidGuardTaskReturnTypeContext(
    string methodName,
    string actualReturnType,
    bool isAsync)
{
    public string MethodName { get; } = methodName;
    public string ActualReturnType { get; } = actualReturnType;
    public bool IsAsync { get; } = isAsync;
}