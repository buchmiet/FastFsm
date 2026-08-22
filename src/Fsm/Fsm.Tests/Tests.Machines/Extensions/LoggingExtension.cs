using FastFsm.Contracts;
using Tests.Machines.Machines;
using Xunit.Abstractions;

namespace Tests.Machines.Extensions;

public class LoggingExtension : IStateMachineExtension<ExtState, ExtTrigger>
{
    private readonly ITestOutputHelper _output;

    public LoggingExtension(ITestOutputHelper output)
    {
        _output = output;
    }

    public ExtensionHooks Hooks => ExtensionHooks.All;

    public void OnAttemptStarting(in TransitionAttemptContext<ExtState, ExtTrigger> attempt) => _output.WriteLine($"Extension: Before transition attempt {attempt.AttemptId}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
        in TransitionResult<ExtState> result) => _output.WriteLine($"Extension: After transition, outcome={result.Outcome}");

    public void OnGuardEvaluating(
        in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
        in TransitionInfo<ExtState> candidate,
        string guardName) => _output.WriteLine($"Extension: Evaluating guard '{guardName}'");

    public void OnGuardEvaluated(
        in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
        in TransitionInfo<ExtState> candidate,
        string guardName,
        bool result) => _output.WriteLine($"Extension: Guard '{guardName}' returned {result}");

}