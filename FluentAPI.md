# FastFSM Fluent API Documentation (v0.8.0)

## Overview

The Fluent API is a complete, production-ready alternative to attribute-based state machine definitions in FastFSM. It provides a more intuitive and readable way to define both simple (FSM) and hierarchical (HSM) state machines while generating identical runtime code. Full HSM support includes parent-child relationships, history modes (shallow/deep), initial substates, and priority-based transition resolution.

> **Compile-time only**
> `Configure()` is **analyzed at compile-time** by the source generator. It is **not executed** at runtime.
> DSL accepts only **method groups** (no lambdas/expressions). Any attempt to read instance state or compute values inside DSL will produce a **compile error**.

## What's New in v0.8.0

- **Instance-based Configure()** - The `Configure()` method is now an instance method (not static)
- **Method Groups** - Use method groups directly instead of `nameof()` for cleaner, more refactor-safe code
- **Stricter DSL Purity** - Compile-time enforcement prevents lambdas, expressions, and field access in DSL

## Core Design Principles

1. **Zero lambdas** - Only method groups and literal values allowed
2. **Zero imperative logic** - The `Configure()` method contains only declarative DSL calls
3. **Compile-time only** - All DSL methods are no-ops at runtime; the source generator processes them
4. **Type safety** - Full IntelliSense support and compile-time validation
5. **Parity with attributes** - Every feature available via attributes is available via Fluent API

## Basic State Machine Structure

Every Fluent API state machine requires:
1. The `[StateMachine]` attribute to specify state and trigger types (add `EnableHierarchy = true` for HSM)
2. An instance `Configure()` method containing the DSL definition (must be `private`, parameterless, non-virtual)
3. State and trigger enums
4. Handler methods for guards, actions, and callbacks

### Minimal Example

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleMachine
{
    public enum State { Idle, Active, Done }
    public enum Trigger { Start, Complete }

    private void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start).GoTo(State.Active)
        .State(State.Active)
            .On(Trigger.Complete).GoTo(State.Done)
        .State(State.Done);
}
```

**Important:**
- `Configure()` must be an instance method (not static) as of v0.8.0
- `.State()` in the chain does NOT use generic type parameters. Only the initial `FSM.State()` can optionally be generic for type inference.

## Complete DSL Reference

### State Machine Configuration Methods

| Method | Description | Example |
|--------|-------------|---------|
| `.OnException(ExceptionHandler handler)` | Set exception handler | `.OnException(HandleException)` |

Note on OnException:
- Global handler: Applies to the entire machine (not per-state).
- Order-independent: May appear at the beginning, middle, or end of the chain.
- Single handler: Exactly one `OnException` is allowed; duplicates produce a compile-time diagnostic.
- Allowed signatures:
  - Sync: `ExceptionDirective Handle(ExceptionContext<TState, TTrigger> ctx)`
  - Async: `ValueTask<ExceptionDirective> Handle(ExceptionContext<TState, TTrigger> ctx)`
  - Both may optionally take `CancellationToken` as the second parameter.

### State Definition Methods

| Method | Description | Example |
|--------|-------------|---------|
| `.State(TState state)` | Define a state | `.State(State.Idle)` |
| `.OnEntry(Entry cb)` | Set entry callback | `.OnEntry(OnIdleEntry)` |
| `.OnEntryAsync(EntryAsync cb)` | Set async entry callback | `.OnEntryAsync(OnIdleEntryAsync)` |
| `.OnExit(Exit cb)` | Set exit callback | `.OnExit(OnIdleExit)` |
| `.OnExitAsync(ExitAsync cb)` | Set async exit callback | `.OnExitAsync(OnIdleExitAsync)` |

### HSM-Specific Methods (Hierarchical State Machines)

| Method | Description | Example |
|--------|-------------|---------|
| `.ChildOf(TState parent)` | Define parent-child relationship | `.ChildOf(State.Parent)` |
| `.Initial(TState child)` | Set initial substate | `.Initial(State.Parent_Child1)` |
| `.HistoryShallow()` | Enable shallow history mode | `.HistoryShallow()` |
| `.HistoryDeep()` | Enable deep history mode | `.HistoryDeep()` |

### Transition Definition Methods

| Method | Description | Example |
|--------|-------------|---------|
| `.On(TTrigger trigger)` | Start transition definition | `.On(Trigger.Start)` |
| `.OnInternal(TTrigger trigger)` | Start internal transition | `.OnInternal(Trigger.Update)` |

### Transition Modifiers

**Recommended order:** Payload → Guard → Action → Priority → GoTo

| Method | Description | Example |
|--------|-------------|---------|
| `.Payload<T>()` | Specify payload type | `.Payload<OrderData>()` |
| `.Guard(Guard cb)` | Add guard condition | `.Guard(CanStart)` |
| `.Guard(GuardAsync cb)` | Add async guard | `.Guard(CanStartAsync)` |
| `.Action(Act cb)` | Add transition action | `.Action(ProcessStart)` |
| `.Action(ActAsync cb)` | Add async action | `.Action(ProcessStartAsync)` |
| `.Priority(int priority)` | Set transition priority (HSM) | `.Priority(100)` |

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

    private void Configure() => FSM
        .State(State.Idle)
            .OnEntry(PrepareSystem)
            .On(Trigger.Start)
                .Guard(CanStart)  // Method group - clean syntax!
                .Action(StartProcess)
                .GoTo(State.Running)
        .State(State.Running)
            .On(Trigger.Stop)
                .Action(StopProcess)
                .GoTo(State.Stopped)
            .On(Trigger.Pause)
                .GoTo(State.Idle)
        .State(State.Stopped)
            .OnExit(Cleanup);

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

    private void Configure() => FSM
        .State(State.New)
            .On(Trigger.Process)
                .Guard(ValidateOrder)  // Method group
                .Action(ProcessOrder)  // Method group
                .GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Ship)
                .Action(ShipOrder)
                .GoTo(State.Shipped)
            .On(Trigger.Cancel)
                .Action(CancelOrder)
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

    private void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Submit)
                .Payload<SubmitRequest>()
                .Action(HandleSubmit)  // Method group
                .GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Update)
                .Payload<UpdateData>()
                .Action(HandleUpdate)
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

    private void Configure() => FSM
        .State(State.Disconnected)
            .OnEntryAsync(CleanupAsync)
            .On(Trigger.Connect)
                .Guard(CanConnectAsync)  // Async guard via method group
                .Action(StartConnectionAsync)  // Async action via method group
                .GoTo(State.Connecting)
        .State(State.Connecting)
            .On(Trigger.ConnectionEstablished)
                .Action(FinalizeConnectionAsync)
                .GoTo(State.Connected)
        .State(State.Connected)
            .OnExitAsync(DisconnectAsync)
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

### Exception Handling

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class ExceptionHandlingMachine
{
    public enum State { Idle, Processing, Failed, Done }
    public enum Trigger { Start, Complete, Retry }

    private void Configure() => FSM
        .OnException(HandleException)  // Set exception handler via method group
        .State(State.Idle)
            .On(Trigger.Start)
                .Action(StartProcessing)
                .GoTo(State.Processing)
        .State(State.Processing)
            .OnEntry(OnProcessingEntry)
            .On(Trigger.Complete)
                .GoTo(State.Done)
        .State(State.Failed)
            .On(Trigger.Retry)
                .GoTo(State.Idle)
        .State(State.Done);

    private void StartProcessing()
    {
        // May throw exception
        if (!IsSystemReady())
            throw new InvalidOperationException("System not ready");
    }

    private void OnProcessingEntry()
    {
        // May throw exception
        ValidateConfiguration();
    }

    // Exception handler signature options:
    // 1. Synchronous: ExceptionDirective HandleException(ExceptionContext<State, Trigger> ctx)
    // 2. Synchronous with token: ExceptionDirective HandleException(ExceptionContext<State, Trigger> ctx, CancellationToken ct)
    // 3. Async: ValueTask<ExceptionDirective> HandleExceptionAsync(ExceptionContext<State, Trigger> ctx)
    // 4. Async with token: ValueTask<ExceptionDirective> HandleExceptionAsync(ExceptionContext<State, Trigger> ctx, CancellationToken ct)

    private ExceptionDirective HandleException(ExceptionContext<State, Trigger> ctx)
    {
        // Log the exception
        Console.WriteLine($"Exception in {ctx.Stage} during {ctx.From} -> {ctx.To}: {ctx.Exception.Message}");

        // Decide whether to continue or propagate
        if (ctx.Exception is InvalidOperationException && ctx.Stage == TransitionStage.Action)
        {
            // Continue execution, swallow the exception
            return ExceptionDirective.Continue;
        }

        // Propagate the exception to the caller
        return ExceptionDirective.Propagate;
    }

    private bool IsSystemReady() => DateTime.Now.Second % 2 == 0; // Example condition
    private void ValidateConfiguration() { /* validation logic */ }
}
```

**Exception Handler Requirements:**
- Must accept `ExceptionContext<TState, TTrigger>` as first parameter
- Can optionally accept `CancellationToken` as second parameter
- Must return `ExceptionDirective` or `ValueTask<ExceptionDirective>`
- Can be synchronous or asynchronous
- Exactly one global handler per machine is supported; multiple declarations result in a compile-time error.

**ExceptionDirective Values:**
- `Continue` - Swallow the exception and continue execution
- `Propagate` - Re-throw the exception to the caller

**ExceptionContext Properties:**
- `From` - The source state of the transition
- `To` - The target state of the transition
- `Trigger` - The trigger that initiated the transition
- `Exception` - The exception that was thrown
- `Stage` - Where the exception occurred (Guard, OnExit, OnEntry, Action)
- `StateAlreadyChanged` - Whether the state was already changed when exception occurred

**Important Notes:**
- `OperationCanceledException` is always propagated, even if handler returns `Continue`
- Exception handlers are called for all exceptions except `OperationCanceledException`
- If no exception handler is defined, exceptions are propagated by default

### Internal Transitions

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class InternalTransitionMachine
{
    public enum State { Active, Inactive }
    public enum Trigger { Update, Toggle }

    public sealed class UpdateData { public int Value { get; init; } }

    private int _counter;

    private void Configure() => FSM
        .State(State.Active)
            .OnInternal(Trigger.Update)  // Use OnInternal for internal transitions
                .Payload<UpdateData>()
                .Guard(ValidateUpdate)  // Method group
                .Action(ApplyUpdate)
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

### Hierarchical State Machine (HSM)

```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class HierarchicalMachine
{
    public enum State
    {
        Idle,
        Processing,
        Processing_Initialization,
        Processing_Work,
        Processing_Cleanup,
        Done
    }
    public enum Trigger { Start, Next, Finish, Abort }

    private void Configure() => FSM
        // Parent state with history and initial child
        .State(State.Processing)
            .Initial(State.Processing_Initialization)
            .HistoryShallow()  // or .HistoryDeep() for deep history
            .OnEntry(OnProcessingEntry)
            .OnExit(OnProcessingExit)
            .On(Trigger.Abort).GoTo(State.Idle)

        // Child states
        .State(State.Processing_Initialization)
            .ChildOf(State.Processing)
            .On(Trigger.Next).GoTo(State.Processing_Work)

        .State(State.Processing_Work)
            .ChildOf(State.Processing)
            .On(Trigger.Next).GoTo(State.Processing_Cleanup)

        .State(State.Processing_Cleanup)
            .ChildOf(State.Processing)
            .On(Trigger.Finish).GoTo(State.Done)

        // Simple states
        .State(State.Idle)
            .On(Trigger.Start).GoTo(State.Processing)

        .State(State.Done);

    private void OnProcessingEntry() { /* Called when entering Processing or any child */ }
    private void OnProcessingExit() { /* Called when exiting Processing to non-child */ }
}
```

### HSM with Priority-based Transition Resolution

```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class PriorityMachine
{
    private void Configure() => FSM
        .State(State.Parent)
            .On(Trigger.Action)
                .Guard(HighPriorityCondition)  // Method group
                .Priority(100)  // Higher priority, evaluated first
                .GoTo(State.HighPriorityTarget)
            .On(Trigger.Action)
                .Guard(MediumPriorityCondition)  // Method group
                .Priority(50)
                .GoTo(State.MediumPriorityTarget)
            .On(Trigger.Action)
                .Priority(10)  // Lower priority, default fallback
                .GoTo(State.DefaultTarget);

    private bool HighPriorityCondition() => /* check condition */;
    private bool MediumPriorityCondition() => /* check condition */;
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

## What's Legal in Configure()

The `Configure()` method is processed at compile-time by the source generator. To ensure proper analysis, only these constructs are allowed:

### ✅ Allowed

- **Method groups** to instance methods: `.Guard(CanProcess)`, `.Action(DoWork)`
- **Literal values**: `.State(State.Idle)`, `.Priority(100)`
- **Enum values**: `.On(Trigger.Start)`, `.GoTo(State.Active)`
- **Generic type arguments**: `.Payload<OrderData>()`

### ❌ Not Allowed (Compile Error)

- **Lambdas**: `.Guard(() => _count > 0)` → FSM3075
- **Expressions**: `.Priority(_maxRetries)` → FSM3076
- **Method invocations**: `.GoTo(CalculateNextState())` → FSM3077
- **Property access**: `.Guard(IsReady)` where `IsReady` is a property → FSM3072
- **External methods**: `.Guard(Helper.Validate)` → FSM3073
- **Field access**: `.Priority(MaxPriority)` where `MaxPriority` is a field → FSM3076

## Dealing with Ambiguous Method Groups

When you have overloaded methods, the compiler may report FSM3070:

```csharp
// Problem: Multiple overloads exist
private bool Validate() => true;
private bool Validate(OrderData data) => data.Amount > 0;

.Guard(Validate)  // ERROR FSM3070: Ambiguous method group

// Solution 1: Rename methods to be unique
private bool ValidateSimple() => true;
private bool ValidateOrder(OrderData data) => data.Amount > 0;

// Solution 2: Use nameof as last resort (not recommended)
.Guard(nameof(Validate))  // Selects the parameterless version
```

## Important Notes

### Configure() Method Requirements

The `Configure()` method must be:
- **Instance method** (not static) - FSM3081d
- **Private** - FSM3081a
- **Parameterless** - FSM3081b
- **Non-virtual/non-override** - FSM3081c
- **Defined in the partial class** (not inherited) - FSM3082
- **Not a partial method** - FSM3083

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
2. HSM modifiers (`.ChildOf()`, `.Initial()`, `.HistoryShallow()`, `.HistoryDeep()`)
3. State callbacks (`.OnEntry()`, `.OnExit()`)
4. Transition trigger (`.On()` or `.OnInternal()`)
5. Payload specification (`.Payload<T>()`)
6. Guard (`.Guard()`)
7. Action (`.Action()`)
8. Priority (`.Priority()`) - must be before `.GoTo()`
9. Target (`.GoTo()` or `.Internal()`)

### Parser Behavior

- The FluentParser processes the `Configure()` method at compile time
- It generates the same `StateMachineModel` as the attribute-based parser
- Both approaches produce identical runtime code
- Mixing attributes and Fluent API in the same class is not supported
- Duplicate transitions with the same (FromState, Trigger, Priority) tuple are automatically deduplicated (first wins)

## Migration from v0.7.x

### Before (v0.7.x with nameof)
```csharp
private static void Configure() => FSM  // static method
    .State(State.A)
        .OnEntry(nameof(OnAEntry))
        .On(Trigger.Next)
            .Guard(nameof(CanGo))
            .Action(nameof(DoTransition))
            .GoTo(State.B);

private bool CanGo() => true;
private void OnAEntry() { }
private void DoTransition() { }
```

### After (v0.8.0 with method groups)
```csharp
private void Configure() => FSM  // instance method
    .State(State.A)
        .OnEntry(OnAEntry)  // method group
        .On(Trigger.Next)
            .Guard(CanGo)  // method group
            .Action(DoTransition)  // method group
            .GoTo(State.B);

private bool CanGo() => true;
private void OnAEntry() { }
private void DoTransition() { }
```

## Troubleshooting

### Common Errors

**FSM3070: Ambiguous method group reference**
- Multiple method overloads exist with compatible signatures
- Solution: Rename methods to be unique or use `nameof()` as last resort

**FSM3071: Impure DSL - expression not allowed**
- You're using an expression, conditional, or computation in DSL
- Solution: Use only method groups and literals

**FSM3072: Property or indexer used where method expected**
- A property was provided where a method is required
- Solution: Create a method that returns the property value

**FSM3073: External method group not allowed**
- Method from another class or instance
- Solution: Create a wrapper method in your state machine class

**FSM3075: Lambda expression not allowed**
- Lambda expressions are forbidden in DSL
- Solution: Extract to a named method

**FSM3076: Field or property access in DSL**
- Reading a field or property value
- Solution: Use a literal value instead

**FSM3077: Method invocation in DSL**
- Calling a method to compute a value
- Solution: Use compile-time constants

**FSM3081a-d: Invalid Configure method signature**
- Configure must be private, instance, parameterless, non-virtual
- Solution: Fix the method signature

**FSM3082: Configure inherited from base class**
- Configure must be defined in the partial class itself
- Solution: Define Configure in your partial class

## FAQ

**Q: Why must `Configure()` be an instance method?**
A: This allows natural use of method groups for instance methods, eliminating the need for `nameof()` and providing better refactoring support.

**Q: Can I use DI in `Configure()`?**
A: No. `Configure()` is compile-time only. Use DI in runtime callbacks (actions/entry/exit).

**Q: Does instance `Configure()` affect performance?**
A: No. DSL is not executed at runtime; generated code remains zero-allocation.

**Q: Can I have multiple `Configure()` methods?**
A: No. Exactly one `Configure` method per type is allowed.

**Q: How do I handle conditional logic in DSL?**
A: Use multiple transitions with guards and priorities, or implement the logic in guard methods.

## Best Practices

1. **Keep Configure() pure** - No business logic, only DSL calls
2. **Use meaningful names** - State and trigger names should be self-documenting
3. **Prefer unique method names** - Avoid overloads to prevent ambiguity
4. **One transition per line** - Format for readability
5. **Group related states** - Keep related state definitions together
6. **Test both paths** - Ensure guard true/false paths are tested
7. **Handle cancellation** - Always respect CancellationToken in async methods
8. **Avoid `nameof()`** - Use method groups for cleaner, refactor-safe code

## Version History

- **v0.8.0** - Instance-based Configure, method groups, stricter DSL purity:
  - `Configure()` is now an instance method (not static)
  - Method groups replace `nameof()` for all callbacks
  - Compile-time enforcement of DSL purity (no lambdas/expressions)
  - Full exception handling support via `.OnException()`
  - Improved diagnostics (FSM3070-FSM3084)

- **v0.7.5** - Initial Fluent API release with full FSM and HSM support:
  - Parent-child state relationships via `.ChildOf()` method
  - Initial substates via `.Initial()` method
  - History modes via `.HistoryShallow()` and `.HistoryDeep()` methods
  - Priority support for transition resolution via `.Priority()` method
  - Full parity with attribute-based HSM definitions

---

*For more examples and test cases, see the FastFsm.Tests project, particularly the FluentAPISpecificTests and FluentAPIComparisonTests classes.*