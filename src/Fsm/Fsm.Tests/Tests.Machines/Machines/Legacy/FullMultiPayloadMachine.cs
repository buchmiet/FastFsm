using Abstractions.Attributes;
﻿// Tests.Fsm/Machines/FullMultiPayloadMachine.cs

using Tests.Machines.Payloads;

namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(PhysicalOrderState), typeof(PhysicalOrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(PhysicalOrderTrigger.Process, typeof(OrderPayload))]
[PayloadType(PhysicalOrderTrigger.Pay, typeof(PaymentPayload))]
[PayloadType(PhysicalOrderTrigger.Ship, typeof(ShippingPayload))]
public partial class FullMultiPayloadMachine
{

    [Transition(PhysicalOrderState.New, PhysicalOrderTrigger.Process, PhysicalOrderState.Processing, Action = nameof(HandleOrder))]
    [Transition(PhysicalOrderState.Processing, PhysicalOrderTrigger.Pay, PhysicalOrderState.Paid, Action = nameof(HandlePayment))]
    [Transition(PhysicalOrderState.Paid, PhysicalOrderTrigger.Ship, PhysicalOrderState.Shipped, Action = nameof(HandleShipping))]
    private void Configure() { }

    private void HandleOrder(OrderPayload order) { }
    private void HandlePayment(PaymentPayload payment) { }
    private void HandleShipping(ShippingPayload shipping) { }
}
