// Tests.Fsm/Machines/FullMultiPayloadMachine.cs


using Abstractions.Fluent;
using Tests.Machines.Payloads;

namespace Tests.Machines.Machines.Fluent;

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

    private void Configure() => FSM
        .State<OrderState>(OrderState.New)
        .On(OrderTrigger.Process).Action<OrderPayload>(HandleOrder).GoTo(OrderState.Processing)
        .State(OrderState.Processing)
        .On(OrderTrigger.Pay).Action<PaymentPayload>(HandlePayment).GoTo(OrderState.Paid)
        .State(OrderState.Paid)
        .On(OrderTrigger.Ship).Action<ShippingPayload>(HandleShipping).GoTo(OrderState.Shipped)
        .State(OrderState.Shipped)
        .State(OrderState.Delivered)
        .State(OrderState.Cancelled);

    private void HandleOrder(in OrderPayload order) 
    {
        ProcessedOrderIds.Add(order.OrderId);
        TotalProcessed += order.Amount;
    }

    private void HandlePayment(in PaymentPayload payment) => ProcessedPaymentIds.Add(payment.OrderId);

    private void HandleShipping(in ShippingPayload shipping) => ShippedTrackingNumbers.Add(shipping.OrderId);
}
