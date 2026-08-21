using FastFsm.Contracts;
using Tests.Machines.Machines;

namespace Tests.Machines.Extensions
{
    public class ThrowingExtension : IStateMachineExtension<BasicState, Trigger>
    {
        public void OnAttemptStarting(in TransitionAttemptContext<BasicState, Trigger> attempt)
        {
            throw new InvalidOperationException("This extension is designed to fail.");
        }
    }
    public class CountingExtension : IStateMachineExtension<BasicState, Trigger>
    {
        public int BeforeTransitionCount { get; private set; }
        public int AfterTransitionCount { get; private set; }

        public void OnAttemptStarting(in TransitionAttemptContext<BasicState, Trigger> attempt)
        {
            BeforeTransitionCount++;
        }

        public void OnAttemptCompleted(
            in TransitionAttemptContext<BasicState, Trigger> attempt,
            in TransitionResult<BasicState> result)
        {
            AfterTransitionCount++;
        }
    }
}
