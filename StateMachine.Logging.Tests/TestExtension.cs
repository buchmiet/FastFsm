using StateMachine.Contracts;

namespace StateMachine.Logging.Tests
{
    /// <summary>
    /// Test extension that can throw exceptions
    /// </summary>
    public class TestExtension : IStateMachineExtension
    {
        public bool ThrowOnBeforeTransition { get; set; }
        public bool ThrowOnAfterTransition { get; set; }
        public bool ThrowOnGuardEvaluation { get; set; }
        public bool ThrowOnGuardEvaluated { get; set; }

        public Action<IStateMachineContext>? BeforeTransitionCallback { get; set; }
        public Action<IStateMachineContext, bool>? AfterTransitionCallback { get; set; }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            if (ThrowOnBeforeTransition)
                throw new InvalidOperationException("Test exception in OnBeforeTransition");

            BeforeTransitionCallback?.Invoke(context);
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            if (ThrowOnAfterTransition)
                throw new InvalidOperationException("Test exception in OnAfterTransition");

            AfterTransitionCallback?.Invoke(context, success);
        }

        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
        {
            if (ThrowOnGuardEvaluation)
                throw new InvalidOperationException("Test exception in OnGuardEvaluation");
        }

        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
        {
            if (ThrowOnGuardEvaluated)
                throw new InvalidOperationException("Test exception in OnGuardEvaluated");
        }
    }
}
