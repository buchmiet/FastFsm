using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Instance.Tests;

[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(OrderPayload))]
public partial class PayloadInstanceMachine
{
    public enum State { Idle, Processing, Completed }
    public enum Trigger { Submit, Finish }

    public OrderPayload? LastPayload { get; private set; }

    private void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Submit)
                .Guard<OrderPayload>(CanSubmit)
                .Action<OrderPayload>(StartProcessing)
                .GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Finish)
                .Action(CompleteOrder)
                .GoTo(State.Completed);

    private bool CanSubmit(in OrderPayload payload) => !string.IsNullOrWhiteSpace(payload.Id);
    private void StartProcessing(in OrderPayload payload) => LastPayload = payload;
    private void CompleteOrder() => LastPayload = null;

    public readonly record struct OrderPayload(string Id);
}
