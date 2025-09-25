using Machines.Tests.Features.Integration;

namespace Machines.Tests.Machines;

// Full variant machine with single payload type and extensions
[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(typeof(OrderPayload))]



public partial class FullOrderMachine
{
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedOrderIds { get; } = new();

    [State(OrderState.New, OnEntry = nameof(OnEnterNew))]
    [State(OrderState.Processing, OnEntry = nameof(OnEnterProcessing))]
    [State(OrderState.Paid, OnEntry = nameof(OnEnterPaid))]
    private void ConfigureStates() { }

    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing,
        Guard = nameof(CanProcess), Action = nameof(ProcessOrder))]
    [Transition(OrderState.Processing, OrderTrigger.Pay, OrderState.Paid,
        Action = nameof(RecordPayment))]
    [Transition(OrderState.Processing, OrderTrigger.Cancel, OrderState.Cancelled)]
    [Transition(OrderState.Paid, OrderTrigger.Ship, OrderState.Shipped,
        Guard = nameof(CanShip))]
    [Transition(OrderState.Shipped, OrderTrigger.Deliver, OrderState.Delivered)]
    private void Configure() { }

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
public enum OrderState { New, Processing, Paid, Shipped, Delivered, Cancelled }
public enum OrderTrigger { Process, Pay, Ship, Deliver, Cancel, Refund }