# Payloads

Payloads associate typed data with triggers and make that data available to guards and actions.

## Per-trigger payload types

### Attribute API

```csharp
[StateMachine(typeof(OrderState), typeof(OrderTrigger), GenerateExtensibleVersion = true)]
[PayloadType(OrderTrigger.Process, typeof(OrderPayload))]
[PayloadType(OrderTrigger.Pay, typeof(PaymentPayload))]
public partial class OrderMachine
{
    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing, Action = nameof(HandleOrder))]
    private void ConfigureTransitions() { }

    private void HandleOrder(OrderPayload order) { }
}
```

### Fluent API

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

Set a default payload type when triggers share the same payload type:

```csharp
[StateMachine(typeof(S), typeof(T), DefaultPayloadType = typeof(MyPayload))]
```

## Firing with a payload

```csharp
machine.TryFire(T.Process, new OrderPayload { OrderId = 42 });
machine.Fire(T.Process, payload);
```

Asynchronous machines use `TryFireAsync` and `FireAsync` with the same payload argument.

## Guards with payloads

A guard can accept the configured payload type:

```csharp
private bool CanProcess(OrderPayload payload) => payload.Amount > 0;
```

A payload guard on a machine without payload configuration is reported as FSM0301.

## Multiple payload declarations

When Fluent `.Payload()` is called more than once for one transition, the generator reports FSM3020 and uses the last configured value.

## Extensions

Payload configuration is independent of `GenerateExtensibleVersion`. When extension support is required, set `GenerateExtensibleVersion = true` explicitly; see [extensions.md](extensions.md).
