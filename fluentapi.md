# FastFSM Fluent API Documentation

## Overview

The Fluent API is a complete, production-ready alternative to attribute-based state machine definitions in FastFSM. It provides a more intuitive and readable way to define state machines while generating identical runtime code.

## Core Design Principles

1. **Zero lambdas** - Only `nameof(Method)` and literal values allowed
2. **Zero imperative logic** - The `Configure()` method contains only declarative DSL calls
3. **Compile-time only** - All DSL methods are no-ops at runtime; the source generator processes them
4. **Type safety** - Full IntelliSense support and compile-time validation
5. **Parity with attributes** - Every feature available via attributes is available via Fluent API

## Basic State Machine Structure

Every Fluent API state machine requires:
1. The `[StateMachine]` attribute to specify state and trigger types
2. A static `Configure()` method containing the DSL definition
3. State and trigger enums
4. Handler methods for guards, actions, and callbacks

### Minimal Example

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleMachine
{
    public enum State { Idle, Active, Done }
    public enum Trigger { Start, Complete }

    private static void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start).GoTo(State.Active)
        .State(State.Active)
            .On(Trigger.Complete).GoTo(State.Done)
        .State(State.Done);
}
```

**Important:** Note that `.State()` in the chain does NOT use generic type parameters. Only the initial `FSM.State()` can optionally be generic for type inference.

## Complete DSL Reference

### State Definition Methods

| Method | Description | Example |
|--------|-------------|---------|
| `.State(TState state)` | Define a state | `.State(State.Idle)` |
| `.OnEntry(string methodName)` | Set entry callback | `.OnEntry(nameof(OnIdleEntry))` |
| `.OnEntryAsync(string methodName)` | Set async entry callback | `.OnEntryAsync(nameof(OnIdleEntryAsync))` |
| `.OnExit(string methodName)` | Set exit callback | `.OnExit(nameof(OnIdleExit))` |
| `.OnExitAsync(string methodName)` | Set async exit callback | `.OnExitAsync(nameof(OnIdleExitAsync))` |

### Transition Definition Methods

| Method | Description | Example |
|--------|-------------|---------|
| `.On(TTrigger trigger)` | Start transition definition | `.On(Trigger.Start)` |
| `.OnInternal(TTrigger trigger)` | Start internal transition | `.OnInternal(Trigger.Update)` |

### Transition Modifiers

**Recommended order:** Payload → Guard → Action → GoTo

| Method | Description | Example |
|--------|-------------|---------|
| `.Payload<T>()` | Specify payload type | `.Payload<OrderData>()` |
| `.Guard(string methodName)` | Add guard condition | `.Guard(nameof(CanStart))` |
| `.GuardAsync(string methodName)` | Add async guard | `.GuardAsync(nameof(CanStartAsync))` |
| `.Action(string methodName)` | Add transition action | `.Action(nameof(ProcessStart))` |
| `.ActionAsync(string methodName)` | Add async action | `.ActionAsync(nameof(ProcessStartAsync))` |

### Transition Target Methods

| Method | Description | Example |
|--------|-------------|---------|
| `.GoTo(TState targetState)` | Set target state | `.GoTo(State.Active)` |
| `.Internal()` | Mark as internal transition | `.Internal()` |

## Common Patterns

### State Machine with Guards and Actions

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class GuardedMachine
{
    public enum State { Idle, Running, Stopped }
    public enum Trigger { Start, Stop, Pause }
    
    private bool _isReady;
    
    private static void Configure() => FSM
        .State(State.Idle)
            .OnEntry(nameof(PrepareSystem))
            .On(Trigger.Start)
                .Guard(nameof(CanStart))
                .Action(nameof(StartProcess))
                .GoTo(State.Running)
        .State(State.Running)
            .On(Trigger.Stop)
                .Action(nameof(StopProcess))
                .GoTo(State.Stopped)
            .On(Trigger.Pause)
                .GoTo(State.Idle)
        .State(State.Stopped)
            .OnExit(nameof(Cleanup));
    
    private void PrepareSystem() => _isReady = true;
    private bool CanStart() => _isReady;
    private void StartProcess() { /* implementation */ }
    private void StopProcess() { /* implementation */ }
    private void Cleanup() => _isReady = false;
}
```

### State Machine with Default Payload

```csharp
[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(OrderData))]
public partial class OrderMachine
{
    public enum State { New, Processing, Shipped }
    public enum Trigger { Process, Ship, Cancel }
    
    public sealed class OrderData
    {
        public required string OrderId { get; init; }
        public decimal Amount { get; init; }
    }
    
    private static void Configure() => FSM
        .State(State.New)
            .On(Trigger.Process)
                .Guard(nameof(ValidateOrder))
                .Action(nameof(ProcessOrder))
                .GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Ship)
                .Action(nameof(ShipOrder))
                .GoTo(State.Shipped)
            .On(Trigger.Cancel)
                .Action(nameof(CancelOrder))
                .GoTo(State.New);
    
    // When DefaultPayloadType is set, handlers can accept the payload
    private bool ValidateOrder(OrderData data) => data.Amount > 0;
    private void ProcessOrder(OrderData data) { /* process */ }
    private void ShipOrder(OrderData data) { /* ship */ }
    private void CancelOrder() { /* cancel - no payload needed */ }
}
```

### State Machine with Multiple Payload Types

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MultiPayloadMachine
{
    public enum State { Idle, Processing, Done }
    public enum Trigger { Submit, Update, Complete }
    
    public class SubmitRequest { public string Id { get; set; } }
    public class UpdateData { public int Count { get; set; } }
    
    private static void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Submit)
                .Payload<SubmitRequest>()
                .Action(nameof(HandleSubmit))
                .GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Update)
                .Payload<UpdateData>()
                .Action(nameof(HandleUpdate))
                .Internal()  // Stay in same state
            .On(Trigger.Complete)
                .GoTo(State.Done);
    
    private void HandleSubmit(SubmitRequest request) { /* handle */ }
    private void HandleUpdate(UpdateData data) { /* update */ }
}
```

### Async State Machine

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class AsyncMachine
{
    public enum State { Disconnected, Connecting, Connected }
    public enum Trigger { Connect, ConnectionEstablished, Disconnect }
    
    private static void Configure() => FSM
        .State(State.Disconnected)
            .OnEntryAsync(nameof(CleanupAsync))
            .On(Trigger.Connect)
                .GuardAsync(nameof(CanConnectAsync))
                .ActionAsync(nameof(StartConnectionAsync))
                .GoTo(State.Connecting)
        .State(State.Connecting)
            .On(Trigger.ConnectionEstablished)
                .ActionAsync(nameof(FinalizeConnectionAsync))
                .GoTo(State.Connected)
        .State(State.Connected)
            .OnExitAsync(nameof(DisconnectAsync))
            .On(Trigger.Disconnect)
                .GoTo(State.Disconnected);
    
    private async Task CleanupAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
    }
    
    private async ValueTask<bool> CanConnectAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
        return true;
    }
    
    private async Task StartConnectionAsync(CancellationToken ct)
    {
        await Task.Delay(500, ct);
    }
    
    private async Task FinalizeConnectionAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
    }
    
    private async ValueTask DisconnectAsync()
    {
        await Task.Delay(50);
    }
}
```

### Internal Transitions

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class InternalTransitionMachine
{
    public enum State { Active, Inactive }
    public enum Trigger { Update, Toggle }
    
    public sealed class UpdateData { public int Value { get; init; } }
    
    private int _counter;
    
    private static void Configure() => FSM
        .State(State.Active)
            .OnInternal(Trigger.Update)  // Use OnInternal for internal transitions
                .Payload<UpdateData>()
                .Guard(nameof(ValidateUpdate))
                .Action(nameof(ApplyUpdate))
                .Internal()  // Stays in Active state
            .On(Trigger.Toggle)
                .GoTo(State.Inactive)
        .State(State.Inactive)
            .On(Trigger.Toggle)
                .GoTo(State.Active);
    
    private bool ValidateUpdate(UpdateData data) => data.Value > 0;
    private void ApplyUpdate(UpdateData data) => _counter += data.Value;
}
```

## Method Signatures

### Guard Methods

Guards must return `bool` (sync) or `ValueTask<bool>`/`Task<bool>` (async):

```csharp
// Synchronous guards
private bool CanTransition() => true;
private bool CanTransitionWithPayload(MyPayload payload) => payload.IsValid;

// Asynchronous guards
private async ValueTask<bool> CanTransitionAsync(CancellationToken ct) => await CheckAsync(ct);
private async Task<bool> CanTransitionWithPayloadAsync(MyPayload payload, CancellationToken ct) 
    => await ValidateAsync(payload, ct);
```

### Action Methods

Actions return `void` (sync) or `Task`/`ValueTask` (async):

```csharp
// Synchronous actions
private void OnTransition() { /* implementation */ }
private void OnTransitionWithPayload(MyPayload payload) { /* process payload */ }

// Asynchronous actions
private async Task OnTransitionAsync(CancellationToken ct) => await ProcessAsync(ct);
private async ValueTask OnTransitionWithPayloadAsync(MyPayload payload, CancellationToken ct) 
    => await ProcessPayloadAsync(payload, ct);
```

### State Callbacks (OnEntry/OnExit)

```csharp
// Synchronous callbacks
private void OnStateEntry() { /* initialization */ }
private void OnStateEntryWithPayload(MyPayload payload) { /* init with payload */ }
private void OnStateExit() { /* cleanup */ }

// Asynchronous callbacks
private async Task OnStateEntryAsync(CancellationToken ct) => await InitAsync(ct);
private async ValueTask OnStateExitAsync() => await CleanupAsync();
```

## Important Notes

### Type Parameters in Chains
❌ **Incorrect** (causes CS0308 compilation error):
```csharp
FSM.State<State>(State.A)
   .State<State>(State.B)  // ERROR: State() is not generic in the chain
```

✅ **Correct**:
```csharp
FSM.State(State.A)
   .State(State.B)  // No type parameter needed
```

### Method Order
While the parser is flexible, the recommended order for readability is:
1. State definition (`.State()`)
2. State callbacks (`.OnEntry()`, `.OnExit()`)
3. Transition trigger (`.On()` or `.OnInternal()`)
4. Payload specification (`.Payload<T>()`)
5. Guard (`.Guard()` or `.GuardAsync()`)
6. Action (`.Action()` or `.ActionAsync()`)
7. Target (`.GoTo()` or `.Internal()`)

### Parser Behavior
- The FluentParser processes the `Configure()` method at compile time
- It generates the same `StateMachineModel` as the attribute-based parser
- Both approaches produce identical runtime code
- Mixing attributes and Fluent API in the same class is not supported

## Migration from Attributes

### Attribute-based (Legacy)
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class AttributeMachine
{
    [Transition(State.A, Trigger.Next, State.B, Guard = nameof(CanGo), Action = nameof(OnGo))]
    [State(State.B, OnEntry = nameof(OnBEntry))]
    private void Configure() { }
    
    private bool CanGo() => true;
    private void OnGo() { }
    private void OnBEntry() { }
}
```

### Fluent API (Recommended)
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class FluentMachine
{
    private static void Configure() => FSM
        .State(State.A)
            .On(Trigger.Next)
                .Guard(nameof(CanGo))
                .Action(nameof(OnGo))
                .GoTo(State.B)
        .State(State.B)
            .OnEntry(nameof(OnBEntry));
    
    private bool CanGo() => true;
    private void OnGo() { }
    private void OnBEntry() { }
}
```

## Limitations and Future Work

### Currently Supported
✅ Simple state machines (FSM)  
✅ Guards (sync/async)  
✅ Actions (sync/async)  
✅ State callbacks (OnEntry/OnExit, sync/async)  
✅ Payloads (default and per-transition)  
✅ Internal transitions  
✅ Extensible state machines  

### Not Yet Implemented (Coming in v0.8)
❌ Hierarchical State Machines (HSM)  
❌ Parent states  
❌ History modes (Shallow/Deep)  
❌ Priority for transitions  
❌ Initial substates  

## Best Practices

1. **Keep Configure() pure** - No business logic, only DSL calls
2. **Use meaningful names** - State and trigger names should be self-documenting
3. **Prefer Fluent over attributes** - More readable and maintainable
4. **One transition per line** - Format for readability
5. **Group related states** - Keep related state definitions together
6. **Test both paths** - Ensure guard true/false paths are tested
7. **Handle cancellation** - Always respect CancellationToken in async methods

## Troubleshooting

### Common Errors

**CS0308: The non-generic method 'StateBuilder.State' cannot be used with type arguments**
- Remove type parameters from chained `.State()` calls
- Only the first `FSM.State()` can be generic

**No transition from state 'X' on trigger 'Y'**
- Ensure the transition is defined in Configure()
- Check that guards aren't blocking the transition
- Verify the state machine has been started with `.Start()`

**Method 'X' not found**
- Ensure method names use `nameof()` not strings
- Check method signatures match expected patterns
- Methods must be instance methods (not static) except Configure()

## Version History

- **v0.7.5** - Initial Fluent API release with full FSM support
- **v0.8.0** - (Planned) HSM support with Parent(), History(), Priority()

---

*For more examples and test cases, see the FastFsm.Tests project, particularly the FluentAPISpecificTests and FluentAPIComparisonTests classes.*