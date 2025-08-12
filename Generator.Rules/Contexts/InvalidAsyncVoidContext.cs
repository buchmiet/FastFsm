namespace Generator.Rules.Contexts;

/// <summary>
/// Context for FSM014 - Invalid async void validation.
/// </summary>
public class InvalidAsyncVoidContext(
    string methodName,
    string returnType,
    bool isAsync)
{
    public string MethodName { get; } = methodName;
    public string ReturnType { get; } = returnType;
    public bool IsAsync { get; } = isAsync;
}