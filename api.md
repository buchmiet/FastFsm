# FastFsm API Reference

FastFsm is a high-performance, AOT-friendly state machine generator for .NET that uses source generators to create efficient FSM implementations from attribute-decorated classes.

## Table of Contents
1. [Core Concepts](#core-concepts)
2. [Attributes](#attributes)
3. [Transition Execution Order](#transition-execution-order)
4. [Callback Methods](#callback-methods)
5. [Public API](#public-api)
6. [CancellationToken Propagation](#cancellationtoken-propagation)
7. [Exception Policy](#exception-policy)
8. [OnException & ExceptionDirective](#onexception--exceptiondirective)
9. [Validation Rules](#validation-rules)
10. [Examples](#examples)

## Core Concepts

### State Machine Types
- **Sync Machines**: Synchronous state machines with `IStateMachine<TState, TTrigger>` interface
- **Async Machines**: Asynchronous state machines with `IAsyncStateMachine<TState, TTrigger>` interface

### Variants
- **Pure**: No OnEntry/OnExit callbacks, minimal footprint
- **Core**: Basic callbacks without payload support
- **Payload**: Single payload type support with typed API
- **MultiPayload**: Different payload types per trigger

## Attributes

### `[StateMachine]`
Marks a partial class as a state machine.

```csharp
[StateMachine(typeof(StateEnum), typeof(TriggerEnum))]
public partial class MyStateMachine { }
```

**Properties:**
- `StateType` (required): Enum type for states
- `TriggerType` (required): Enum type for triggers
- `DefaultPayloadType`: Default payload type for all triggers
- `GenerateStructuralApi`: Generate HasTransition/GetDefinedTriggers methods (default: false)
- `ContinueOnCapturedContext`: Control async continuation context (default: false)
- `GenerateExtensibleVersion`: Generate extensible variant (default: true)

### `[State]`
Defines state callbacks (OnEntry/OnExit).

```csharp
[State(States.Processing, OnEntry = nameof(OnEnterProcessing), OnExit = nameof(OnExitProcessing))]
private void ConfigureStates() { }
```

**Properties:**
- `State` (required): The state enum value
- `OnEntry`: Method name for entry callback
- `OnExit`: Method name for exit callback

### `[Transition]`
Defines state transitions.

```csharp
[Transition(States.Initial, Triggers.Start, States.Processing, 
    Guard = nameof(CanStart), Action = nameof(DoStart))]
private void ConfigureTransitions() { }
```

**Properties:**
- `FromState` (required): Source state
- `Trigger` (required): Trigger that initiates transition
- `ToState` (required): Target state
- `Guard`: Method name for guard condition
- `Action`: Method name for transition action

### `[InternalTransition]`
Defines internal transitions (no state change).

```csharp
[InternalTransition(States.Processing, Triggers.Update, nameof(UpdateData))]
private void ConfigureInternalTransitions() { }
```

**Properties:**
- `State` (required): Current state
- `Trigger` (required): Trigger for internal transition
- `Action` (required): Method name for action
- `Guard`: Method name for guard condition

### `[PayloadType]`
Specifies payload types for the state machine.

```csharp
// Default payload for all triggers
[PayloadType(typeof(MyPayload))]

// Trigger-specific payload
[PayloadType(Triggers.Process, typeof(ProcessData))]
[PayloadType(Triggers.Complete, typeof(CompletionData))]
public partial class MyStateMachine { }
```

## Transition Execution Order

For successful transitions, callbacks execute in this specific order:

1. **Guard** evaluation
2. **OnExit** (from current state) - if transition fails here, state remains unchanged
3. **State Change** - state is updated to target state
4. **OnEntry** (to new state) - exceptions propagate to caller
5. **Action** - exceptions propagate to caller

For internal transitions, only Guard → Action is executed (no state change, no OnEntry/OnExit).

## Callback Methods

### Guard Methods
Return `bool` (sync) or `ValueTask<bool>` (async) to control transition execution.

```csharp
// Synchronous guard
private bool CanProcess() => _isReady;

// Asynchronous guard
private async ValueTask<bool> CanProcessAsync() 
{
    return await CheckConditionsAsync();
}

// Guard with payload
private bool CanProcess(ProcessData data) => data.IsValid;

// Guard with CancellationToken
private async ValueTask<bool> CanProcessAsync(CancellationToken cancellationToken)
{
    return await CheckAsync(cancellationToken);
}

// Guard with payload and CancellationToken
private async ValueTask<bool> CanProcessAsync(ProcessData data, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return data.IsValid && await CheckAsync(cancellationToken);
}
```

### Action, OnEntry, OnExit Methods
Return `void` (sync) or `Task`/`ValueTask` (async).

```csharp
// Synchronous action
private void ProcessData() { }

// Asynchronous action
private async Task ProcessDataAsync() 
{
    await PerformOperationAsync();
}

// Action with payload
private void ProcessData(ProcessPayload payload) { }

// Action with CancellationToken
private async Task ProcessDataAsync(CancellationToken cancellationToken)
{
    await PerformOperationAsync(cancellationToken);
}

// Action with payload and CancellationToken
private async ValueTask ProcessDataAsync(ProcessPayload payload, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await ProcessAsync(payload, cancellationToken);
}
```

### Overload Resolution
The generator intelligently selects the best overload based on available parameters:

1. **Priority order**: (Payload, CancellationToken) → (Payload) → (CancellationToken) → ()
2. **Async machine with sync callback**: Allowed, executes synchronously
3. **Sync machine with async callback**: Error FSM011

### Signature Matrix

| Callback Type | Sync Return | Async Return | Parameters |
|--------------|-------------|--------------|------------|
| Guard | `bool` | `ValueTask<bool>` | (), (T), (CT), (T, CT) |
| Action | `void` | `Task`/`ValueTask` | (), (T), (CT), (T, CT) |
| OnEntry | `void` | `Task`/`ValueTask` | (), (T), (CT), (T, CT) |
| OnExit | `void` | `Task`/`ValueTask` | (), (T), (CT), (T, CT) |

Where: T = payload type, CT = CancellationToken

## Public API

### IStateMachine<TState, TTrigger> (Sync)
```csharp
public interface IStateMachine<TState, TTrigger>
{
    TState CurrentState { get; }
    bool TryFire(TTrigger trigger, object? payload = null);
    void Fire(TTrigger trigger, object? payload = null);
    bool CanFire(TTrigger trigger);
    IReadOnlyList<TTrigger> GetPermittedTriggers();
}
```

### IAsyncStateMachine<TState, TTrigger> (Async)
```csharp
public interface IAsyncStateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
{
    ValueTask<bool> TryFireAsync(TTrigger trigger, object? payload = null, 
        CancellationToken cancellationToken = default);
    ValueTask FireAsync(TTrigger trigger, object? payload = null, 
        CancellationToken cancellationToken = default);
    ValueTask<bool> CanFireAsync(TTrigger trigger, 
        CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(
        CancellationToken cancellationToken = default);
}
```

### IStateMachineWithPayload<TState, TTrigger, TPayload>
```csharp
public interface IStateMachineWithPayload<TState, TTrigger, TPayload> 
    : IStateMachine<TState, TTrigger>
{
    bool TryFire(TTrigger trigger, TPayload payload);
    void Fire(TTrigger trigger, TPayload payload);
    bool CanFire(TTrigger trigger, TPayload payload);
    
    // Async variants for async machines
    ValueTask<bool> TryFireAsync(TTrigger trigger, TPayload payload, 
        CancellationToken cancellationToken = default);
    ValueTask FireAsync(TTrigger trigger, TPayload payload, 
        CancellationToken cancellationToken = default);
    ValueTask<bool> CanFireAsync(TTrigger trigger, TPayload payload, 
        CancellationToken cancellationToken = default);
}
```

### Structural API (when GenerateStructuralApi = true)
```csharp
bool HasTransition(TState fromState, TTrigger trigger);
IEnumerable<TTrigger> GetDefinedTriggers(TState fromState);
```

## CancellationToken Propagation

### Key Principles
1. **ThrowIfCancellationRequested()** is called at the start of all public async methods
2. **CancellationToken is propagated** to all async callbacks that accept it
3. **OperationCanceledException is never caught** - it propagates to the caller
4. **treatCancellationAsFailure = true** in all guard evaluations

### Implementation Details

#### Public API Methods
```csharp
public async ValueTask<bool> TryFireAsync(TTrigger trigger, 
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested(); // First line
    // ... implementation
}
```

#### Callback Invocation
```csharp
// If callback has (CancellationToken) or (T, CancellationToken) overload
await CallbackAsync(cancellationToken).ConfigureAwait(false);

// Exception filter ensures OperationCanceledException propagates
catch (Exception ex) when (ex is not OperationCanceledException)
{
    // Handle other exceptions
}
```

#### Initial State OnEntry
Constructor uses `CancellationToken.None` for initial OnEntry since constructors can't be async:
```csharp
_ = Task.Run(async () => {
    await OnEntryInitial(CancellationToken.None).ConfigureAwait(false);
});
```

## Exception Policy

### Current Behavior (as implemented)
1. **Guard exceptions**: Caught, treated as returning false, transition fails
2. **OnExit exceptions**: Caught, transition fails, state unchanged
3. **OnEntry exceptions**: **Propagate to caller**, state already changed
4. **Action exceptions**: **Propagate to caller**, state already changed
5. **OperationCanceledException**: Always propagates, never caught

### Exception Propagation Matrix

| Callback | Exception Type | Behavior | State Impact |
|----------|---------------|----------|--------------|
| Guard | Any except OCE | Caught, returns false | No change |
| Guard | OperationCanceledException | Propagates | No change |
| OnExit | Any except OCE | Caught, transition fails | No change |
| OnExit | OperationCanceledException | Propagates | No change |
| OnEntry | Any | Propagates | Already changed |
| Action | Any | Propagates | Already changed |

### Example
```csharp
// State: Initial
try 
{
    await machine.FireAsync(Triggers.Process); 
    // If OnEntry or Action throws, exception propagates here
    // BUT state is already changed to Processing
}
catch (InvalidOperationException ex)
{
    // Handle exception
    // Note: machine.CurrentState is now Processing (not Initial)
}
```

**Uwaga:** zachowanie wyjątków w `OnEntry` i `Action` może zostać zmodyfikowane przez `OnException` (patrz rozdział *OnException & ExceptionDirective*); `OperationCanceledException` zawsze propaguje.

## OnException & ExceptionDirective

`OnException` to opcjonalny hook wywoływany przez wygenerowany kod **tylko** w dwóch miejscach:

* podczas **OnEntry** (po zmianie stanu),
* podczas **Action** (po zmianie stanu).

Hook pozwala zdecydować, czy wyjątek ma zostać **zpropagowany** do wywołującego, czy też **połknięty i zignorowany**, co umożliwia kontynuację pracy maszyny (np. w scenariuszach IoT z błędami przejściowymi).

> **Ważne:** `OperationCanceledException` **zawsze propaguje** (nie jest przechwytywany ani maskowany).

### Kiedy hook **nie działa**

* **Guard**: wyjątki są traktowane jak wynik `false`; hook nie jest wywoływany i nie może zmienić rezultatu guarda.
* **OnExit**: wyjątek przerywa przejście; hook nie jest wywoływany.
* **Początkowy OnEntry wywoływany w konstruktorze** (inicjalizacja stanu startowego): nie jest otoczony polityką `OnException`.

### Atrybut

```csharp
using Abstractions.Attributes;

[OnException(nameof(HandleException))]
public partial class MyMachine { /* ... */ }
```

### Kontekst i dyrektywa

Hook przyjmuje silnie typowany kontekst i zwraca dyrektywę:

```csharp
using StateMachine.Exceptions;

public enum ExceptionDirective
{
    Propagate, // przekaż wyjątek do wywołującego
    Continue   // połknij wyjątek i kontynuuj
}

public readonly struct ExceptionContext<TState, TTrigger>
{
    public TState From { get; }
    public TState To { get; }
    public TTrigger Trigger { get; }
    public Exception Exception { get; }
    public TransitionStage Stage { get; }       // OnEntry | Action
    public bool StateAlreadyChanged { get; }    // OnEntry/Action = true
}
```

### Dozwolone sygnatury hooka

Generator akceptuje **jedną** z poniższych sygnatur (priorytet wyboru w tej kolejności):

1. `ValueTask<ExceptionDirective> Handle(ExceptionContext<TState, TTrigger> ctx, CancellationToken ct)`
2. `ValueTask<ExceptionDirective> Handle(ExceptionContext<TState, TTrigger> ctx)`
3. `ExceptionDirective Handle(ExceptionContext<TState, TTrigger> ctx)`

> Maszyna **async** może mieć hook **sync** lub **async**.
> Maszyna **sync** nie może mieć hooka **async** (diagnostyka FSM011).

### Semantyka

* **OnEntry/Action**

  * `Continue` → wyjątek jest **połykany**, maszyna **pozostaje** w nowym stanie i kontynuuje wykonanie.
  * `Propagate` → wyjątek jest **propagowany** do wywołującego (stan już zmieniony).
* **Guard / OnExit** – hook nie jest stosowany.
* **OperationCanceledException** – zawsze propaguje (dyrektywa ignorowana).

#### Macierz zachowania z `OnException` (tylko miejsca, gdzie działa)

| Stage   | Dyrektywa | Efekt                           | Wpływ na stan |
| ------- | --------- | ------------------------------- | ------------- |
| OnEntry | Continue  | Połknięcie wyjątku, kontynuacja | Już zmieniony |
| OnEntry | Propagate | Wyjątek do wywołującego         | Już zmieniony |
| Action  | Continue  | Połknięcie wyjątku, kontynuacja | Już zmieniony |
| Action  | Propagate | Wyjątek do wywołującego         | Już zmieniony |

### Przykłady

#### Sync: kontynuacja dla błędów przejściowych (IoT)

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
[OnException(nameof(HandleException))]
public partial class DeviceMachine
{
    [Transition(State.Idle, Trigger.Start, State.Running, Action = nameof(DoWork))]
    [State(State.Running, OnEntry = nameof(OnEnterRunning))]
    private void Configure() { }

    private void OnEnterRunning() { /* może rzucić IOException */ }
    private void DoWork() { /* może rzucić TransientDeviceException */ }

    private ExceptionDirective HandleException(ExceptionContext<State, Trigger> ctx)
        => ctx.Exception switch
        {
            TransientDeviceException => ExceptionDirective.Continue, // połknij i jedź dalej
            IOException io when IsRecoverable(io) => ExceptionDirective.Continue,
            _ => ExceptionDirective.Propagate
        };

    private static bool IsRecoverable(IOException _) => true;
}
```

#### Async: hook z `CancellationToken`

```csharp
[OnException(nameof(HandleExceptionAsync))]
public partial class MyAsyncMachine
{
    private async ValueTask<ExceptionDirective> HandleExceptionAsync(
        ExceptionContext<State, Trigger> ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Telemetry.WriteAsync(ctx, ct);
        return ctx.Exception is TimeoutException
            ? ExceptionDirective.Continue
            : ExceptionDirective.Propagate;
    }
}
```

### Dobre praktyki

* Używaj `Continue` **tylko** dla dobrze rozpoznanych, przejściowych wyjątków (np. I/O, sieć, sprzęt).
* Zawsze loguj kontekst błędu (From/To/Trigger/Stage), aby nie maskować degradacji.
* Nie zmieniaj logiki gardów przez `OnException` — inwarianty maszyny powinny pozostać nienaruszone.

## Validation Rules

### Compile-Time Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| FSM001 | Error | Missing [StateMachine] attribute |
| FSM002 | Error | Invalid types in [StateMachine] attribute |
| FSM003 | Error | Invalid callback method signature |
| FSM004 | Error | Invalid enum value in transition |
| FSM005 | Warning | Duplicate transition definition |
| FSM006 | Warning | Unreachable state detected |
| FSM007 | Error | Async void callback (use Task/ValueTask) |
| FSM008 | Error | Guard returns Task (use ValueTask<bool>) |
| FSM009 | Error | Force variant not implemented |
| FSM010 | Warning | Guard with payload in non-payload machine |
| FSM011 | Error | Async callback in sync machine |

### Common Validation Scenarios

#### Valid Callback Signatures
- Return type matches callback type (bool for guards, void for actions)
- Maximum 2 parameters (payload and/or CancellationToken)
- Async methods return Task/ValueTask (not async void)

#### State Machine Consistency
- All states must be reachable from initial state
- No duplicate transitions (same from-state + trigger combination)
- Payload types must be consistent for multi-payload machines

## Examples

### Basic Async State Machine
```csharp
[StateMachine(typeof(OrderStates), typeof(OrderTriggers))]
public partial class OrderStateMachine
{
    [Transition(OrderStates.New, OrderTriggers.Submit, OrderStates.Processing,
        Guard = nameof(CanSubmit), Action = nameof(ProcessOrder))]
    private void Configure() { }
    
    private async ValueTask<bool> CanSubmit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ValidateOrderAsync(cancellationToken);
    }
    
    private async Task ProcessOrder(CancellationToken cancellationToken)
    {
        await SubmitToSystemAsync(cancellationToken);
    }
}
```

### Payload State Machine with Overloads
```csharp
[StateMachine(typeof(States), typeof(Triggers))]
[PayloadType(typeof(ProcessData))]
public partial class DataProcessor
{
    [State(States.Processing, OnEntry = nameof(OnEnterProcessing))]
    [Transition(States.Ready, Triggers.Process, States.Processing,
        Guard = nameof(CanProcess), Action = nameof(DoProcess))]
    private void Configure() { }
    
    // Overloaded callbacks - generator picks best match
    private bool CanProcess() => true; // Fallback
    private async ValueTask<bool> CanProcess(ProcessData data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return data.IsValid && await CheckResourcesAsync(ct);
    }
    
    private async Task OnEnterProcessing()
    {
        await InitializeAsync();
    }
    
    private async Task OnEnterProcessing(ProcessData data)
    {
        await InitializeWithDataAsync(data);
    }
    
    private async ValueTask DoProcess(ProcessData data, CancellationToken ct)
    {
        await ProcessDataAsync(data, ct);
    }
}
```

### Multi-Payload State Machine
```csharp
[StateMachine(typeof(States), typeof(Triggers))]
[PayloadType(Triggers.Configure, typeof(ConfigData))]
[PayloadType(Triggers.Process, typeof(ProcessData))]
[PayloadType(Triggers.Complete, typeof(ResultData))]
public partial class MultiPayloadMachine
{
    [Transition(States.Ready, Triggers.Configure, States.Configured,
        Action = nameof(ApplyConfig))]
    [Transition(States.Configured, Triggers.Process, States.Processing,
        Guard = nameof(CanProcess), Action = nameof(ProcessData))]
    [Transition(States.Processing, Triggers.Complete, States.Done,
        Action = nameof(StoreResult))]
    private void Configure() { }
    
    private async Task ApplyConfig(ConfigData config, CancellationToken ct)
    {
        await ConfigureSystemAsync(config, ct);
    }
    
    private async ValueTask<bool> CanProcess(ProcessData data)
    {
        return data.Items.Any();
    }
    
    private async Task ProcessData(ProcessData data, CancellationToken ct)
    {
        await ProcessItemsAsync(data.Items, ct);
    }
    
    private void StoreResult(ResultData result)
    {
        _results.Add(result);
    }
}
```

### Usage Examples
```csharp
// Create machine
var machine = new OrderStateMachine(OrderStates.New);

// Check if transition is possible
if (await machine.CanFireAsync(OrderTriggers.Submit, cancellationToken))
{
    // Fire trigger - exceptions from OnEntry/Action propagate
    try
    {
        await machine.FireAsync(OrderTriggers.Submit, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        // Handle non-cancellation exceptions
        // Note: state may have already changed
    }
}

// Get permitted triggers
var triggers = await machine.GetPermittedTriggersAsync(cancellationToken);

// With payload
var processor = new DataProcessor(States.Ready);
var data = new ProcessData { Items = items };

await processor.FireAsync(Triggers.Process, data, cancellationToken);
```