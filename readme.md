# FastFSM - High-Performance State Machines for .NET


FastFSM is a powerful, zero-overhead finite state machine framework for .NET that leverages C# source generators to create highly optimized state machines at compile time. It combines the ease of declarative configuration with performance that rivals hand-written code.

## Table of Contents

  - [Why FastFSM?](#why-fastfsm)
  - [Key Features](#key-features)
  - [Getting Started](#getting-started)
  - [Core Concepts](#core-concepts)
  - [New in 0.7: Hierarchical State Machines (HSM)](#new-in-07-hierarchical-state-machines-hsm)
  - [The State Machine Lifecycle (New in 0.6)](#the-state-machine-lifecycle-new-in-06)
  - [Basic Usage](#basic-usage)
  - [API Reference](#api-reference)
  - [Advanced Features](#advanced-features)
  - [Performance](#performance)
  - [Real-World Examples](#real-world-examples)
  - [Architecture Overview](#architecture-overview)
  - [Migration Guide](#migration-guide)
  - [Contributing](#contributing)
  - [License](#license)

## Why FastFSM?

Traditional .NET state machine libraries often rely on reflection, dictionaries, or expression trees at runtime, leading to:

  - Performance overhead from indirect calls and allocations
  - Runtime errors that could be caught at compile time
  - Poor compatibility with AOT compilation and trimming
  - Complex APIs that obscure the actual state logic

FastFSM solves these problems by generating optimized code at compile time, giving you:

  - **Zero runtime reflection** - all transitions compile to simple switch statements
  - **Zero heap allocations** - no garbage collection pressure in your state logic
  - **Compile-time validation** - invalid states and transitions are caught during build
  - **Predictable Lifecycle** - explicit start-up logic eliminates race conditions
  - **Native AOT ready** - fully compatible with trimming and ahead-of-time compilation
  - **Intuitive API** - define states with simple attributes, no complex fluent builders

-----

## Key Features

  - 🚀 **Blazing Fast** - transitions execute in \~0.6 nanoseconds
  - 🗑️ **Zero Allocations** - no heap allocations during state transitions
  - 🛡️ **Type Safe** - full IntelliSense support and compile-time checking
  - 📦 **Modular Design** - pay only for features you use
  - 🔌 **Extensible** - optional logging, dependency injection, and custom extensions
  - ⚡ **Async Support** - first-class async/await support with separate sync/async APIs
  - 🎯 **AOT Compatible** - works with Native AOT and aggressive trimming

-----

## Getting Started

### Installation

```bash
# Core package - just the essentials
dotnet add package FastFSM.Net

# Optional: Add structured logging support
dotnet add package FastFSM.Net.Logging

# Optional: Add dependency injection integration
dotnet add package FastFSM.Net.DependencyInjection
```

### Quick Example

```csharp
// 1. Define your states and triggers as enums
public enum DoorState { Open, Closed, Locked }
public enum DoorTrigger { Open, Close, Lock, Unlock }

// 2. Create your state machine class
[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    // 3. Define transitions using attributes
    [Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Open)]
    [Transition(DoorState.Open, DoorTrigger.Close, DoorState.Closed)]
    [Transition(DoorState.Closed, DoorTrigger.Lock, DoorState.Locked)]
    [Transition(DoorState.Locked, DoorTrigger.Unlock, DoorState.Closed)]
    private void ConfigureTransitions() { }
}

// 4. Use your state machine
var door = new DoorController(DoorState.Closed);
door.Start(); // Start the machine to trigger initial OnEntry callbacks

door.CanFire(DoorTrigger.Open);  // true
door.Fire(DoorTrigger.Open);       // door is now Open
door.CurrentState;                 // DoorState.Open
```

-----

## Core Concepts

### States and Triggers

States represent the possible conditions of your system, while triggers are the events that cause transitions between states. Both are defined as enums for type safety and performance.

### Transitions

Transitions define how your state machine moves from one state to another in response to triggers. They can include:

  - **Guards** - conditions that must be true for the transition to occur
  - **Actions** - code that executes during the transition

### State Callbacks

States can have entry and exit callbacks that execute when entering or leaving a state:

  - **OnEntry** - executes when entering a state
  - **OnExit** - executes when leaving a state

-----

## New in 0.7: Hierarchical State Machines (HSM)

FastFSM 0.7 introduces **Hierarchical State Machines** (HSM), enabling you to organize states into parent-child relationships for more maintainable and scalable designs. HSM adds powerful features while maintaining FastFSM's zero-allocation, sub-nanosecond performance guarantees.

### Enabling HSM

> **Enable HSM in one of two ways:**
> - Set `EnableHierarchy = true` on the `[StateMachine]` attribute
> - Use any HSM feature (`Parent`, `History`, `IsInitial`) - auto-enables hierarchy
>
> HSM is also **automatically enabled** as soon as you use `Parent` or `History` on any state; specifying `EnableHierarchy=true` is optional in that case.

### What HSM Adds

Hierarchical State Machines bring several powerful capabilities to FastFSM:

- **Composite Parents**: States can contain child states, creating a hierarchy
- **Initial Substates**: Each composite parent must designate exactly one child with `IsInitial = true`
- **History Modes**: Shallow history remembers the last active child; deep history remembers the entire substate path
- **Internal Transitions**: Execute actions without changing state or triggering exit/entry callbacks
- **Explicit Priorities**: Control transition precedence with numeric priorities
- **Deterministic Tie-Breaking**: Equal priorities resolve by declaration order
- **Transition Inheritance**: Child states inherit parent transitions, with child transitions overriding parent ones

### Transition Resolution Order

When multiple transitions could handle the same trigger, FastFSM resolves them deterministically:

1. **Higher Priority first** - Transitions with higher `Priority` values win
2. **Child beats Parent** - Child state transitions override parent transitions
3. **Source order on ties** - When priorities are equal, first declared wins

> **Summary:** Priority → Child over Parent → Source order (deterministic)

### Quick Start

Here's a minimal HSM example showing a menu system with parent-child states:

```csharp
// Define states with hierarchy naming convention
public enum MenuState 
{ 
    Root,
    Menu,           // Parent state
    Menu_Main,      // Child of Menu
    Menu_Settings,  // Child of Menu
    Game
}

public enum MenuTrigger { Enter, Settings, Back, StartGame, RefreshUI }

[StateMachine(typeof(MenuState), typeof(MenuTrigger), EnableHierarchy = true)]
public partial class MenuSystem
{
    // Define parent state with shallow history
    [State(MenuState.Menu, History = HistoryMode.Shallow)]
    private void ConfigureMenu() { }
    
    // Define children with one marked as initial
    [State(MenuState.Menu_Main, Parent = MenuState.Menu, IsInitial = true)]
    private void ConfigureMainMenu() { }
    
    [State(MenuState.Menu_Settings, Parent = MenuState.Menu)]
    private void ConfigureSettings() { }
    
    // Transitions between menu states
    [Transition(MenuState.Menu_Main, MenuTrigger.Settings, MenuState.Menu_Settings)]
    [Transition(MenuState.Menu_Settings, MenuTrigger.Back, MenuState.Menu_Main, 
        Priority = 100)]
    [Transition(MenuState.Menu, MenuTrigger.StartGame, MenuState.Game)]
    private void ConfigureTransitions() { }
    
    // Internal transition at parent level - applies to all children
    [InternalTransition(MenuState.Menu, MenuTrigger.RefreshUI, 
        Action = nameof(RefreshDisplay))]
    private void ConfigureRefresh() { }
    
    private void RefreshDisplay() => Console.WriteLine("Refreshing UI...");
}

// Usage
var menu = new MenuSystem(MenuState.Menu);
menu.Start();  // Enters Menu, then auto-enters Menu_Main (initial child)

menu.Fire(MenuTrigger.Settings);    // Menu_Main → Menu_Settings
menu.Fire(MenuTrigger.RefreshUI);   // Internal transition, stays in Menu_Settings
menu.Fire(MenuTrigger.StartGame);   // Exits entire Menu hierarchy → Game
```

### API Cheatsheet

#### State Configuration
```csharp
[State(state, 
    Parent = parentState,        // Makes this a child of parentState
    IsInitial = true,           // Marks as initial child (exactly one required per parent)
    History = HistoryMode.X,    // None, Shallow, or Deep
    OnEntry = "method",         // Entry callback
    OnExit = "method")]         // Exit callback
```

#### Transitions with Priorities
```csharp
[Transition(fromState, trigger, toState,
    Priority = 100,             // Higher number = higher priority (default: 0)
    Guard = "guardMethod",      // Optional condition
    Action = "actionMethod")]   // Optional action during transition
```

#### Internal Transitions
```csharp
[InternalTransition(state, trigger,
    Priority = 100,             // Priority for resolution order
    Guard = "guardMethod",      // Optional condition
    Action = "actionMethod")]   // Action without state change or exit/entry
```

> **Note:** Internal transitions **never** change the state and **never** run `OnExit`/`OnEntry`; they only evaluate guard and run action.

#### Generated Helper Methods
```csharp
// Runtime hierarchy checking
bool IsInHierarchy(TState ancestor)  // True if ancestor is current state or any parent
```
The generator creates optimized helper methods for hierarchy queries:

```csharp
// Check if current state is within a parent's hierarchy
bool IsInHierarchy(MenuState parentState);

// Per-state helpers for ≤64 states (uses bitmasks for speed)
bool IsInMenu();     // true if in Menu or any child
bool IsInSettings(); // true if in Settings or its children

// DEBUG-only path visualization
#if DEBUG
string DumpActivePath(); // Returns e.g., "Menu / Menu_Settings"
#endif
```

#### Generated HSM Fields

The generator creates zero-allocation structures for hierarchy support:

_One of the following forms is emitted depending on the number of states._

```csharp
// For ≤64 states: Single ulong bitmask per state
private static readonly ulong[] s_hierarchyMask = new ulong[] { 
    0b0001, // Root
    0b0110, // Menu (includes children)
    0b0110, // Menu_Main (part of Menu)
    // ...
};

// For >64 states: Array of uint32 backing
private static readonly uint[][] s_hierarchyMask = new uint[][] {
    new uint[] { 0x00000001 }, // Root
    new uint[] { 0x00000006 }, // Menu
    // ...
};

// Parent indices and initial children
private static readonly byte[] s_parent = new byte[] { 255, 0, 1, 1, 0 };  // 255 = NO_PARENT sentinel
private static readonly int[] s_initialChild = new int[] { 1, 2, -1, -1, -1 };

// Note: Types/sentinels match the emitted code; parent indices commonly use byte with 255 as NO_PARENT.
```

### Caveats (Must-Read)

- **Composite states must have children and exactly one `IsInitial` child** (FSM102: Error). The generator will fail if a parent has no initial child.
- **History on a non-composite parent is a warning** (FSM104: Warning). Fix by removing history or adding child states.
- **Implicit transitions to a composite parent** resolve via initial substate or history (FSM105: Info).
- **Equal priorities are allowed**; ties are resolved by declaration order (FSM106: Warning).
- **Internal transitions do not change state** and never run `OnExit`/`OnEntry` callbacks.
- **Cross-hierarchy transitions are allowed**; child transition handlers pre-empt parent handlers.
- **Payload + HSM**: When a transition targets a parent, the automatic descent to its initial child uses the parameterless `OnEntry`. Payload reaches `OnEntry(TPayload)` only when the transition targets that specific child directly.
- **Enum-only fallback** (no `[State]` method declarations): Supported but remains flat—no HSM features without explicit `[State]` attributes.
- **`DumpActivePath()` is DEBUG-only**: Returns the active state path as a string for debugging (e.g., `"Root / Menu / Menu_Settings"`).

### Debugging HSM

FastFSM provides helper methods for understanding the current state hierarchy:

#### IsInHierarchy() (All builds)

The `IsInHierarchy(TState ancestor)` method checks if the current state lies within the hierarchy of a given ancestor state. This is useful for runtime state validation and conditional logic based on hierarchical relationships.

```csharp
var workflow = new WorkflowMachine(WorkflowState.Work_Processing_Loading);
workflow.Start();

// Check hierarchical relationships
if (workflow.IsInHierarchy(WorkflowState.Work))
{
    // Current state is within the Work composite
}
```

#### DumpActivePath() (DEBUG-only)

```csharp
#if DEBUG
var workflow = new WorkflowMachine(WorkflowState.Work_Processing_Loading);
workflow.Start();
Console.WriteLine(workflow.DumpActivePath());
// Output: "Work / Work_Processing / Work_Processing_Loading"
#endif
```

This method walks up the parent chain from the current state to build the complete path string. This method is emitted **only in DEBUG builds**; it is not present in Release.

### Enum-only Fallback

If no `[State]` attributes are present in your state machine class, FastFSM automatically uses all enum members as states (flat mode only - no hierarchy support). This provides a quick way to define simple state machines:

```csharp
public enum SimpleState { Idle, Running, Complete }

[StateMachine(typeof(SimpleState), typeof(SimpleTrigger))]
public partial class SimpleMachine 
{
    // No [State] attributes - all enum values become states
    // Generator emits FSM994 info diagnostic to confirm enum-only mode
}
```

The generator emits diagnostic **FSM994** (info level) when using enum-only fallback mode. Enum-only fallback machines are **always flat** (no hierarchy); HSM requires `[State]` declarations with `Parent/IsInitial/History`.

### Async + Cancellation in HSM

HSM fully supports async operations with proper cancellation handling:

- **Cancellation during transition**: If cancelled during exit/action/entry sequence, the state remains unchanged
- **Exception handling**: Exceptions propagate normally, following the configured `OnException` directive
- **OperationCanceledException**: Always propagates without special handling

If a transition is cancelled during `OnExit`, `Action`, or `OnEntry`, the **state remains unchanged**; exceptions propagate as usual.

### Migration (0.6 → 0.7) - Backwards Compatibility

Flat FSMs are completely unaffected by the 0.7 update—all existing state machines continue to work without any changes. **Internal-only machines are fully supported** - you don't need any external transitions for a valid state machine.

To enable HSM features:

1. Add parent relationships using the `Parent` parameter on child states
2. Mark exactly one child per parent with `IsInitial = true`
3. Optionally enable hierarchy explicitly with `EnableHierarchy = true` on the `[StateMachine]` attribute
4. Hierarchy is automatically enabled if any HSM attributes (`Parent`, `IsInitial`, `History`) are used

The generator handles all the complexity of hierarchy management, maintaining the same high-performance characteristics as flat machines.

### Performance

HSM maintains FastFSM's commitment to zero allocations and predictable performance:

- **≤64 states**: Uses bitmask fast path for hierarchy checks (sub-nanosecond operations)
- **>64 states**: Switches to array masks for larger state spaces
- **>256 states**: Planned for future releases (0.8+)
- **Zero allocations**: All HSM features maintain zero heap allocation guarantees
- **Compile-time optimization**: Hierarchy is flattened to switch statements at compile time
- **History tracking**: Minimal overhead with stack-allocated structures

Benchmark results show HSM transitions execute in ~0.85ns for simple parent→child transitions, with internal transitions even faster at ~0.70ns.

Results are indicative; see `/Benchmark` for methodology & code.

-----

## The State Machine Lifecycle (New in 0.6)

As of version 0.6, creating a state machine and running it are two distinct steps. The constructor **only** sets the initial state. You must explicitly start the machine to make it operational and trigger the first `OnEntry` callback. This change prevents race conditions and makes the machine's behavior deterministic.

1.  **Constructor**: Sets the initial `CurrentState` but does **not** run any `OnEntry` logic. The machine is not yet active.
2.  **`Start()` / `StartAsync()`**: Activates the machine. This method runs the `OnEntry` callback for the initial state. Subsequent calls to `Start()` do nothing.
3.  **Operations**: Calling any method like `Fire`, `TryFire`, or `CanFire` before `Start()` will throw an `InvalidOperationException`.

<!-- end list -->

```mermaid
sequenceDiagram
    participant U as User Code
    participant M as Machine
    U->>M: new DoorController(DoorState.Closed)
    Note right of M: _currentState = Closed<br/>_started = false
    U-->>M: Start()
    M->>M: _started = true
    M->>M: Executes OnEntry for 'Closed' state
    Note right of M: Machine is now active
    U-->>M: Fire(DoorTrigger.Open)
    Note right of M: Transition proceeds
```

-----

## Basic Usage

### Simple State Machine

```csharp
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class OrderWorkflow
{
    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted)]
    [Transition(OrderState.Submitted, OrderTrigger.Approve, OrderState.Approved)]
    [Transition(OrderState.Submitted, OrderTrigger.Reject, OrderState.Rejected)]
    private void Configure() { }
}

// Usage
var workflow = new OrderWorkflow(OrderState.New);
workflow.Start();
workflow.Fire(OrderTrigger.Submit);
```

### Adding Guards and Actions

```csharp
[StateMachine(typeof(AccountState), typeof(AccountTrigger))]
public partial class BankAccount
{
    private decimal _balance;
    
    [Transition(AccountState.Active, AccountTrigger.Withdraw, AccountState.Active,
        Guard = nameof(HasSufficientFunds),
        Action = nameof(DebitAccount))]
    private void ConfigureWithdrawal() { }
    
    private bool HasSufficientFunds() => _balance >= 100;
    private void DebitAccount() => _balance -= 100;
}

// Usage
var account = new BankAccount(AccountState.Active);
account.Start();
account.Fire(AccountTrigger.Withdraw);
```

### State Entry/Exit Callbacks

```csharp
[StateMachine(typeof(ConnectionState), typeof(ConnectionTrigger))]
public partial class NetworkConnection
{
    [State(ConnectionState.Connected, 
        OnEntry = nameof(StartHeartbeat),
        OnExit = nameof(StopHeartbeat))]
    private void ConfigureStates() { }
    
    [Transition(ConnectionState.Disconnected, ConnectionTrigger.Connect, ConnectionState.Connected)]
    [Transition(ConnectionState.Connected, ConnectionTrigger.Disconnect, ConnectionState.Disconnected)]
    private void ConfigureTransitions() { }
    
    private void StartHeartbeat() => Console.WriteLine("Heartbeat started");
    private void StopHeartbeat() => Console.WriteLine("Heartbeat stopped");
}

// Usage
var connection = new NetworkConnection(ConnectionState.Disconnected);
connection.Start(); // Machine is not active until started
connection.Fire(ConnectionTrigger.Connect); // "Heartbeat started" is printed
```

-----

## API Reference

### StateMachine Attribute

Marks a partial class as a state machine.

```csharp
[StateMachine(typeof(TState), typeof(TTrigger))]
```

**Parameters:**

  - `TState` - Enum type defining possible states
  - `TTrigger` - Enum type defining possible triggers

**Optional Properties:**

  - `DefaultPayloadType` - Default payload type for all transitions
  - `GenerateExtensibleVersion` - Enable extension support

### Transition Attribute

Defines a state transition.

```csharp
[Transition(fromState, trigger, toState, Guard = "method", Action = "method")]
```

**Parameters:**

  - `fromState` - Source state
  - `trigger` - Trigger that causes transition
  - `toState` - Destination state
  - `Guard` (optional) - Method name that returns bool
  - `Action` (optional) - Method name to execute during transition

### State Attribute

Configures state-specific behavior.

```csharp
[State(state, OnEntry = "method", OnExit = "method")]
```

**Parameters:**

  - `state` - The state to configure
  - `OnEntry` (optional) - Method to execute when entering state
  - `OnExit` (optional) - Method to execute when leaving state

### Generated Methods

Every state machine automatically gets these methods. **Note:** All methods (except the constructor) will throw an `InvalidOperationException` if called before `Start()` / `StartAsync()`.

```csharp
// Current state of the machine
TState CurrentState { get; }

// Starts the machine, making it operational and running the initial OnEntry callback.
void Start()
ValueTask StartAsync(CancellationToken ct = default)

// Try to fire a trigger (returns true if successful)
bool TryFire(TTrigger trigger, object? payload = null)
ValueTask<bool> TryFireAsync(TTrigger trigger, object? payload = null, CancellationToken ct = default)

// Fire a trigger (throws if invalid)
void Fire(TTrigger trigger, object? payload = null)
ValueTask FireAsync(TTrigger trigger, object? payload = null, CancellationToken ct = default)

// Check if a trigger can be fired
bool CanFire(TTrigger trigger)

// Get all valid triggers from current state
IReadOnlyList<TTrigger> GetPermittedTriggers()
```

### Extension Hooks

When extension support is enabled (extensible machines), the following hooks are invoked around a transition in an order consistent with UML run-to-completion semantics:

- Before: `OnBeforeTransition(ctx)` — after a transition path is selected (from, trigger, to), before any effects.
- Guard start: `OnGuardEvaluation(ctx, guardName)` — immediately before the guard is evaluated.
- Guard result: `OnGuardEvaluated(ctx, guardName, result)` — immediately after evaluation.
- Exit: state `OnExit` of the source state (if defined).
- Action: transition action (if defined).
- State change: `CurrentState` is set to the target state.
- Entry: state `OnEntry` of the target state (if defined).
- After: `OnAfterTransition(ctx, success)` — success=true when the whole sequence completes; otherwise success=false.

Notes:
- Guard hooks are only fired during `TryFire`/`Fire` (not during `CanFire` or `GetPermittedTriggers`).
- If a guard returns false: `OnBeforeTransition` → `OnGuardEvaluation` → `OnGuardEvaluated(false)` → `OnAfterTransition(false)`.
- If an exception occurs during Exit/Action/Entry: `OnAfterTransition(false)` is invoked; no additional rollback is guaranteed.

-----

## Advanced Features

### Nested State Machine Classes

State machine types can be declared as nested partial classes inside other types. The generator will mirror the containing class hierarchy so that callbacks and guard/action methods resolve correctly:

```csharp
public class OuterContainer
{
    public enum InnerState { A, B }
    public enum InnerTrigger { Next }

    [StateMachine(typeof(InnerState), typeof(InnerTrigger))]
    public partial class InnerMachine
    {
        [Transition(InnerState.A, InnerTrigger.Next, InnerState.B)]
        private void Configure() { }
        private void OnEntryA() => Console.WriteLine("Entering A");
    }
}

// Usage
var machine = new OuterContainer.InnerMachine(OuterContainer.InnerState.A);
machine.Start(); // Nested callbacks resolve in correct scope
```

### Typed Payloads

```csharp
[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
[PayloadType(typeof(ProcessData))]
public partial class DataProcessor
{
    [Transition(ProcessState.Ready, ProcessTrigger.Start, ProcessState.Processing,
        Guard = nameof(IsValidData),
        Action = nameof(ProcessData))]
    private void Configure() { }
    
    private bool IsValidData(ProcessData data) => data != null && data.IsValid;
    private void ProcessData(ProcessData data) => Console.WriteLine($"Processing {data.Id}");
}

// Usage
var processor = new DataProcessor(ProcessState.Ready);
processor.Start(); // Must be started before use
var data = new ProcessData { Id = "123", IsValid = true };
processor.Fire(ProcessTrigger.Start, data);
```

### Async Support

FastFSM provides separate sync and async APIs for clarity and type safety. Async methods return `ValueTask` for high performance.

```csharp
[StateMachine(typeof(DownloadState), typeof(DownloadTrigger))]
public partial class FileDownloader
{
    [Transition(DownloadState.Ready, DownloadTrigger.Start, DownloadState.Downloading,
        Action = nameof(StartDownloadAsync))]
    private void Configure() { }
    
    private async ValueTask StartDownloadAsync()
    {
        await Task.Delay(100); // Simulate async work
    }
}

// Usage
var downloader = new FileDownloader(DownloadState.Ready);
await downloader.StartAsync(); // Use StartAsync for async machines
await downloader.FireAsync(DownloadTrigger.Start);
```

### Dependency Injection

When using `FastFSM.Net.DependencyInjection`, the `IStateMachineFactory` provides helpers to manage the new lifecycle:

```csharp
// In your service configuration (e.g., Program.cs)
services.AddStateMachineFactory();
services.AddTransient<OrderWorkflow>(); // Register your state machine

// In your consumer class
public class OrderService(IStateMachineFactory factory)
{
    public void ProcessNewOrder()
    {
        // Option 1: Create and start manually
        var sm1 = factory.Create<OrderWorkflow>(OrderState.New);
        sm1.Start();
        
        // Option 2: Create and start in one step
        var sm2 = factory.CreateStarted<OrderWorkflow>(OrderState.New);
        
        // Option 3: For async machines
        var downloader = await factory.CreateStartedAsync<FileDownloader>(DownloadState.Ready);
    }
}
```

-----

## Benchmarks

### Performance Summary

FastFSM achieves **sub‑nanosecond** transition times (0.81 ± 0.03 ns) for basic synchronous operations, with **zero heap allocations**. When compared to state machine libraries across languages, FastFSM demonstrates the value of compile-time code generation over runtime abstractions.

### Test Environment

| Component       | Version / Details                                              |
| --------------- | -------------------------------------------------------------- |
| **CPU**         | AMD Ryzen 5 9600X (6C/12T Zen 5 @ 3.9–5.4 GHz, AVX‑512)        |
| **Memory**      | 32 GB DDR5, Windows 11 24H2 "High Performance" power plan      |
| **.NET**        |  9.0.5 (*RyuJIT AVX‑512*, Server GC, `COMPlus_EnableAVX512=1`) |
| **JVM**         |  OpenJDK 21.0.8+9‑LTS (Temurin, Server VM, G1 GC)              |
| **C++**         |  MSVC 19.44 (/O2 /GL /arch\:AVX512) + Google Benchmark 1.8.4   |
| **Rust**        |  rustc 1.80 + Statig 0.4 + criterion 0.5.1 (LTO, `codegen-units=1`) |
| **TypeScript**  |  Bun 1.2.19 + mitata (compiled TS → JS)                        |
| **JavaScript**  |  Bun 1.2.19 + mitata (pure ES2023)                             |
| **Tools**       | BenchmarkDotNet 0.15.2 (.NET), JMH 1.37 (Java)                 |
| **Methodology** | 1024 ops per iteration · 15 iterations (mean ± StdDev)         |
| **Date**        | 7 Aug 2025                                                     |

*Results may vary ± 5‑8 % on CPUs without AVX‑512 support. Rust benchmarks use Statig 0.4 library for library-to-library comparison fairness.*

---

### FastFSM vs .NET State‑Machine Libraries

| Scenario               | FastFSM     | Stateless  | LiquidState  | Appccelerate | Winner                       |
| ---------------------- | ----------- | ---------- | ------------ | ------------ | ---------------------------- |
| **Basic Transitions**  | **0.81 ns** | 249.03 ns  | 25.31 ns     | 260.85 ns    | FastFSM (31× vs LiquidState) |
| Guards + Actions       | **2.18 ns** | 267.37 ns  |  n/a¹        | 273.53 ns    | FastFSM (123× vs Stateless)  |
| Payload                | **0.83 ns** | 300.63 ns  | 30.13 ns     | 291.60 ns    | FastFSM (36× vs LiquidState) |
| Can Fire Check         | **0.31 ns** | 115.54 ns  |  n/a¹        |  n/a¹        | FastFSM (373× vs Stateless)  |
| Get Permitted Triggers | **4.18 ns** | 32.69 ns   |  n/a¹        |  n/a¹        | FastFSM (7.8× vs Stateless)  |
| Async Hot Path²        | 444.77 ns   | 357.12 ns  | **75.87 ns** | 504.37 ns    | LiquidState (5.9× faster)    |
| Async With Yield³      | **456.72 ns**| 1100.78 ns | 490.22 ns    | 1738.62 ns   | FastFSM                      |

¹ API not available   ² `ValueTask.CompletedTask` vs `Task.FromResult`   ³ `Task.Yield()` for real context switch

---

### FastFSM vs TypeScript (Bun)

| Scenario                  | TypeScript (Bun) | FastFSM (.NET) | Δ (relative)   |
| ------------------------- | ---------------- | -------------- | -------------- |
| **Basic Transitions**     | \~1.2 ns         | **0.81 ns**    | 1.5× slower    |
| Guards + Actions          | **\~1.5 ns**     | 2.18 ns        | 1.45× *faster* |
| Payload                   | \~2.8 ns         | **0.83 ns**    | 3.4× slower    |
| Can Fire Check            | \~0.6 ns         | **0.31 ns**    | 2.0× slower    |
| Get Permitted Triggers    | **\~1.0 ns**     | 4.18 ns        | 4.2× *faster*  |
| Async Hot Path (no yield) | **\~208 ns**     | 444.77 ns      | 2.1× *faster*  |
| Async With Yield          | \~2000 ns        | **456.72 ns**  | 4.4× slower    |

*TS implementation: minimal FSM with `switch`; Bun's JIT optimises hot paths strongly. Async models differ (`setImmediate()` vs `Task.Yield()`).*

---

### FastFSM vs JavaScript (Bun)

| Scenario               | JavaScript (Bun) | FastFSM (.NET) | Δ (relative)   |
| ---------------------- | ---------------- | -------------- | -------------- |
| **Basic Transitions**  | \~1.0 ns         | **0.81 ns**    | 1.2× slower    |
| Guards + Actions       | \~3.0 ns         | **2.18 ns**    | 1.4× slower    |
| Payload                | \~2.8 ns         | **0.83 ns**    | 3.4× slower    |
| Can Fire Check         | \~0.38 ns        | **0.31 ns**    | 1.2× slower    |
| Get Permitted Triggers | **\~0.39 ns**    | 4.18 ns        | 10.7× *faster* |
| Async Hot Path         | **\~203 ns**     | 444.77 ns      | 2.2× *faster*  |
| Async With Yield       | \~712 ns         | **456.72 ns**  | 1.6× slower    |

*Pure ES2023 class with `switch`, same methodology.*

---

### FastFSM vs Java State‑Machine Libraries

| Scenario              | FastFSM (.NET)    | Squirrel (JDK 21) | Spring StateMachine (JDK 21) |
| --------------------- | ----------------- | ----------------- | ---------------------------- |
| **Basic Transitions** | **0.81 ns / 0 B** | 289 ns / 1.5 kB   | 12 188 ns / 30.7 kB          |
| Guards + Actions      | **2.18 ns / 0 B** | 321 ns / 1.5 kB   | 13 564 ns / 31.3 kB          |
| Payload               | **0.83 ns / 0 B** | 311 ns / 1.5 kB   | 12 321 ns / 31.6 kB          |
| Async Hot Path        | 444.77 ns         | **314 ns**        | 12 110 ns                    |
| Async With Yield      | **456.72 ns**     | 310 ns            | 26 599 ns                    |

---

### FastFSM vs C++ State Machines (Boost.SML)

| Scenario              | FastFSM (.NET) | Boost.SML (C++) | Δ                   |
| --------------------- | -------------- | --------------- | ------------------- |
| **Basic Transitions** | **0.81 ns**    | 1.23 ns         | FastFSM 1.5× faster |
| Guards + Actions      | 2.18 ns        | **1.32 ns**     | C++ 1.7× faster     |
| Payload               | **0.83 ns**    | 1.35 ns         | FastFSM 1.6× faster |
| Can Fire Check        | **0.31 ns**    | 1.28 ns         | 4.1× faster         |
| Async Hot Path\*      | 444.77 ns      | 1.38 ns         | *Not comparable*    |

*Boost.SML "async" is a synchronous simulation (no coroutines).*

---

### FastFSM vs Rust State Machines (Statig Library)

| Scenario              | FastFSM (.NET) | Rust (Statig 0.4) | Δ                      |
| --------------------- | -------------- | ----------------- | ---------------------- |
| **Basic Transitions** | **0.81 ns**    | 1,730 ns          | FastFSM 2,136× faster  |
| Guards + Actions      | 2.18 ns        | **0.37 ns**       | Statig 5.9× faster     |
| Payload               | 0.83 ns        | **0.41 ns**       | Statig 2.0× faster     |
| Async Hot Path        | 444.77 ns      | **7.3 ns**        | Statig 61× faster      |
| Async With Yield      | 456.72 ns      | **19.4 ns**       | Statig 24× faster      |

**Note**: Rust benchmarks use the Statig 0.4 library for fair library-to-library comparison. Statig's abstractions add significant overhead for basic transitions (enum wrapping, trait dispatch) but excel at complex scenarios through aggressive inlining. Hand-rolled Rust code would show different trade-offs: ~1.77 ns for basic transitions but 0.7–0.8 ns for guards/payload scenarios.

---

### Cross‑Language Summary (ns/op)

| Scenario           | FastFSM (.NET) | Java | C++  | Rust (Statig) | TypeScript (Bun) | JavaScript (Bun) | Fastest        |
| ------------------ | -------------- | ---- | ---- | ------------- | ---------------- | ---------------- | -------------- |
| **Basic**          | **0.81**       | 289  | 1.23 | 1,730         | 1.2              | 1.0              | **FastFSM**    |
| **Guards+Actions** | 2.18           | 321  | 1.32 | **0.37**      | 1.5              | 3.0              | **Rust**       |
| **Payload**        | 0.83           | 311  | 1.35 | **0.41**      | 2.8              | 2.8              | **Rust**       |
| **Async (hot)**    | 444.77         | 314  | 1.38 | **7.3**       | 208              | 203              | **C++***       |
| **Async (yield)**  | 456.72         | 310  | n/a  | **19.4**      | 2000             | 712              | **Rust**       |

*C++ "async" is synchronous simulation only; Rust shows true async performance.

---

### Memory Allocations per Operation

| Library             | Allocations / op     | Native Code Size |
| ------------------- | -------------------- | ---------------- |
| FastFSM             | **0 bytes**          | 160 – 8 050 B    |
| Statig (Rust)       | **0 bytes**          | ~2 – 8 KB        |
| Stateless           | 608 – 2 295 B        | 3 436 – 21 417 B |
| LiquidState         | 136 – 656 B          | 64 – 3 496 B     |
| Appccelerate        | 1 608 – 3 166 B      | 1 084 – 3 721 B  |
| Squirrel            | 1 456 – 1 536 B      | n/a              |
| Spring StateMachine | 30 675 – 31 659 B    | n/a              |
| TypeScript (Bun)    | \~40 – 90 B (engine) | n/a              |
| JavaScript (Bun)    | \~40 – 60 B (engine) | n/a              |

---

### Key Performance Insights

The benchmarks reveal fundamental trade-offs in state machine design:

1. **Library Abstractions vs Hand-rolled Code**: The Rust Statig library shows a 1000× slowdown for basic transitions compared to hand-rolled code (1,730 ns vs 1.77 ns), demonstrating the cost of runtime abstractions. However, it excels at complex scenarios through aggressive inlining.

2. **Compile-time vs Runtime**: FastFSM's source generator approach achieves hand-rolled performance while maintaining library convenience. This validates the design choice of compile-time code generation over runtime flexibility.

3. **Language vs Implementation**: Performance varies more by implementation strategy than language choice. A well-optimized library in any language can outperform poorly designed code in a "faster" language.

4. **Zero-allocation Achievement**: Both FastFSM and Statig achieve true zero-allocation operation, critical for high-frequency state transitions in performance-sensitive applications.

---


## Real-World Examples

All examples assume the machine is started with `Start()` or `StartAsync()` after instantiation.

### Order Processing System

```csharp
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(Order))]
public partial class OrderProcessor
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IInventoryService _inventory;
    
    public OrderProcessor(OrderState initial, IPaymentGateway payment, IInventoryService inventory) 
        : this(initial)
    {
        _paymentGateway = payment;
        _inventory = inventory;
    }
    
    [State(OrderState.Processing, OnEntry = nameof(ReserveInventory))]
    [State(OrderState.Cancelled, OnEntry = nameof(ReleaseInventory))]
    private void ConfigureStates() { }
    
    [Transition(OrderState.New, OrderTrigger.Submit, OrderState.Submitted,
        Guard = nameof(IsValid))]
    [Transition(OrderState.Submitted, OrderTrigger.Process, OrderState.Processing,
        Guard = nameof(CanProcess), Action = nameof(ChargePayment))]
    [Transition(OrderState.Processing, OrderTrigger.Ship, OrderState.Shipped)]
    [Transition(OrderState.Submitted, OrderTrigger.Cancel, OrderState.Cancelled)]
    private void ConfigureTransitions() { }
    
    private bool IsValid(Order order) => order.Items.Any() && order.Total > 0;
    private bool CanProcess(Order order) => _inventory.IsAvailable(order.Items);
    private async ValueTask ChargePayment(Order order) => await _paymentGateway.Charge(order);
    private void ReserveInventory(Order order) => _inventory.Reserve(order.Items);
    private void ReleaseInventory(Order order) => _inventory.Release(order.Items);
}
```

-----

## Architecture Overview

FastFSM uses a multi-stage compilation approach:

1.  **Declaration** - You define states and transitions using attributes
2.  **Analysis** - Source generator analyzes your declarations at compile time
3.  **Generation** - Optimized implementation is generated as part of your class
4.  **Compilation** - Everything compiles to efficient IL code

The generator creates different variants based on features used:

  - **Pure** - Just transitions (fastest)
  - **Basic** - Adds OnEntry/OnExit
  - **WithPayload** - Typed data support
  - **WithExtensions** - Plugin support
  - **Full** - All features

You automatically get the most efficient variant for your needs.

-----

## Migration Guide

### Upgrading from v0.5 to v0.6

Version 0.6 introduces breaking changes to improve safety and provide a more explicit API. The key is the new machine lifecycle.

**Checklist for migration:**

1.  **Call `Start()` or `StartAsync()`**: After creating any state machine instance, you **must** call `.Start()` (or `await .StartAsync()`) before any other operations.
2.  **Update Interfaces**: The old `IStateMachine<,>` interfaces are gone.
      - For sync machines, use `IStateMachineSync<TState, TTrigger>`.
      - For async machines, use `IStateMachineAsync<TState, TTrigger>`.
      - For extensible machines, use `IExtensibleStateMachineSync<,>` or `IExtensibleStateMachineAsync<,>`.
3.  **Update DI Factory Usage**: If using `IStateMachineFactory`, decide if you want to create a started or unstarted machine:
      - `factory.Create<T>()` returns an unstarted machine.
      - `factory.CreateStarted<T>()` returns an already-started machine.
      - `factory.CreateStartedAsync<T>()` creates and asynchronously starts the machine.
4.  **Remove Race Condition Workarounds**: Any code that was 'waiting' for `OnEntry` to complete after construction can be removed. The `Start()`/`StartAsync()` call now guarantees `OnEntry` has finished before continuing.
5.  **Update Test Setups**: Add `.Start()` / `.StartAsync()` in your test arrange phases or `[GlobalSetup]` methods in benchmarks.

### From Stateless to FastFSM

```csharp
// Stateless
var machine = new StateMachine<State, Trigger>(State.Initial);
machine.Configure(State.Initial)
    .Permit(Trigger.Start, State.Running);

// FastFSM (v0.6+)
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MyMachine
{
    [Transition(State.Initial, Trigger.Start, State.Running)]
    private void Configure() { }
}

// Usage
var machine = new MyMachine(State.Initial);
machine.Start(); // Don't forget to start the machine!
machine.Fire(Trigger.Start);
```

-----

## Contributing

We welcome contributions!

## License

FastFSM is licensed under the MIT License. See [LICENSE] for details.

[1]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/%3Futm_source%3Dchatgpt.com\) "BenchmarkDotNet: Home"
[2]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/articles/configs/diagnosers.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/articles/configs/diagnosers.html%3Futm_source%3Dchatgpt.com\) "Diagnosers - BenchmarkDotNet"
[3]: https://www.google.com/search?q=%5Bhttps://github.com/dotnet/BenchmarkDotNet/issues/1832%3Futm_source%3Dchatgpt.com%5D\(https://github.com/dotnet/BenchmarkDotNet/issues/1832%3Futm_source%3Dchatgpt.com\) "[Proposal] OperationsPerInvoke to be fed by Params #1832 - GitHub"
[4]: https://www.google.com/search?q=%5Bhttps://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask%3Fview%3Dnet-9.0%26utm_source%3Dchatgpt.com%5D\(https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask%3Fview%3Dnet-9.0%26utm_source%3Dchatgpt.com\) "ValueTask Struct (System.Threading.Tasks) | Microsoft Learn"
[5]: https://www.google.com/search?q=%5Bhttps://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/%3Futm_source%3Dchatgpt.com%5D\(https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/%3Futm_source%3Dchatgpt.com\) "Understanding the Whys, Whats, and Whens of ValueTask - .NET Blog"
[6]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/api/BenchmarkDotNet.Engines.DeadCodeEliminationHelper.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/api/BenchmarkDotNet.Engines.DeadCodeEliminationHelper.html%3Futm_source%3Dchatgpt.com\) "Class DeadCodeEliminationHelper - BenchmarkDotNet"
[7]: https://www.google.com/search?q=%5Bhttps://fransbouma.github.io/BenchmarkDotNet/RulesOfBenchmarking.htm%3Futm_source%3Dchatgpt.com%5D\(https://fransbouma.github.io/BenchmarkDotNet/RulesOfBenchmarking.htm%3Futm_source%3Dchatgpt.com\) "Rules of benchmarking - BenchmarkDotNet Documentation"
[8]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/articles/features/event-pipe-profiler.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/articles/features/event-pipe-profiler.html%3Futm_source%3Dchatgpt.com\) "EventPipeProfiler - BenchmarkDotNet"
[9]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/articles/samples/IntroEventPipeProfiler.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/articles/samples/IntroEventPipeProfiler.html%3Futm_source%3Dchatgpt.com\) "Sample: EventPipeProfiler - BenchmarkDotNet"
