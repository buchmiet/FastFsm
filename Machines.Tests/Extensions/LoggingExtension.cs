using FastFsm.Contracts;

namespace Machines.Tests.Extensions;

public class LoggingExtension : IStateMachineExtension
{
    private readonly ITestOutputHelper _output;

    public LoggingExtension(ITestOutputHelper output)
    {
        _output = output;
    }

    public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
    {
        _output.WriteLine($"Extension: Before transition at {context.Timestamp}");
    }

    public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
    {
        _output.WriteLine($"Extension: After transition, success={success}");
    }

    public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
    {
        _output.WriteLine($"Extension: Evaluating guard '{guardName}'");
    }

    public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
    {
        _output.WriteLine($"Extension: Guard '{guardName}' returned {result}");
    }

    public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
    {
        _output.WriteLine("Extension: Unhandled trigger");
    }

    public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
    {
        _output.WriteLine("Extension: Internal transition");
    }
}