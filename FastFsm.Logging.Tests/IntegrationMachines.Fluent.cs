using Abstractions.Fluent;
using Abstractions.Attributes;
using Machines.Tests.Payloads;
using OrderState = Machines.Tests.Machines.OrderState;
using OrderTrigger = Machines.Tests.Machines.OrderTrigger;

namespace FastFsm.Logging.Tests;

public enum TestInitialState { Ready, Working, Done }
public enum TestInitialTrigger { Go, Stop }

public enum WorkflowState { Draft, Submitted }
public enum WorkflowTrigger { Submit }

[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), GenerateExtensibleVersion = true)]
public partial class EmptyExtensibleMachine
{
    private static void Configure() => FSM
        .State(WorkflowState.Draft).On(WorkflowTrigger.Submit).GoTo(WorkflowState.Submitted);
}

[StateMachine(typeof(TestInitialState), typeof(TestInitialTrigger))]
public partial class InitialOnEntryStateMachineActionsFluent
{
    private static void Configure() => FSM
        .State(TestInitialState.Ready)
            .OnEntry(nameof(OnReadyEntry));

    private void OnReadyEntry() { }
}

[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
[PayloadType(OrderTrigger.Pay, typeof(PaymentPayload))]
[PayloadType(OrderTrigger.Ship, typeof(ShippingPayload))]
public partial class LoggingFullMultiPayloadMachine
{
    private static void Configure() => FSM
        .State(OrderState.New)
            .OnEntry(nameof(OnNewEntry))
            .On(OrderTrigger.Process)
                .Action(nameof(HandleOrder))
                .GoTo(OrderState.Processing)
        .State(OrderState.Processing)
            .On(OrderTrigger.Pay)
                .Action(nameof(HandlePayment))
                .GoTo(OrderState.Paid)
        .State(OrderState.Paid)
            .On(OrderTrigger.Ship)
                .Action(nameof(HandleShipping))
                .GoTo(OrderState.Shipped);

    private void OnNewEntry() { }
    private void HandleOrder(OrderPayload order) { }
    private void HandlePayment(PaymentPayload payment) { }
    private void HandleShipping(ShippingPayload shipping) { }
}
