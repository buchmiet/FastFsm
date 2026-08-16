using FastFsm.Contracts;

namespace Machines.Tests.Extensions
{
    public class ThrowingExtension : IStateMachineExtension
    {
        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            throw new InvalidOperationException("This extension is designed to fail.");
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            throw new NotImplementedException();
        }

        public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
        {
            throw new NotImplementedException();
        }

        public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
    }
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
        public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
        public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext { }
        public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
    }
}
