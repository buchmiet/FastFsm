using FastFsm.Contracts;
using Tests.Machines.Machines;

namespace Tests.Machines.Extensions
{
    /// <summary>
    /// Test extension that can throw exceptions
    /// </summary>
    public class TestExtension : IStateMachineExtension<ExtState, ExtTrigger>
    {
        public bool ThrowOnBeforeTransition { get; set; }
        public bool ThrowOnAfterTransition { get; set; }
        public bool ThrowOnGuardEvaluation { get; set; }
        public bool ThrowOnGuardEvaluated { get; set; }

        public Action<TransitionAttemptContext<ExtState, ExtTrigger>>? BeforeTransitionCallback { get; set; }
        public Action<TransitionAttemptContext<ExtState, ExtTrigger>, bool>? AfterTransitionCallback { get; set; }

        public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;

        public void OnAttemptStarting(in TransitionAttemptContext<ExtState, ExtTrigger> attempt)
        {
            if (ThrowOnBeforeTransition)
                throw new InvalidOperationException("Test exception in OnBeforeTransition");

            BeforeTransitionCallback?.Invoke(attempt);
        }

        public void OnAttemptCompleted(
            in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
            in TransitionResult<ExtState> result)
        {
            if (ThrowOnAfterTransition)
                throw new InvalidOperationException("Test exception in OnAfterTransition");

            AfterTransitionCallback?.Invoke(attempt, result.Outcome == TransitionOutcome.Succeeded);
        }

        public void OnGuardEvaluating(
            in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
            in TransitionInfo<ExtState> candidate,
            string guardName)
        {
            if (ThrowOnGuardEvaluation)
                throw new InvalidOperationException("Test exception in OnGuardEvaluation");
        }

        public void OnGuardEvaluated(
            in TransitionAttemptContext<ExtState, ExtTrigger> attempt,
            in TransitionInfo<ExtState> candidate,
            string guardName,
            bool result)
        {
            if (ThrowOnGuardEvaluated)
                throw new InvalidOperationException("Test exception in OnGuardEvaluated");
        }

    }
}
