// FastFsm.Tests/Machines/FullMultiPayloadMachine_Fluent.cs


using FastFsm.Tests.Features.Integration;
using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Tests.Machines
{
   
    [StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
    [PayloadType(OrderTrigger.Process, typeof(AllFeaturesExtendedTests.OrderPayload))]
    [PayloadType(OrderTrigger.Pay, typeof(AllFeaturesExtendedTests.PaymentPayload))]
    [PayloadType(OrderTrigger.Ship, typeof(AllFeaturesExtendedTests.ShippingPayload))]
    public partial class FullMultiPayloadMachine_Fluent
    {
        private static void Configure() => FSM
            .State<OrderState>(OrderState.New)
                .On(OrderTrigger.Process).Action(nameof(HandleOrder)).GoTo(OrderState.Processing)
            .State(OrderState.Processing)
                .On(OrderTrigger.Pay).Action(nameof(HandlePayment)).GoTo(OrderState.Paid)
            .State(OrderState.Paid)
                .On(OrderTrigger.Ship).Action(nameof(HandleShipping)).GoTo(OrderState.Shipped);

        private void HandleOrder(AllFeaturesExtendedTests.OrderPayload order) { }
        private void HandlePayment(AllFeaturesExtendedTests.PaymentPayload payment) { }
        private void HandleShipping(AllFeaturesExtendedTests.ShippingPayload shipping) { }
    }
}