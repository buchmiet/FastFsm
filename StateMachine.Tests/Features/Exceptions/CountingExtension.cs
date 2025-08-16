using StateMachine.Contracts;


namespace StateMachine.Tests.Features.Exceptions
{
    /// <summary>
    /// Implementacja rozszerzenia, które zlicza swoje wywołania.
    /// </summary>
    public class CountingExtension : IStateMachineExtension
    {
        public int BeforeTransitionCount { get; private set; }
        public int AfterTransitionCount { get; private set; }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            BeforeTransitionCount++;
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            AfterTransitionCount++;
        }

        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
    }
}
