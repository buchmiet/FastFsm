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
        .OnEntry(OnEnterNew)
        .On(OrderTrigger.Process).Guard<OrderPayload>(CanProcess).Action<OrderPayload>(ProcessOrder).GoTo(OrderState.Processing)
        .State(OrderState.Processing)
        .OnEntry<OrderPayload>(OnEnterProcessing)
        .On(OrderTrigger.Pay).Action<OrderPayload>(RecordPayment).GoTo(OrderState.Paid)
        .On(OrderTrigger.Cancel).GoTo(OrderState.Cancelled)
        .State(OrderState.Paid)
        .OnEntry<OrderPayload>(OnEnterPaid)
        .On(OrderTrigger.Ship).Guard<OrderPayload>(CanShip).GoTo(OrderState.Shipped)
        .State(OrderState.Shipped)
        .On(OrderTrigger.Deliver).GoTo(OrderState.Delivered);

    private bool CanProcess(in OrderPayload order) => order.Amount > 0;

    private void ProcessOrder(in OrderPayload order)
    {
        ProcessedOrderIds.Add(order.OrderId);
        TotalProcessed += order.Amount;
    }

    private void RecordPayment(in OrderPayload order)
    {
        // Payment processing logic
    }

    private bool CanShip(in OrderPayload order) => !string.IsNullOrEmpty(order.TrackingNumber);

    private void OnEnterNew() { }
    private void OnEnterProcessing(in OrderPayload order) { }
    private void OnEnterPaid(in OrderPayload order) { }
}
