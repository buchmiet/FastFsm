using Machines.Tests.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastFsm.Contracts;

namespace Machines.Tests.Extensions
{
    public class AuditExtension : IStateMachineExtension
    {
        public List<AuditEntry> Entries { get; } = new();

        public class AuditEntry
        {
            public DateTime Timestamp { get; set; }
            public object FromState { get; set; } = null!;
            public object ToState { get; set; } = null!;
            public object? Trigger { get; set; }
            public Type? PayloadType { get; set; }
            public object? PayloadData { get; set; }
            public bool Success { get; set; }
        }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            // Capture state before
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            if (context is IStateMachineContext<PhysicalOrderState, PhysicalOrderTrigger> orderContext &&
                context is IStateSnapshot snapshot)
            {
                Entries.Add(new AuditEntry
                {
                    Timestamp = context.Timestamp,
                    FromState = snapshot.FromState,
                    ToState = snapshot.ToState,
                    Trigger = snapshot.Trigger,
                    PayloadType = orderContext.Payload?.GetType(),
                    PayloadData = orderContext.Payload,
                    Success = success
                });
            }
        }

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
}
