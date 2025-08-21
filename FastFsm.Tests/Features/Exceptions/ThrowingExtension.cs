
using System;
using FastFsm.Contracts;


namespace FastFsm.Tests.Features.Exceptions
{
    /// <summary>
    /// Implementacja rozszerzenia, które zawsze rzuca wyjątek.
    /// </summary>
    public class ThrowingExtension : IStateMachineExtension
    {
        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            throw new InvalidOperationException("This extension is designed to fail.");
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
    }
}
