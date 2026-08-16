// FastFsm.Tests/Machines/FullMultiPayloadMachine.cs

using FastFsm.Tests.Features.Integration;
using System.Collections.Generic;
using Abstractions.Attributes;

namespace FastFsm.Tests.Machines.Legacy;

[StateMachine(typeof(PhysicalOrderState), typeof(PhysicalOrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(PhysicalOrderTrigger.Process, typeof(AllFeaturesExtendedTests.OrderPayload))]
[PayloadType(PhysicalOrderTrigger.Pay, typeof(AllFeaturesExtendedTests.PaymentPayload))]
[PayloadType(PhysicalOrderTrigger.Ship, typeof(AllFeaturesExtendedTests.ShippingPayload))]
public partial class FullMultiPayloadMachine
{

    [Transition(PhysicalOrderState.New, PhysicalOrderTrigger.Process, PhysicalOrderState.Processing, Action = nameof(HandleOrder))]
    [Transition(PhysicalOrderState.Processing, PhysicalOrderTrigger.Pay, PhysicalOrderState.Paid, Action = nameof(HandlePayment))]
    [Transition(PhysicalOrderState.Paid, PhysicalOrderTrigger.Ship, PhysicalOrderState.Shipped, Action = nameof(HandleShipping))]
    private void Configure() { }

    private void HandleOrder(AllFeaturesExtendedTests.OrderPayload order) { }
    private void HandlePayment(AllFeaturesExtendedTests.PaymentPayload payment) { }
    private void HandleShipping(AllFeaturesExtendedTests.ShippingPayload shipping) { }
}