using FastFsm.Contracts;
using Tests.Machines.Machines;

namespace Tests.Machines.Extensions
{
    public class ResultCapturingExtension : IStateMachineExtension<ThrowingActionMachine_TestState, TestTrigger>
    {
        public List<bool> Results { get; } = [];

        public void OnAttemptCompleted(
            in TransitionAttemptContext<ThrowingActionMachine_TestState, TestTrigger> attempt,
            in TransitionResult<ThrowingActionMachine_TestState> result)
            => Results.Add(result.Outcome == TransitionOutcome.Succeeded);
    }
}
