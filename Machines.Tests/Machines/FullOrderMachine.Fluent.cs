using Abstractions.Attributes;
using Abstractions.Fluent;
using Machines.Tests.Features.Integration;

namespace Machines.Tests.Machines;

// Full variant machine with single payload type and extensions
[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(typeof(OrderPayload))]
public partial class FullOrderMachineFluent
{
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedOrderIds { get; } = new();

    private void Configure() => FSM
        .State<OrderState>(OrderState.New)
        .OnEntry(nameof(OnEnterNew))
        .On(OrderTrigger.Process).Guard(nameof(CanProcess)).Action(nameof(ProcessOrder)).GoTo(OrderState.Processing)
        .State(OrderState.Processing)
        .OnEntry(nameof(OnEnterProcessing))
        .On(OrderTrigger.Pay).Action(nameof(RecordPayment)).GoTo(OrderState.Paid)
        .On(OrderTrigger.Cancel).GoTo(OrderState.Cancelled)
        .State(OrderState.Paid)
        .OnEntry(nameof(OnEnterPaid))
        .On(OrderTrigger.Ship).Guard(nameof(CanShip)).GoTo(OrderState.Shipped)
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