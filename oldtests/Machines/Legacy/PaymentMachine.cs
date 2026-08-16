using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Payloads;

namespace FastFsm.Tests.Machines.Legacy;

[StateMachine(typeof(PaymentState), typeof(PaymentTrigger))]
[PayloadType(typeof(PaymentData))]
public partial class PaymentMachine
{
    private const decimal ApprovalThreshold = 100;

    [Transition(PaymentState.Pending, PaymentTrigger.Process, PaymentState.Processed,
        Guard = nameof(CanProcessDirectly))]
    private void Configure() { }

    private bool CanProcessDirectly(PaymentData payment) => payment.Amount <= ApprovalThreshold;
}