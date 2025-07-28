// StateMachine.Tests/Machines/FullMultiPayloadMachine.cs

using StateMachine.Contracts;
using StateMachine.Tests.FullVariant;
using System.Collections.Generic;
using Abstractions.Attributes;

namespace StateMachine.Tests.Machines
{
   
    [StateMachine(typeof(OrderState), typeof(OrderTrigger))]
    [PayloadType(OrderTrigger.Process, typeof(FullVariantExtendedTests.OrderPayload))]
    [PayloadType(OrderTrigger.Pay, typeof(FullVariantExtendedTests.PaymentPayload))]
    [PayloadType(OrderTrigger.Ship, typeof(FullVariantExtendedTests.ShippingPayload))]
    [GenerationMode(GenerationMode.Full, Force = true)]
    public partial class FullMultiPayloadMachine
    {

        [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Action = nameof(HandleOrder))]
        [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid, Action = nameof(HandlePayment))]
        [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped, Action = nameof(HandleShipping))]
        private void Configure() { }

        private void HandleOrder(FullVariantExtendedTests.OrderPayload order) { }
        private void HandlePayment(FullVariantExtendedTests.PaymentPayload payment) { }
        private void HandleShipping(FullVariantExtendedTests.ShippingPayload shipping) { }
    }
}