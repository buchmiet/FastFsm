using FastFsm.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFsm.Tests.Extensions
{
    public class ResultCapturingExtension : IStateMachineExtension
    {
        public List<bool> Results { get; } = [];

        public void OnAfterTransition<T>(T ctx, bool success) where T : IStateMachineContext
            => Results.Add(success);

        public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext { }
        public void OnGuardEvaluation<T>(T ctx, string g) where T : IStateMachineContext { }
        public void OnGuardEvaluated<T>(T ctx, string g, bool r) where T : IStateMachineContext { }

        public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext { }

        public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext { }
    }
}
