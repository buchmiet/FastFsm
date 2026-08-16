# FastFSM Legacy API Documentation (Attribute-Based)

## Overview

The Legacy API (also known as the Attribute-Based API) is the original way to define state machines in FastFSM. It uses C# attributes to declaratively specify states, transitions, guards, actions, and callbacks. While the newer Fluent API offers improved readability and refactoring support, the Legacy API remains fully supported and generates identical high-performance runtime code.

> **Note:** Both the Legacy API and Fluent API are compile-time only. The source generator processes your declarations and generates optimized code with zero runtime overhead.

## Core Concepts

The Legacy API uses attributes to define state machine behavior:
- **[StateMachine]** - Marks a partial class as a state machine
- **[State]** - Configures state-specific behavior (entry/exit callbacks)
- **[Transition]** - Defines state transitions with optional guards and actions
- **[InternalTransition]** - Defines transitions that execute actions without changing state
- **[OnException]** - Sets up exception handling for the state machine

## Basic State Machine Structure

Every Legacy API state machine requires:
1. The `[StateMachine]` attribute on a partial class
2. State and trigger enums
3. One or more methods decorated with `[Transition]` or `[State]` attributes
4. Handler methods referenced by name in the attributes

### Minimal Example

```csharp
// 1. Define your states and triggers as enums
public enum DoorState { Open, Closed, Locked }
public enum DoorTrigger { Open, Close, Lock, Unlock }

// 2. Declare your state machine class with the [StateMachine] attribute
[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    // 3. Define transitions using attributes on a dummy method
    [Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Open)]
    [Transition(DoorState.Open, DoorTrigger.Close, DoorState.Closed)]
    [Transition(DoorState.Closed, DoorTrigger.Lock, DoorState.Locked)]
    [Transition(DoorState.Locked, DoorTrigger.Unlock, DoorState.Closed)]
    private void ConfigureTransitions() { }
}

// 4. Use your state machine
var door = new DoorController(DoorState.Closed);
door.Start();  // Initialize state machine
door.Fire(DoorTrigger.Open);  // Transition: Closed -> Open
Console.WriteLine(door.CurrentState);  // Outputs: Open
```

## Complete Attribute Reference

### [StateMachine] Attribute

Marks a partial class as a state machine and specifies the state and trigger types.

```csharp
[StateMachine(typeof(TState), typeof(TTrigger))]
[StateMachine(typeof(TState), typeof(TTrigger),
    EnableHierarchy = true,
    DefaultPayloadType = typeof(TPayload),
    GenerateExtensibleVersion = true)]
```

**Parameters:**
- `TState` - Enum type defining possible states
- `TTrigger` - Enum type defining possible triggers

**Optional Properties:**
- `EnableHierarchy` - Enable hierarchical state machine features (default: false)
- `DefaultPayloadType` - Default payload type for all transitions
- `GenerateExtensibleVersion` - Enable extension hooks for logging/debugging (default: false)

### [State] Attribute

Configures state-specific behavior including entry/exit callbacks and hierarchical relationships.

```csharp
[State(TState state,
    OnEntry = "methodName",
    OnExit = "methodName",
    Parent = TState.ParentState,
    IsInitial = true,
    History = HistoryMode.Shallow)]
```

**Parameters:**
- `state` - The state to configure

**Optional Properties:**
- `OnEntry` - Method name to execute when entering the state
- `OnExit` - Method name to execute when leaving the state
- `Parent` - Parent state for hierarchical relationships (HSM only)
- `IsInitial` - Marks state as initial child of parent (HSM only, default: false)
- `History` - History mode: None, Shallow, or Deep (HSM only)

### [Transition] Attribute

Defines a state transition with optional guard conditions and actions.

```csharp
[Transition(TState fromState, TTrigger trigger, TState toState,
    Guard = "guardMethodName",
    Action = "actionMethodName",
    Priority = 0)]
```

**Parameters:**
- `fromState` - Source state
- `trigger` - Trigger that causes the transition
- `toState` - Destination state

**Optional Properties:**
- `Guard` - Method name that returns bool (transition only occurs if true)
- `Action` - Method name to execute during transition
- `Priority` - Transition priority for resolution order (higher = higher priority, default: 0)

### [InternalTransition] Attribute

Defines an internal transition that executes an action without changing state.

```csharp
[InternalTransition(TState state, TTrigger trigger,
    Guard = "guardMethodName",
    Action = "actionMethodName",
    Priority = 0)]
```

**Parameters:**
- `state` - State where the internal transition is active
- `trigger` - Trigger that causes the internal transition

**Optional Properties:**
- `Guard` - Method name that returns bool
- `Action` - Method name to execute
- `Priority` - Transition priority for resolution order

### [OnException] Attribute

Sets up global exception handling for the state machine.

```csharp
[OnException("exceptionHandlerMethodName")]
```

**Parameters:**
- Method name that handles exceptions

The exception handler must have one of these signatures:
- `ExceptionDirective HandleException(ExceptionContext<TState, TTrigger> ctx)`
- `ExceptionDirective HandleException(ExceptionContext<TState, TTrigger> ctx, CancellationToken ct)`
- `ValueTask<ExceptionDirective> HandleExceptionAsync(ExceptionContext<TState, TTrigger> ctx)`
- `ValueTask<ExceptionDirective> HandleExceptionAsync(ExceptionContext<TState, TTrigger> ctx, CancellationToken ct)`

## Common Patterns

### State Machine with Guards and Actions

```csharp
[StateMachine(typeof(AccountState), typeof(AccountTrigger))]
public partial class BankAccount
{
    private decimal _balance = 1000;

    [Transition(AccountState.Active, AccountTrigger.Withdraw, AccountState.Active,
        Guard = nameof(HasSufficientFunds),
        Action = nameof(DebitAccount))]
    [Transition(AccountState.Active, AccountTrigger.Deposit, AccountState.Active,
        Action = nameof(CreditAccount))]
    [Transition(AccountState.Active, AccountTrigger.Freeze, AccountState.Frozen)]
    [Transition(AccountState.Frozen, AccountTrigger.Unfreeze, AccountState.Active)]
    private void ConfigureTransitions() { }

    private bool HasSufficientFunds() => _balance >= 100;
    private void DebitAccount() => _balance -= 100;
    private void CreditAccount() => _balance += 100;
}

// Usage
var account = new BankAccount(AccountState.Active);
account.Start();

if (account.CanFire(AccountTrigger.Withdraw))
{
    account.Fire(AccountTrigger.Withdraw);
}
```

### State Machine with Entry/Exit Callbacks

```csharp
[StateMachine(typeof(ConnectionState), typeof(ConnectionTrigger))]
public partial class NetworkConnection
{
    private Timer? _heartbeatTimer;
    public List<string> EventLog { get; } = new();

    // Configure state behaviors
    [State(ConnectionState.Connected,
        OnEntry = nameof(StartHeartbeat),
        OnExit = nameof(StopHeartbeat))]
    [State(ConnectionState.Disconnected,
        OnEntry = nameof(OnDisconnectedEntry))]
    [State(ConnectionState.Connecting,
        OnEntry = nameof(OnConnectingEntry),
        OnExit = nameof(OnConnectingExit))]
    private void ConfigureStates() { }

    // Configure transitions
    [Transition(ConnectionState.Disconnected, ConnectionTrigger.Connect, ConnectionState.Connecting)]
    [Transition(ConnectionState.Connecting, ConnectionTrigger.Connected, ConnectionState.Connected)]
    [Transition(ConnectionState.Connected, ConnectionTrigger.Disconnect, ConnectionState.Disconnected)]
    [Transition(ConnectionState.Connecting, ConnectionTrigger.Failed, ConnectionState.Disconnected)]
    private void ConfigureTransitions() { }

    // Entry/Exit callbacks
    private void StartHeartbeat()
    {
        EventLog.Add("Heartbeat started");
        _heartbeatTimer = new Timer(_ => SendPing(), null, 0, 5000);
    }

    private void StopHeartbeat()
    {
        EventLog.Add("Heartbeat stopped");
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    private void OnDisconnectedEntry() => EventLog.Add("Disconnected");
    private void OnConnectingEntry() => EventLog.Add("Connecting...");
    private void OnConnectingExit() => EventLog.Add("Connection attempt finished");

    private void SendPing() { /* implementation */ }
}
```

### State Machine with Default Payload

```csharp
public sealed class OrderData
{
    public required string OrderId { get; init; }
    public decimal Amount { get; init; }
    public string? CustomerEmail { get; init; }
}

[StateMachine(typeof(OrderState), typeof(OrderTrigger),
    DefaultPayloadType = typeof(OrderData))]
public partial class OrderProcessor
{
    public string? LastProcessedOrderId { get; private set; }
    public decimal TotalProcessed { get; private set; }

    [Transition(OrderState.New, OrderTrigger.Process, OrderState.Processing,
        Guard = nameof(ValidateOrder),
        Action = nameof(StartProcessing))]
    [Transition(OrderState.Processing, OrderTrigger.Complete, OrderState.Completed,
        Action = nameof(CompleteOrder))]
    [Transition(OrderState.Processing, OrderTrigger.Cancel, OrderState.Cancelled,
        Action = nameof(CancelOrder))]
    private void ConfigureTransitions() { }

    // When DefaultPayloadType is set, handlers can accept the payload
    private bool ValidateOrder(OrderData order)
    {
        return order.Amount > 0 && !string.IsNullOrEmpty(order.OrderId);
    }

    private void StartProcessing(OrderData order)
    {
        LastProcessedOrderId = order.OrderId;
        Console.WriteLine($"Processing order {order.OrderId} for ${order.Amount}");
    }

    private void CompleteOrder(OrderData order)
    {
        TotalProcessed += order.Amount;
        Console.WriteLine($"Order {order.OrderId} completed");
    }

    private void CancelOrder()
    {
        Console.WriteLine("Order cancelled");
        // Note: This action doesn't use the payload
    }
}

// Usage
var processor = new OrderProcessor(OrderState.New);
processor.Start();

var order = new OrderData
{
    OrderId = "ORD-123",
    Amount = 99.99m,
    CustomerEmail = "customer@example.com"
};

processor.Fire(OrderTrigger.Process, order);
```

### Multiple Payload Types

When different transitions need different payload types, you can use the [PayloadType] attribute:

```csharp
public class SubmitRequest { public string Id { get; set; } }
public class ProcessData { public int ItemCount { get; set; } }
public class ValidationResult { public bool IsValid { get; set; } }

[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger))]
public partial class MultiPayloadWorkflow
{
    [Transition(WorkflowState.Idle, WorkflowTrigger.Submit, WorkflowState.Validating,
        Action = nameof(HandleSubmit))]
    [PayloadType(typeof(SubmitRequest))]
    private void ConfigureSubmit() { }

    [Transition(WorkflowState.Validating, WorkflowTrigger.Process, WorkflowState.Processing,
        Guard = nameof(IsValidResult),
        Action = nameof(StartProcessing))]
    [PayloadType(typeof(ValidationResult))]
    private void ConfigureValidation() { }

    [Transition(WorkflowState.Processing, WorkflowTrigger.Update, WorkflowState.Processing,
        Action = nameof(UpdateProgress))]
    [PayloadType(typeof(ProcessData))]
    private void ConfigureUpdate() { }

    private void HandleSubmit(SubmitRequest request) { /* implementation */ }
    private bool IsValidResult(ValidationResult result) => result.IsValid;
    private void StartProcessing(ValidationResult result) { /* implementation */ }
    private void UpdateProgress(ProcessData data) { /* implementation */ }
}
```

### Async State Machine

```csharp
[StateMachine(typeof(DownloadState), typeof(DownloadTrigger))]
public partial class FileDownloader
{
    private HttpClient _httpClient = new();
    public byte[]? DownloadedData { get; private set; }

    [State(DownloadState.Idle, OnEntry = nameof(OnIdleEntryAsync))]
    [State(DownloadState.Downloading,
        OnEntry = nameof(OnDownloadingEntryAsync),
        OnExit = nameof(OnDownloadingExitAsync))]
    private void ConfigureStates() { }

    [Transition(DownloadState.Idle, DownloadTrigger.Start, DownloadState.Downloading,
        Guard = nameof(CanDownloadAsync),
        Action = nameof(StartDownloadAsync))]
    [Transition(DownloadState.Downloading, DownloadTrigger.Complete, DownloadState.Completed)]
    [Transition(DownloadState.Downloading, DownloadTrigger.Cancel, DownloadState.Cancelled,
        Action = nameof(CancelDownloadAsync))]
    private void ConfigureTransitions() { }

    // Async callbacks
    private async Task OnIdleEntryAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        Console.WriteLine("Ready to download");
    }

    private async ValueTask<bool> CanDownloadAsync(CancellationToken ct)
    {
        // Check network connectivity
        await Task.Delay(10, ct);
        return true;
    }

    private async Task StartDownloadAsync(CancellationToken ct)
    {
        var url = "https://example.com/file.dat";
        DownloadedData = await _httpClient.GetByteArrayAsync(url, ct);
    }

    private async ValueTask OnDownloadingEntryAsync()
    {
        await Task.Delay(10);
        Console.WriteLine("Download started");
    }

    private async Task OnDownloadingExitAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
        Console.WriteLine("Download finished");
    }

    private async ValueTask CancelDownloadAsync()
    {
        await Task.Delay(10);
        DownloadedData = null;
    }
}

// Usage
var downloader = new FileDownloader(DownloadState.Idle);
await downloader.StartAsync();
await downloader.FireAsync(DownloadTrigger.Start);
```

### Exception Handling

```csharp
[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
[OnException(nameof(HandleException))]
public partial class RobustProcessor
{
    public int RetryCount { get; private set; }
    public Exception? LastException { get; private set; }

    [State(ProcessState.Processing, OnEntry = nameof(OnProcessingEntry))]
    private void ConfigureStates() { }

    [Transition(ProcessState.Idle, ProcessTrigger.Start, ProcessState.Processing,
        Guard = nameof(CanStart),
        Action = nameof(StartProcessing))]
    [Transition(ProcessState.Processing, ProcessTrigger.Complete, ProcessState.Done)]
    [Transition(ProcessState.Processing, ProcessTrigger.Retry, ProcessState.Processing,
        Action = nameof(RetryProcessing))]
    [Transition(ProcessState.Processing, ProcessTrigger.Abort, ProcessState.Failed)]
    private void ConfigureTransitions() { }

    private bool CanStart()
    {
        if (DateTime.Now.Second % 2 == 0)
            throw new InvalidOperationException("Cannot start on even seconds");
        return true;
    }

    private void StartProcessing()
    {
        if (RetryCount == 0)
            throw new InvalidOperationException("First attempt always fails");
    }

    private void OnProcessingEntry()
    {
        if (RetryCount > 3)
            throw new InvalidOperationException("Too many retries");
    }

    private void RetryProcessing()
    {
        RetryCount++;
    }

    // Exception handler
    private ExceptionDirective HandleException(ExceptionContext<ProcessState, ProcessTrigger> ctx)
    {
        LastException = ctx.Exception;
        Console.WriteLine($"Exception in {ctx.Stage}: {ctx.Exception.Message}");

        // Continue for guard exceptions on retry
        if (ctx.Stage == TransitionStage.Guard && ctx.Trigger == ProcessTrigger.Retry)
            return ExceptionDirective.Continue;

        // Continue for action exceptions on first attempt
        if (ctx.Stage == TransitionStage.Action && RetryCount == 0)
        {
            RetryCount++;
            return ExceptionDirective.Continue;
        }

        // Otherwise propagate
        return ExceptionDirective.Propagate;
    }
}
```

### Internal Transitions

Internal transitions execute actions without changing the current state:

```csharp
[StateMachine(typeof(CounterState), typeof(CounterTrigger))]
public partial class CounterMachine
{
    public int Count { get; private set; }
    public DateTime LastUpdate { get; private set; }

    // Regular transitions
    [Transition(CounterState.Idle, CounterTrigger.Start, CounterState.Counting)]
    [Transition(CounterState.Counting, CounterTrigger.Stop, CounterState.Idle)]
    [Transition(CounterState.Counting, CounterTrigger.Reset, CounterState.Idle,
        Action = nameof(ResetCount))]

    // Internal transitions - stay in same state
    [InternalTransition(CounterState.Counting, CounterTrigger.Increment,
        Action = nameof(IncrementCount))]
    [InternalTransition(CounterState.Counting, CounterTrigger.Decrement,
        Guard = nameof(CanDecrement),
        Action = nameof(DecrementCount))]
    private void ConfigureTransitions() { }

    private void IncrementCount()
    {
        Count++;
        LastUpdate = DateTime.Now;
    }

    private bool CanDecrement() => Count > 0;

    private void DecrementCount()
    {
        Count--;
        LastUpdate = DateTime.Now;
    }

    private void ResetCount()
    {
        Count = 0;
        LastUpdate = DateTime.Now;
    }
}
```

## Hierarchical State Machines (HSM)

Hierarchical State Machines allow you to organize states into parent-child relationships.

### Basic Parent-Child Hierarchy

```csharp
public enum WorkflowState
{
    Idle,
    Processing,                 // Parent state
    Processing_Initializing,    // Child of Processing
    Processing_Working,         // Child of Processing
    Processing_Finalizing,      // Child of Processing
    Complete,
    Error
}

[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), EnableHierarchy = true)]
public partial class HierarchicalWorkflow
{
    public List<string> EventLog { get; } = new();

    // Define parent state
    [State(WorkflowState.Processing,
        OnEntry = nameof(OnProcessingEntry),
        OnExit = nameof(OnProcessingExit))]
    private void ConfigureProcessing() { }

    // Define child states with Parent relationship
    [State(WorkflowState.Processing_Initializing,
        Parent = WorkflowState.Processing,
        IsInitial = true,  // This child is entered by default
        OnEntry = nameof(OnInitializingEntry))]
    private void ConfigureInitializing() { }

    [State(WorkflowState.Processing_Working,
        Parent = WorkflowState.Processing,
        OnEntry = nameof(OnWorkingEntry),
        OnExit = nameof(OnWorkingExit))]
    private void ConfigureWorking() { }

    [State(WorkflowState.Processing_Finalizing,
        Parent = WorkflowState.Processing)]
    private void ConfigureFinalizing() { }

    // Transitions
    [Transition(WorkflowState.Idle, WorkflowTrigger.Start, WorkflowState.Processing)]
    // Automatically enters Processing_Initializing (initial child)

    [Transition(WorkflowState.Processing_Initializing, WorkflowTrigger.Next,
        WorkflowState.Processing_Working)]
    [Transition(WorkflowState.Processing_Working, WorkflowTrigger.Next,
        WorkflowState.Processing_Finalizing)]
    [Transition(WorkflowState.Processing_Finalizing, WorkflowTrigger.Complete,
        WorkflowState.Complete)]

    // Transition from parent state applies to all children
    [Transition(WorkflowState.Processing, WorkflowTrigger.Abort, WorkflowState.Error)]
    private void ConfigureTransitions() { }

    // Callbacks
    private void OnProcessingEntry() => EventLog.Add("Enter:Processing");
    private void OnProcessingExit() => EventLog.Add("Exit:Processing");
    private void OnInitializingEntry() => EventLog.Add("Enter:Initializing");
    private void OnWorkingEntry() => EventLog.Add("Enter:Working");
    private void OnWorkingExit() => EventLog.Add("Exit:Working");
}
```

### History States

History modes allow a parent state to remember its last active child:

```csharp
[StateMachine(typeof(PlayerState), typeof(PlayerTrigger), EnableHierarchy = true)]
public partial class GamePlayer
{
    // Parent state with shallow history
    [State(PlayerState.InGame, History = HistoryMode.Shallow)]
    private void ConfigureInGame() { }

    // Child states
    [State(PlayerState.InGame_Playing, Parent = PlayerState.InGame, IsInitial = true)]
    [State(PlayerState.InGame_Paused, Parent = PlayerState.InGame)]
    [State(PlayerState.InGame_Inventory, Parent = PlayerState.InGame)]
    private void ConfigureGameStates() { }

    // Menu states (not in hierarchy)
    [State(PlayerState.MainMenu)]
    [State(PlayerState.Settings)]
    private void ConfigureMenuStates() { }

    // Transitions
    [Transition(PlayerState.MainMenu, PlayerTrigger.Play, PlayerState.InGame)]
    [Transition(PlayerState.InGame_Playing, PlayerTrigger.Pause, PlayerState.InGame_Paused)]
    [Transition(PlayerState.InGame_Paused, PlayerTrigger.Resume, PlayerState.InGame_Playing)]
    [Transition(PlayerState.InGame_Playing, PlayerTrigger.OpenInventory, PlayerState.InGame_Inventory)]
    [Transition(PlayerState.InGame_Inventory, PlayerTrigger.CloseInventory, PlayerState.InGame_Playing)]

    // Exit to menu and return
    [Transition(PlayerState.InGame, PlayerTrigger.Menu, PlayerState.MainMenu)]
    [Transition(PlayerState.MainMenu, PlayerTrigger.Resume, PlayerState.InGame)]
    // When returning to InGame, it remembers the last child state due to History
    private void ConfigureTransitions() { }
}
```

### Deep History

Deep history remembers the complete state hierarchy:

```csharp
[StateMachine(typeof(EditorState), typeof(EditorTrigger), EnableHierarchy = true)]
public partial class TextEditor
{
    // Top-level parent with deep history
    [State(EditorState.Editing, History = HistoryMode.Deep)]
    private void ConfigureEditing() { }

    // First level children
    [State(EditorState.Editing_Text, Parent = EditorState.Editing, IsInitial = true)]
    [State(EditorState.Editing_Code, Parent = EditorState.Editing)]
    private void ConfigureEditingModes() { }

    // Second level children (nested under Editing_Code)
    [State(EditorState.Editing_Code_CSharp,
        Parent = EditorState.Editing_Code, IsInitial = true)]
    [State(EditorState.Editing_Code_JavaScript, Parent = EditorState.Editing_Code)]
    [State(EditorState.Editing_Code_Python, Parent = EditorState.Editing_Code)]
    private void ConfigureCodeModes() { }

    // When returning to Editing state, deep history will restore
    // the exact nested state (e.g., Editing_Code_Python)
}
```

### Priority-based Transition Resolution

When multiple transitions could be valid, priority determines which one is taken:

```csharp
[StateMachine(typeof(TaskState), typeof(TaskTrigger), EnableHierarchy = true)]
public partial class TaskProcessor
{
    private int _priority;
    private bool _isUrgent;

    // Multiple transitions from same state with same trigger
    [Transition(TaskState.Ready, TaskTrigger.Process, TaskState.HighPriority,
        Guard = nameof(IsHighPriority),
        Priority = 100)]  // Highest priority, checked first
    [Transition(TaskState.Ready, TaskTrigger.Process, TaskState.UrgentProcessing,
        Guard = nameof(IsUrgent),
        Priority = 50)]   // Medium priority
    [Transition(TaskState.Ready, TaskTrigger.Process, TaskState.NormalProcessing,
        Priority = 0)]    // Default priority, fallback
    private void ConfigureTransitions() { }

    private bool IsHighPriority() => _priority > 90;
    private bool IsUrgent() => _isUrgent;
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
private async ValueTask<bool> CanTransitionAsync(CancellationToken ct)
{
    await Task.Delay(10, ct);
    return true;
}

private async Task<bool> ValidateAsync(MyPayload payload, CancellationToken ct)
{
    return await _validator.ValidateAsync(payload, ct);
}
```

### Action Methods

Actions return `void` (sync) or `Task`/`ValueTask` (async):

```csharp
// Synchronous actions
private void OnTransition() { /* implementation */ }
private void OnTransitionWithPayload(MyPayload payload) { /* process */ }

// Asynchronous actions
private async Task ProcessAsync(CancellationToken ct)
{
    await _service.ProcessAsync(ct);
}

private async ValueTask HandleAsync(MyPayload payload, CancellationToken ct)
{
    await ProcessPayloadAsync(payload, ct);
}
```

### State Callbacks (OnEntry/OnExit)

```csharp
// Synchronous callbacks
private void OnStateEntry() { /* initialization */ }
private void OnStateExit() { /* cleanup */ }

// With payload
private void OnEntryWithPayload(MyPayload payload) { /* init with data */ }

// Asynchronous callbacks
private async Task OnStateEntryAsync(CancellationToken ct)
{
    await InitializeAsync(ct);
}

private async ValueTask OnStateExitAsync()
{
    await CleanupAsync();
}
```

### Exception Handler

```csharp
// Synchronous handler
private ExceptionDirective HandleException(ExceptionContext<TState, TTrigger> ctx)
{
    LogException(ctx.Exception);
    return ctx.Stage == TransitionStage.Guard
        ? ExceptionDirective.Continue
        : ExceptionDirective.Propagate;
}

// Asynchronous handler
private async ValueTask<ExceptionDirective> HandleExceptionAsync(
    ExceptionContext<TState, TTrigger> ctx,
    CancellationToken ct)
{
    await LogExceptionAsync(ctx.Exception, ct);
    return ExceptionDirective.Continue;
}
```

## Important Notes

### Method Declaration

The methods decorated with state machine attributes (`Configure()`, `ConfigureStates()`, etc.) are "dummy" methods. They exist only to hold the attributes and are never called at runtime. Common patterns:

```csharp
// Single method with all transitions
[Transition(...)]
[Transition(...)]
[Transition(...)]
private void Configure() { }

// Separate methods for organization
[State(State.A, OnEntry = nameof(OnAEntry))]
[State(State.B, OnEntry = nameof(OnBEntry))]
private void ConfigureStates() { }

[Transition(State.A, Trigger.X, State.B)]
private void ConfigureTransitions() { }

// Method names are arbitrary
[Transition(...)]
private void SetupStateMachine() { }
```

### Method Name References

All callback references use `nameof()` for compile-time safety:

```csharp
[State(State.Active, OnEntry = nameof(OnActiveEntry))]  // ✅ Correct
[State(State.Active, OnEntry = "OnActiveEntry")]         // ⚠️ Works but not refactor-safe
```

### Multiple Attributes

You can stack multiple attributes on the same method:

```csharp
[Transition(State.A, Trigger.X, State.B)]
[Transition(State.A, Trigger.Y, State.C)]
[Transition(State.B, Trigger.Z, State.A)]
[InternalTransition(State.B, Trigger.Update, Action = nameof(DoUpdate))]
private void Configure() { }
```

### Attribute Order

The order of attributes doesn't matter - the source generator processes them all:

```csharp
// These are equivalent:
[State(State.A, OnEntry = nameof(EnterA))]
[Transition(State.A, Trigger.X, State.B)]

// vs

[Transition(State.A, Trigger.X, State.B)]
[State(State.A, OnEntry = nameof(EnterA))]
```

## Comparison with Fluent API

Both APIs generate identical runtime code. Choose based on your preferences:

### Legacy API (Attributes)
**Pros:**
- Declarative and concise
- All configuration visible at class level
- Familiar attribute-based approach
- No special DSL to learn

**Cons:**
- Uses `nameof()` which is more verbose
- Less refactoring-friendly
- Harder to see the "flow" of the state machine

### Fluent API
**Pros:**
- Method groups provide cleaner syntax
- Better refactoring support
- More readable flow
- Compile-time DSL validation

**Cons:**
- Requires learning the DSL
- More verbose for simple machines
- Instance method requirement (v0.8.0+)

### Migration Example

**Legacy API:**
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MachineLegacy
{
    [State(State.A, OnEntry = nameof(EnterA))]
    private void ConfigureA() { }

    [Transition(State.A, Trigger.Next, State.B,
        Guard = nameof(CanGo),
        Action = nameof(DoTransition))]
    private void ConfigureTransition() { }

    private void EnterA() { }
    private bool CanGo() => true;
    private void DoTransition() { }
}
```

**Fluent API (v0.8.0):**
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MachineFluent
{
    private void Configure() => FSM
        .State(State.A)
            .OnEntry(EnterA)
            .On(Trigger.Next)
                .Guard(CanGo)
                .Action(DoTransition)
                .GoTo(State.B);

    private void EnterA() { }
    private bool CanGo() => true;
    private void DoTransition() { }
}
```

## Best Practices

1. **Use nameof()** - Always use `nameof()` for method references to maintain compile-time safety
2. **Organize attributes** - Group related attributes on logical methods for clarity
3. **Document complex guards** - Add XML comments to guard methods explaining their logic
4. **Keep actions simple** - Actions should be fast; defer heavy work to background tasks
5. **Handle exceptions** - Use `[OnException]` for robust error handling
6. **Test thoroughly** - Test all transitions, especially guards and edge cases
7. **Use meaningful names** - State and trigger names should clearly express intent

## Troubleshooting

### Common Issues

**No transition from state X on trigger Y**
- Check that the transition is defined with correct state/trigger values
- Verify guards aren't blocking the transition
- Ensure the state machine has been started with `.Start()`

**OnEntry/OnExit not called**
- Verify method names match exactly (use `nameof()`)
- Check method signatures match expected patterns
- Ensure methods are instance methods (not static)

**Guard always returns false**
- Debug the guard method separately
- Check that guard has access to necessary state
- Verify guard signature matches requirements

**Async methods not awaited**
- Use `FireAsync()` for async transitions
- Ensure async methods return `Task` or `ValueTask`
- Check that `CancellationToken` is properly handled

## Summary

The Legacy API provides a robust, attribute-based approach to defining state machines in FastFSM. While the newer Fluent API offers some advantages, the Legacy API remains fully supported and is an excellent choice for teams familiar with attribute-based configuration or when you prefer a more declarative style.

Both APIs compile to the same high-performance runtime code with zero allocations and sub-nanosecond transition times, so choose the API that best fits your team's preferences and coding style.

---

*For more examples and test cases, see the FastFsm.Tests project, particularly the *.Legacy.cs files which demonstrate various Legacy API patterns and use cases.*