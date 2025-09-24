// FastFsm.Tests/Machines/FullMultiPayloadMachine.cs


using FastFsm.Tests.Features.Integration;
using System.Collections.Generic;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
[PayloadType(OrderTrigger.Pay, typeof(PaymentPayload))]
[PayloadType(OrderTrigger.Ship, typeof(ShippingPayload))]
public partial class FullMultiPayloadMachine
{
    public List<int> ProcessedOrderIds { get; } = new();
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedPaymentIds { get; } = new();
    public List<int> ShippedTrackingNumbers { get; } = new();

    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Action = nameof(HandleOrder))]
    [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid, Action = nameof(HandlePayment))]
    [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped, Action = nameof(HandleShipping))]
    private void Configure() { }

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