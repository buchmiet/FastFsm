namespace Generator.Model;

/// <summary>
/// Represents an exception handler method for the state machine.
/// </summary>
public sealed class ExceptionHandlerModel
{
    /// <summary>
    /// The name of the exception handler method.
    /// </summary>
    public string MethodName { get; set; } = default!;
    
    /// <summary>
    /// Whether the handler is async (returns ValueTask<ExceptionDirective>).
    /// </summary>
    public bool IsAsync { get; set; }
    
    /// <summary>
    /// Whether the handler accepts a CancellationToken as second parameter.
    /// </summary>
    public bool AcceptsCancellationToken { get; set; }
    
    /// <summary>
    /// The fully qualified closed generic type for ExceptionContext.
    /// Example: "global::StateMachine.Exceptions.ExceptionContext<MyStates, MyTriggers>"
    /// </summary>
    public string ExceptionContextClosedType { get; set; } = default!;
}