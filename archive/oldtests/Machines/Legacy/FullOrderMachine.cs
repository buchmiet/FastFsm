using System.Collections.Generic;
using static FastFsm.Tests.Features.Integration.AllFeaturesExtendedTests;

namespace FastFsm.Tests.Machines.Legacy;

// Full variant machine with single payload type and extensions
[StateMachine(typeof(PhysicalOrderState), typeof(PhysicalOrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(typeof(OrderPayload))]



public partial class FullOrderMachine
{
    public decimal TotalProcessed { get; private set; }
    public List<int> ProcessedOrderIds { get; } = new();

    [State(PhysicalOrderState.New, OnEntry = nameof(OnEnterNew))]
    [State(PhysicalOrderState.Processing, OnEntry = nameof(OnEnterProcessing))]
    [State(PhysicalOrderState.Paid, OnEntry = nameof(OnEnterPaid))]
    private void ConfigureStates() { }

    [Transition(PhysicalOrderState.New, PhysicalOrderTrigger.Process, PhysicalOrderState.Processing,
        Guard = nameof(CanProcess), Action = nameof(ProcessOrder))]
    [Transition(PhysicalOrderState.Processing, PhysicalOrderTrigger.Pay, PhysicalOrderState.Paid,
        Action = nameof(RecordPayment))]
    [Transition(PhysicalOrderState.Processing, PhysicalOrderTrigger.Cancel, PhysicalOrderState.Cancelled)]
    [Transition(PhysicalOrderState.Paid, PhysicalOrderTrigger.Ship, PhysicalOrderState.Shipped,
        Guard = nameof(CanShip))]
    [Transition(PhysicalOrderState.Shipped, PhysicalOrderTrigger.Deliver, PhysicalOrderState.Delivered)]
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
