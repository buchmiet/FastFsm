# Payloads

Payloads let triggers carry typed data into guards and actions.

## Per-trigger payload types

**Attributes:**

```csharp
[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
[PayloadType(OrderTrigger.Pay, typeof(PaymentPayload))]
public partial class OrderMachine
{
    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Action = nameof(HandleOrder))]
    private void Configure() { }

    private void HandleOrder(OrderPayload order) { }
}
```

**Fluent:**

```csharp
[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
public partial class OrderMachineFluent
{
    private void Configure() => FSM
        .State(OrderState.New)
            .On(OrderTrigger.Process)
                .Action(nameof(HandleOrder))
                .GoTo(OrderState.Processing);

    private void HandleOrder(OrderPayload order) { }
}
```

## Default payload type

When every trigger uses the same payload:

```csharp
[StateMachine(typeof(S), typeof(T), DefaultPayloadType = typeof(MyPayload))]
```

## Firing with payload

```csharp
machine.TryFire(T.Process, new OrderPayload { OrderId = 42 });
machine.Fire(T.Process, payload);
```

Async machines use `TryFireAsync` / `FireAsync` with the same payload argument.

## Guards with payload

Guards may accept the payload type:

```csharp
private bool CanProcess(OrderPayload payload) => payload.Amount > 0;
```

Using payload guards on a non-payload machine is an error (FSM0301).

## Multiple payloads on one transition

Fluent `.Payload()` may be called more than once; the last wins (FSM3020 warning). Prefer one payload type per transition.

## Extensible machines

Payload machines that also use extensions typically set `GenerateExtensibleVersion = true` (the default) so constructors accept both extensions and logger.
