using Machines.Tests.Payloads;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(PaymentState), typeof(PaymentTrigger))]
[PayloadType(typeof(PaymentData))]
public partial class PaymentMachine
{
    private const decimal ApprovalThreshold = 100;

    [Transition(PaymentState.Pending, PaymentTrigger.Process, PaymentState.Processed,
        Guard = (CanProcessDirectly))]
    private void Configure() { }

    private bool CanProcessDirectly(PaymentData payment) => payment.Amount <= ApprovalThreshold;
}
