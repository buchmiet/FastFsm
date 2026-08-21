using Tests.Machines.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastFsm.Contracts;

namespace Tests.Machines.Extensions
{
    public class AuditExtension : IStateMachineExtension<PhysicalOrderState, PhysicalOrderTrigger>
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

        public void OnAttemptCompleted(
            in TransitionAttemptContext<PhysicalOrderState, PhysicalOrderTrigger> attempt,
            in TransitionResult<PhysicalOrderState> result)
        {
            Entries.Add(new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                FromState = attempt.SourceState,
                ToState = result.FinalState,
                Trigger = attempt.Trigger,
                PayloadType = attempt.Payload?.GetType(),
                PayloadData = attempt.Payload,
                Success = result.Outcome == TransitionOutcome.Succeeded
            });
        }
    }
}
