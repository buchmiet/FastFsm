// FastFsm.Tests/Machines/FullMultiPayloadMachine.cs


using System.Collections.Generic;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Integration;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(AllFeaturesExtendedTests.OrderPayload))]
[PayloadType(OrderTrigger.Pay, typeof(AllFeaturesExtendedTests.PaymentPayload))]
[PayloadType(OrderTrigger.Ship, typeof(AllFeaturesExtendedTests.ShippingPayload))]
public partial class FullMultiPayloadMachine
{
    public List<int> ProcessedOrderIds { get; } = new();
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedPaymentIds { get; } = new();
    public List<int> ShippedTrackingNumbers { get; } = new();

    private void Configure() => FSM
        .State<OrderState>(OrderState.New)
        .On(OrderTrigger.Process).Action(nameof(HandleOrder)).GoTo(OrderState.Processing)
        .State(OrderState.Processing)
        .On(OrderTrigger.Pay).Action(nameof(HandlePayment)).GoTo(OrderState.Paid)
        .State(OrderState.Paid)
        .On(OrderTrigger.Ship).Action(nameof(HandleShipping)).GoTo(OrderState.Shipped)
        .State(OrderState.Shipped)
        .State(OrderState.Delivered)
        .State(OrderState.Cancelled);

    private void HandleOrder(AllFeaturesExtendedTests.OrderPayload order) 
    {
        ProcessedOrderIds.Add(order.OrderId);
        TotalProcessed += order.Amount;
    }
        
    private void HandlePayment(AllFeaturesExtendedTests.PaymentPayload payment) 
    {
        ProcessedPaymentIds.Add(payment.OrderId);
    }
        
    private void HandleShipping(AllFeaturesExtendedTests.ShippingPayload shipping) 
    {
        ShippedTrackingNumbers.Add(shipping.OrderId);
    }
}