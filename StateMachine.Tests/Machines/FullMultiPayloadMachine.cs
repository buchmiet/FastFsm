// StateMachine.Tests/Machines/FullMultiPayloadMachine.cs

using StateMachine.Contracts;
using StateMachine.Tests.Features.Integration;
using System.Collections.Generic;
using Abstractions.Attributes;

namespace StateMachine.Tests.Machines
{
   
    [StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
    [PayloadType(OrderTrigger.Process, typeof(AllFeaturesExtendedTests.OrderPayload))]
    [PayloadType(OrderTrigger.Pay, typeof(AllFeaturesExtendedTests.PaymentPayload))]
    [PayloadType(OrderTrigger.Ship, typeof(AllFeaturesExtendedTests.ShippingPayload))]
    public partial class FullMultiPayloadMachine
    {

        [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Action = nameof(HandleOrder))]
        [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid, Action = nameof(HandlePayment))]
        [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped, Action = nameof(HandleShipping))]
        private void Configure() { }

        private void HandleOrder(AllFeaturesExtendedTests.OrderPayload order) { }
        private void HandlePayment(AllFeaturesExtendedTests.PaymentPayload payment) { }
        private void HandleShipping(AllFeaturesExtendedTests.ShippingPayload shipping) { }
    }
}
