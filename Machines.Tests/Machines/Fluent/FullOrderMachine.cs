using Abstractions.Fluent;
using Machines.Tests.Payloads;

namespace Machines.Tests.Machines.Fluent;

// Full variant machine with single payload type and extensions
[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(typeof(OrderPayload))]
public partial class FullOrderMachine
{
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedOrderIds { get; } = new();

    private void Configure() => FSM
        .State<OrderState>(OrderState.New)
        .OnEntry((OnEnterNew))
        .On(OrderTrigger.Process).Guard((CanProcess)).Action((ProcessOrder)).GoTo(OrderState.Processing)
        .State(OrderState.Processing)
        .OnEntry((OnEnterProcessing))
        .On(OrderTrigger.Pay).Action((RecordPayment)).GoTo(OrderState.Paid)
        .On(OrderTrigger.Cancel).GoTo(OrderState.Cancelled)
        .State(OrderState.Paid)
        .OnEntry((OnEnterPaid))
        .On(OrderTrigger.Ship).Guard((CanShip)).GoTo(OrderState.Shipped)
        .State(OrderState.Shipped)
        .On(OrderTrigger.Deliver).GoTo(OrderState.Delivered);

    private bool CanProcess(OrderPayload order) => order.Amount > 0;

    private void ProcessOrder(OrderPayload order)
    {
        ProcessedOrderIds.Add(order.OrderId);
        TotalProcessed += order.Amount;
    }

    private void RecordPayment(OrderPayload order)
    {
        // Payment processing logic
    }

    private bool CanShip(OrderPayload order) => !string.IsNullOrEmpty(order.TrackingNumber);

    private void OnEnterNew() { }
    private void OnEnterProcessing(OrderPayload order) { }
    private void OnEnterPaid(OrderPayload order) { }
}