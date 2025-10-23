using Abstractions.Fluent;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

// Initial OnEntry state machine - Fluent version
[StateMachine(typeof(TestInitialState), typeof(TestInitialTrigger))]
public partial class InitialOnEntryStateMachineActionsFluent
{
    private static void Configure() => FSM
        .State(TestInitialState.Ready)
            .OnEntry(nameof(OnReadyEntry));

    private void OnReadyEntry() { }
}

// Full multi-payload machine - Fluent version
[StateMachine(typeof(OrderStatePayload), typeof(OrderTriggerPayload), GenerateExtensibleVersion = true)]
[PayloadType(OrderTriggerPayload.Process, typeof(OrderPayload))]
[PayloadType(OrderTriggerPayload.Pay, typeof(PaymentPayload))]
[PayloadType(OrderTriggerPayload.Ship, typeof(ShippingPayload))]
public partial class FullMultiPayloadMachine
{
    private static void Configure() => FSM
        .State(OrderStatePayload.New)
            .OnEntry(nameof(OnNewEntry))
            .On(OrderTriggerPayload.Process)
                .Action(nameof(HandleOrder))
                .GoTo(OrderStatePayload.Processing).And()
        .State(OrderStatePayload.Processing)
            .On(OrderTriggerPayload.Pay)
                .Action(nameof(HandlePayment))
                .GoTo(OrderStatePayload.Paid).And()
        .State(OrderStatePayload.Paid)
            .On(OrderTriggerPayload.Ship)
                .Action(nameof(HandleShipping))
                .GoTo(OrderStatePayload.Shipped);

    private void OnNewEntry() { }
    private void HandleOrder(OrderPayload order) { }
    private void HandlePayment(PaymentPayload payment) { }
    private void HandleShipping(ShippingPayload shipping) { }
}

