using FastFsm.Contracts;

namespace Tests.Machines.Extensions
{

        public class FaultyExtension : IStateMachineExtension
        {
            public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }

            public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }

            public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }

            public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }

            public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }

            public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }

            public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext
            {
                throw new Exception("Extension error");
            }
        }
    }




