// FastFsm.Tests/Machines/FullMultiPayloadMachineFluent.cs


using FastFsm.Tests.Features.Integration;
using System.Collections.Generic;
using Abstractions.Fluent;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
[PayloadType(OrderTrigger.Pay, typeof(PaymentPayload))]
[PayloadType(OrderTrigger.Ship, typeof(ShippingPayload))]
public partial class FullMultiPayloadMachineFluent
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

    private void HandleOrder(OrderPayload order) 
    {
        ProcessedOrderIds.Add(order.OrderId);
        TotalProcessed += order.Amount;
    }
        
    private void HandlePayment(PaymentPayload payment) 
    {
        ProcessedPaymentIds.Add(payment.OrderId);
    }
        
    private void HandleShipping(ShippingPayload shipping) 
    {
        ShippedTrackingNumbers.Add(shipping.OrderId);
    }
}