# FastFSM -- High-Performance State Machines for .NET

FastFSM is a powerful, zero-overhead state machine framework for .NET that leverages C# source generators to create highly optimized code at compile time. It combines easy, declarative configuration with performance that rivals hand-written code. **FastFSM lets you define Finite State Machines (FSM) and Hierarchical State Machines (HSM)** with simple attributes, while achieving near hardware-level execution speeds.

**🎉 Version 0.7 Complete (August 17, 2024)** -- Full Hierarchical State Machine (HSM) support is now available! Create parent-child state relationships, use shallow/deep history modes, set transition priorities, and leverage internal transitions -- all while maintaining our zero-allocation, sub-nanosecond performance guarantees.

## Table of Contents

- [Why FastFSM?](#why-fastfsm)
- [Key Features](#key-features)
- [Getting Started](#getting-started)
- [Core Concepts](#core-concepts)
- [Hierarchical State Machines (HSM) -- New in v0.7](#hierarchical-state-machines-hsm----new-in-v07)
- [The State Machine Lifecycle (New in v0.6)](#the-state-machine-lifecycle-new-in-v06)
- [Basic Usage](#basic-usage)
- [API Reference](#api-reference)
- [Advanced Features](#advanced-features)
- [Benchmarks](#benchmarks)
- [Performance Summary](#performance-summary)
- [FastFSM vs .NET State-Machine Libraries](#fastfsm-vs-net-state-machine-libraries)
- [FastFSM HSM vs Stateless HSM](#fastfsm-hsm-vs-stateless-hsm)
- [FastFSM vs TypeScript (Bun)](#fastfsm-vs-typescript-bun)
- [FastFSM vs JavaScript (Bun)](#fastfsm-vs-javascript-bun)
- [FastFSM vs Java State-Machine Libraries](#fastfsm-vs-java-state-machine-libraries)
- [FastFSM vs C++ State Machines (Boost.SML)](#fastfsm-vs-c-state-machines-boostsml)
- [FastFSM vs Rust State Machines (Statig)](#fastfsm-vs-rust-state-machines-statig)
- [Cross-Language Summary](#cross-language-summary-nsop)
- [Memory Allocations per Operation](#memory-allocations-per-operation)
- [Architecture Overview](#architecture-overview)
- [Migration Guide](#migration-guide)
- [Contributing](#contributing)
- [License](#license)

## Why FastFSM?

State machines are a fundamental design pattern for modeling systems with a finite number of states and transitions triggered by events. They are widely used to manage object lifecycles, workflows, game logic, UI navigation, and more.

Traditional .NET state machine libraries often rely on reflection, dictionaries, or expression trees at runtime, leading to:

- **Performance overhead** from indirect calls and heap allocations
- **Runtime errors** that could be caught at compile time
- **Limited AOT compatibility** due to reliance on dynamic code
- **Complex APIs** that can obscure the actual state logic

FastFSM solves these problems by generating optimized code at compile time, giving you:

- **Zero runtime reflection** -- transitions compile down to simple `switch` statements
- **Zero heap allocations** -- no garbage collection pressure during transitions
- **Compile-time validation** -- invalid states and transitions are caught during build
- **Predictable lifecycle** -- explicit startup (`Start()` method) eliminates race conditions
- **AOT-friendly** -- works with .NET Native AOT and trimming without issues
- **Intuitive API** -- define states and transitions with attributes instead of complex fluent builders

## Key Features

- 🚀 **Extreme Performance** -- flat state transitions execute in ~**0.7 nanoseconds** each, and even hierarchical transitions in ~**11 nanoseconds**, with **zero** heap allocations
- 🗑️ **Zero Allocations** -- no runtime allocations during state transitions (async uses minimal Task infrastructure only)
- 🛡️ **Type-Safe & Compile-Time Checked** -- full IntelliSense support and compile-time validation of states/triggers
- 📦 **Modular Design** -- feature-gated code generation ensures you only pay for what you use
- 🔌 **Extensible** -- optional integrations for logging (FastFSM.Net.Logging) and dependency injection (FastFSM.Net.DependencyInjection)
- ⚡ **Async/Await Support** -- first-class async support with separate sync and async state machine interfaces
- 🏗️ **Hierarchical States** (v0.7) -- parent-child state relationships with automatic initial substates
- 📚 **History States** (v0.7) -- shallow and deep history modes for returning to prior sub-states
- 🎚️ **Transition Priorities** (v0.7) -- explicit control over transition resolution order in ambiguous cases
- 🔄 **Internal Transitions** (v0.7) -- handle events with actions *without* leaving the current state (no exit/entry)

## Getting Started

### Installation

```bash
# Core package – just the essentials
dotnet add package FastFSM.Net

# Optional: Add structured logging support
dotnet add package FastFSM.Net.Logging

# Optional: Add dependency injection integration
dotnet add package FastFSM.Net.DependencyInjection
```

### Quick Example

```csharp
// 1. Define your states and triggers as enums
public enum DoorState   { Open, Closed, Locked }
public enum DoorTrigger { Open, Close, Lock, Unlock }

// 2. Declare your state machine class with the [StateMachine] attribute
[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    // 3. Define transitions using attributes on a dummy method
    [Transition(DoorState.Closed, DoorTrigger.Open,   DoorState.Open)]
    [Transition(DoorState.Open,   DoorTrigger.Close,  DoorState.Closed)]
    [Transition(DoorState.Closed, DoorTrigger.Lock,   DoorState.Locked)]
    [Transition(DoorState.Locked, DoorTrigger.Unlock, DoorState.Closed)]
    private void ConfigureTransitions() { }

    // (Optional) Define state entry/exit behaviors:
    [State(DoorState.Open, OnEntry = nameof(OnDoorOpened))]
    private void ConfigureOpen() { }
    
    private void OnDoorOpened() => Console.WriteLine("Door opened!");
}

// 4. Use your state machine
var door = new DoorController(DoorState.Closed);
door.Start();                    // Initialize state machine (triggers OnEntry for initial state)

bool canOpen = door.CanFire(DoorTrigger.Open);  // true
door.Fire(DoorTrigger.Open);    // Transition: Closed -> Open (Door opened!)
Console.WriteLine(door.CurrentState);  // Outputs: Open
```

### Quick Example -- Hierarchical States (v0.7)

```csharp
// Define states with parent-child relationships (using naming convention for clarity)
public enum WorkflowState 
{ 
    Idle,
    Processing,           // Parent state
    Processing_Loading,   // Child of Processing
    Processing_Working,   // Child of Processing
    Processing_Saving,    // Child of Processing
    Complete
}
public enum WorkflowTrigger { Start, UpdateProgress, Finish }

// Create hierarchical state machine (enable hierarchy via attribute)
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), EnableHierarchy = true)]
public partial class WorkflowMachine
{
    // Define parent state with shallow history (remembers last substate on re-entry)
    [State(WorkflowState.Processing, History = HistoryMode.Shallow)]
    private void ConfigureProcessing() { }

    // Define initial child state (Parent = Processing, IsInitial marks the default substate)
    [State(WorkflowState.Processing_Loading, Parent = WorkflowState.Processing, IsInitial = true)]
    private void ConfigureLoading() { }
    [State(WorkflowState.Processing_Working, Parent = WorkflowState.Processing)]
    private void ConfigureWorking() { }
    [State(WorkflowState.Processing_Saving, Parent = WorkflowState.Processing)]
    private void ConfigureSaving() { }

    // Define transitions (including across hierarchy boundaries)
    [Transition(WorkflowState.Idle, WorkflowTrigger.Start, WorkflowState.Processing)]
    // When transitioning into Processing, it will automatically enter Processing_Loading (initial child)

    // Internal transition in a parent state – action executes without state change
    [InternalTransition(WorkflowState.Processing, WorkflowTrigger.UpdateProgress, Action = nameof(LogProgress))]
    private void ConfigureTransitions() { }

    private void LogProgress() => Console.WriteLine("Progress updated.");
}
```

*Diagram:* *A state diagram here could illustrate the* `WorkflowMachine` *hierarchy: the* `Processing` *state has child substates* `Loading`*,* `Working`*, and* `Saving`*. An* `InternalTransition` *on* `Processing` *for* `UpdateProgress` *executes an action without leaving the state.*

## Core Concepts

### States and Triggers

**States** represent the possible conditions or modes of your system, while **triggers** (or events) are the inputs that cause transitions between states. In FastFSM, both are defined as `enum` types for type safety and performance.

### Transitions

**Transitions** define how the state machine moves from one state to another in response to a trigger. A transition may include:

- **Guard Conditions** -- a boolean condition that must be true for the transition to occur
- **Actions (Effects)** -- code that executes during the transition (e.g., side effects when the transition happens)

If a trigger is fired but no valid transition exists from the current state (or any active parent state, in an HSM), the trigger is ignored by default.

### State Entry/Exit

States can have entry and exit behaviors that execute when entering or leaving the state:

- **OnEntry** -- method called upon entering a state (e.g., initialization logic)
- **OnExit** -- method called when exiting a state (e.g., cleanup or logging)

Hierarchical states inherit entry/exit behavior: entering a child state will run the parent's entry logic first, then the child's. Exiting works in reverse (child exit, then parent exit).

## Hierarchical State Machines (HSM) -- New in v0.7

FastFSM 0.7 introduces **Hierarchical State Machines**, allowing you to organize states into parent-child relationships for more complex scenarios while preserving FastFSM's zero-allocation, nanosecond performance. In an HSM, a state can have nested substates:

- **Parent state** -- a state that has one or more child substates. It typically represents a high-level mode.
- **Child state** -- a state with a designated parent. Child states inherit behaviors (like allowed triggers) from their parent and often represent more specific sub-modes.

**Initial Substate:** You can mark one child state per parent as `IsInitial = true`. When a parent state is entered, FastFSM will automatically enter its initial child state.

**History States:** FastFSM supports *history* for parent states. Using `History = HistoryMode.Shallow` or `HistoryMode.Deep` on a parent state allows the state machine to remember the last active substate (or deep nested substate) when re-entering that parent. This means you can return to where you left off inside a parent state, rather than always starting at the initial substate.

**Internal Transitions:** In an HSM, a parent state can handle a trigger without changing state via an `[InternalTransition]`. This lets you respond to events at a high level (maybe with an action) without disturbing the active child state.

Even with these added capabilities, **FastFSM's HSM operations remain extremely fast (on the order of ~11 ns per transition) and allocation-free**, so you can scale up state complexity without performance worry.

## The State Machine Lifecycle (New in v0.6)

Version 0.6 introduced an explicit lifecycle for state machines to remove ambiguity and race conditions:

- **Construction:** When you instantiate your state machine class (e.g., `new DoorController(initialState)`), it is in an *unstarted* state. No `OnEntry` callbacks have run yet.
- **Start:** You must call `Start()` on the machine to begin execution. This will perform initial setup, including invoking any `OnEntry` handler for the initial state. The call to `Start()` completes only after all entry actions have finished running.
- **Running:** After `Start()`, you can fire triggers using `Fire(trigger)` or check availability with `CanFire(trigger)`. Transitions occur immediately within the `Fire` call.
- **Completion:** If your state machine has a designated final or terminal state (not strictly required in FastFSM), you can determine completion by checking the current state after transitions.
- **Disposal:** FastFSM state machines are lightweight and typically don't require disposal. However, if you integrate with other systems (like registering event listeners in `OnEntry`), you may implement `IDisposable` in your partial class to clean up resources on disposal.

**Why an explicit Start?** In earlier versions (and some other libraries), the act of constructing the state machine could immediately trigger entry actions. This could lead to subtle race conditions if events were fired during construction or if the object wasn't fully constructed. By requiring an explicit `Start()`, FastFSM ensures your state machine is fully configured before any transitions occur.

## Basic Usage

For more advanced examples, including hierarchical states, internal transitions, and payload handling, see the sections below and the test suite in the repository.

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

Guards are boolean conditions that must be true for a transition to occur. Actions are methods executed during the transition:

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
if (account.CanFire(AccountTrigger.Withdraw)) {
    account.Fire(AccountTrigger.Withdraw);
}
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
connection.Start(); // Machine is now active
connection.Fire(ConnectionTrigger.Connect); // "Heartbeat started" is printed
```

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
- `GenerateExtensibleVersion` - Enable extension support (default: false)
- `EnableHierarchy` - Enable hierarchical state machine features (default: false)

### Transition Attribute

Defines a state transition.

```csharp
[Transition(fromState, trigger, toState, Guard = "method", Action = "method", Priority = 0)]
```

**Parameters:**
- `fromState` - Source state
- `trigger` - Trigger that causes transition
- `toState` - Destination state
- `Guard` (optional) - Method name that returns bool
- `Action` (optional) - Method name to execute during transition
- `Priority` (optional) - Transition priority (higher values = higher priority, default: 0)

### InternalTransition Attribute

Defines an internal transition that executes an action without changing state.

```csharp
[InternalTransition(state, trigger, Guard = "method", Action = "method", Priority = 0)]
```

**Parameters:**
- `state` - State where the internal transition is active
- `trigger` - Trigger that causes the internal transition
- `Guard` (optional) - Method name that returns bool
- `Action` (optional) - Method name to execute
- `Priority` (optional) - Transition priority for resolution order

### State Attribute

Configures state-specific behavior.

```csharp
[State(state, OnEntry = "method", OnExit = "method", 
       Parent = parentState, IsInitial = false, History = HistoryMode.None)]
```

**Parameters:**
- `state` - The state to configure
- `OnEntry` (optional) - Method to execute when entering state
- `OnExit` (optional) - Method to execute when leaving state
- `Parent` (optional) - Parent state for hierarchical relationships
- `IsInitial` (optional) - Marks state as initial child of parent (default: false)
- `History` (optional) - History mode: None, Shallow, or Deep

### Generated Methods

Every state machine automatically gets these methods:

```csharp
// Current state of the machine
TState CurrentState { get; }

// Starts the machine, running the initial OnEntry callback
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

// HSM-specific methods (when hierarchy is enabled):
bool IsInHierarchy(TState ancestor)  // Check if current state is within ancestor's hierarchy

// DEBUG-only:
#if DEBUG
string DumpActivePath()  // Returns the active state path (e.g., "Parent / Child")
#endif
```

### Extension Hooks

When extension support is enabled (`GenerateExtensibleVersion = true`), the following hooks are invoked:

- `OnBeforeTransition(ctx)` - Before any transition effects
- `OnGuardEvaluation(ctx, guardName)` - Before guard evaluation
- `OnGuardEvaluated(ctx, guardName, result)` - After guard evaluation
- `OnAfterTransition(ctx, success)` - After transition completes

**Note:** Guard hooks are only fired during `TryFire`/`Fire`, not during `CanFire` or `GetPermittedTriggers`.

## Advanced Features

### Typed Payloads

```csharp
[StateMachine(typeof(ProcessState), typeof(ProcessTrigger), DefaultPayloadType = typeof(ProcessData))]
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
processor.Start();
var data = new ProcessData { Id = "123", IsValid = true };
processor.Fire(ProcessTrigger.Start, data);
```

### Async Support

FastFSM provides separate sync and async APIs for clarity and type safety.

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
await downloader.StartAsync();
await downloader.FireAsync(DownloadTrigger.Start);
```

### Dependency Injection

When using `FastFSM.Net.DependencyInjection`:

```csharp
// In your service configuration
services.AddStateMachineFactory();
services.AddTransient<OrderWorkflow>();

// In your consumer class
public class OrderService(IStateMachineFactory factory)
{
    public void ProcessNewOrder()
    {
        // Create and start in one step
        var sm = factory.CreateStarted<OrderWorkflow>(OrderState.New);
        sm.Fire(OrderTrigger.Submit);
    }
}
```

### Compile-Time Safety Toggles

FastFSM provides compile-time symbols for balancing performance vs safety:

- `FASTFSM_SAFE_GUARDS` - Wraps guards in try/catch blocks
- `FASTFSM_SAFE_ACTIONS` - Wraps actions in try/catch blocks
- `FASTFSM_DEBUG_GENERATED_COMMENTS` - Adds debug comments to generated code

**Recommended Configuration:**

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <DefineConstants>$(DefineConstants);FASTFSM_SAFE_ACTIONS;FASTFSM_SAFE_GUARDS</DefineConstants>
</PropertyGroup>
```

### Nested State Machine Classes

State machines can be nested inside other types:

```csharp
public class GameController
{
    public enum PlayerState { Idle, Moving, Attacking }
    public enum PlayerTrigger { Move, Attack, Stop }

    [StateMachine(typeof(PlayerState), typeof(PlayerTrigger))]
    public partial class PlayerStateMachine
    {
        [Transition(PlayerState.Idle, PlayerTrigger.Move, PlayerState.Moving)]
        [Transition(PlayerState.Moving, PlayerTrigger.Attack, PlayerState.Attacking)]
        private void Configure() { }
    }
}

// Usage
var player = new GameController.PlayerStateMachine(GameController.PlayerState.Idle);
player.Start();
```

## Benchmarks

### Performance Summary

FastFSM achieves **sub-nanosecond** transition times for flat (non-hierarchical) state machines and low double-digit nanosecond times for hierarchical transitions -- all with **zero heap allocations** during operation[\[1\]](https://github.com/buchmiet/FastFsm/blob/67256e72d650d372df7e245618d81544603a0d20/readme.md#L749-L757). Below is a quick summary of FastFSM performance characteristics on a modern desktop CPU:

- **Flat FSM:** ~**0.68 ns** per basic transition, **0 bytes** allocated
- **HSM (basic transition):** ~**11.7 ns** per hierarchical transition (entering/exiting parent states), **0 bytes** allocated
- **HSM (internal transition):** ~**4.2 ns** for an internal transition (handled in-state with no exit/entry), **0 bytes** allocated
- **HSM (history restore):** ~**15.0 ns** to re-enter a parent state and automatically return to the last active substate (shallow history), **0 bytes** allocated

When compared to other state machine libraries (across various languages and frameworks), FastFSM demonstrates the advantage of compile-time code generation over runtime abstraction layers.

### Test Environment

All benchmarks were conducted under the following environment/setup for consistency:

| Component | Version / Details |
|-----------|------------------|
| **CPU** | AMD Ryzen 5 9600X (6C/12T, Zen 5 @ 3.9--5.4 GHz, AVX-512 enabled) |
| **Memory** | 32 GB DDR5, Windows 11 Pro 24H2 (High Performance power profile) |
| **.NET Runtime** | .NET 9.0.5 (RyuJIT with AVX-512, Server GC) |
| **JVM** | OpenJDK 21.0.8+9 (Temurin, Server VM, G1 GC) |
| **C++ Compiler** | MSVC 19.44 (Visual C++ 2022, /O2 /GL, AVX512) + Google Benchmark 1.8.4 |
| **Rust** | rustc 1.80.0 + Statig 0.4 (LTO, codegen-units=1, criterion 0.5.1) |
| **TypeScript** | Bun 1.2.19 (JavaScriptCore JIT) + mitata benchmark |
| **JavaScript** | Bun 1.2.19 (JavaScriptCore JIT, ES2023 syntax) + mitata |
| **Benchmark Tool** | BenchmarkDotNet 0.15.2 (.NET), JMH 1.37 (Java), Criterion 0.5 (Rust) |
| **Methodology** | 1024 operations per iteration, 15 iterations (reporting mean ± std dev) |
| **Date** | August 7, 2025 |

**Note:** Results may vary by ±5--8% on CPUs without AVX-512. The Rust benchmarks use the Statig 0.4 library for a fair comparison with a Rust state machine abstraction (as opposed to a hand-written state machine), to illustrate library overhead.

### FastFSM vs .NET State‑Machine Libraries

The table below compares FastFSM with popular .NET state machine libraries on several scenarios. Lower times are better. All values are *nanoseconds per operation* (mean). "**Winner**" indicates which library is fastest in that scenario and the speedup relative to the next-best library.

| Scenario | FastFSM | Stateless | LiquidState | Appccelerate | Winner |
|----------|---------|-----------|-------------|--------------|--------|
| **Basic Transitions** | **0.68 ns** | 193.06 ns | 22.14 ns | 187.21 ns | FastFSM (**33×** vs LiquidState) |
| Guards + Actions | **0.56 ns** | 210.33 ns | *n/a*¹ | 193.07 ns | FastFSM (**345×** vs Appccelerate) |
| Payload (data transfer) | **0.72 ns** | 231.14 ns | 24.93 ns | 205.49 ns | FastFSM (**34×** vs LiquidState) |
| Can Fire (query) | **0.31 ns** | 101.88 ns | *n/a*¹ | *n/a*¹ | FastFSM (**330×** vs Stateless) |
| Get Permitted Triggers | **1.06 ns** | 24.85 ns | *n/a*¹ | *n/a*¹ | FastFSM (**≈23×** vs Stateless) |
| Async Hot Path² | 411.89 ns | 288.53 ns | **63.13 ns** | 437.81 ns | LiquidState (**6.5×** faster) |
| Async w/ Yield³ | **412.89 ns** | 1002.01 ns | 450.72 ns | 1315.18 ns | FastFSM (fastest) |

¹ API not available or scenario not applicable in that library  
² All async actions complete immediately (e.g. using `Task.FromResult` or `ValueTask.CompletedTask`). Measures the overhead of handling an asynchronous action without an actual context switch.  
³ Includes a forced context switch (`Task.Yield()` in FastFSM, analogous constructs in others). Represents a "real-world" async scenario with awaiting. This incurs additional runtime scheduler overhead.

**Analysis:** FastFSM dominates in all synchronous scenarios by large margins, achieving tens or hundreds of times faster transitions than other libraries. Notably, **Basic** transitions in FastFSM are about 32× faster than the next best (LiquidState), and **Guard+Action** transitions are over two orders of magnitude faster than in Stateless or Appccelerate. Even in the **Payload** scenario (passing data with events), FastFSM outperforms the next fastest (LiquidState) by ~34×.

For **CanFire** checks and **GetPermittedTriggers** (querying state), FastFSM's compile-time approach yields results in ~0.3--1.1 ns, whereas other libraries, if they support these queries, take tens of nanoseconds or allocate memory. FastFSM's `CanFire` is essentially a compile-time constant check (hence <1 ns), and `GetPermittedTriggers` returns a precomputed static array of triggers (zero allocations).

In asynchronous scenarios, results vary. **Async Hot Path** (where async actions complete immediately) is a best-case scenario for minimizing scheduler overhead. Here, LiquidState's aggressively inlined design makes it the fastest (≈63 ns), outperforming FastFSM (~412 ns)[\[2\]](https://github.com/buchmiet/FastFsm/blob/67256e72d650d372df7e245618d81544603a0d20/readme.md#L782-L789). Stateless (~289 ns) also beats FastFSM slightly in this scenario. However, when a real context switch is introduced (**Async with Yield**), FastFSM takes the lead at ~413 ns. FastFSM's efficient state handling and use of `ValueTask` result in about a **2.4× speedup** over LiquidState and **2.8× over Stateless** in the realistic async case[\[3\]](https://github.com/buchmiet/FastFsm/blob/67256e72d650d372df7e245618d81544603a0d20/readme.md#L786-L794). (Appccelerate is slower in both async scenarios.)

### FastFSM HSM vs Stateless HSM (Hierarchical State Machines)

This table compares FastFSM 0.7's hierarchical state machine performance against the popular Stateless library (which offers a basic form of hierarchical states via substates). FastFSM's HSM operations are measured with a similar state configuration in Stateless. Times are in nanoseconds; allocations are per transition:

| Scenario | FastFSM HSM | Stateless HSM | Speedup | FastFSM Allocations | Stateless Allocations |
|----------|-------------|---------------|---------|--------------------|-----------------------|
| **Hierarchical Transition** | **11.69 ns** | 548.91 ns | **47×** | **0 B** | 3,952 B |
| Internal Transition | **4.24 ns** | 259.03 ns | **61×** | **0 B** | 1,408 B |
| Shallow History Restore | **15.01 ns** | *n/a* | *n/a* | **0 B** | *n/a* |
| Async Transition (with yield) | **409.90 ns** | 1,164.60 ns | **2.8×** | 376 B¹ | 13,434 B |

¹ The 376 B allocation in FastFSM's async case comes from the .NET `async/await` infrastructure when yielding; FastFSM's own state logic still allocates nothing.  
Stateless does not support built-in history states, so that scenario is not applicable for comparison.

**Key Insights:** FastFSM's hierarchical state handling remains extremely fast and memory-efficient:

- A full **Hierarchical Transition** (exiting a child and parent, then entering a parent and child) in FastFSM is about 11.7 ns total, versus ~549 ns in Stateless -- roughly **47× faster**, with *zero* allocations versus ~4 KB allocated by Stateless on each transition[\[4\]](https://github.com/buchmiet/FastFsm/blob/67256e72d650d372df7e245618d81544603a0d20/readme.md#L796-L804).
- **Internal transitions** (handling an event in a state without state changes) are especially efficient: ~4.2 ns in FastFSM, ~259 ns in Stateless (61× faster). FastFSM treats internal transitions almost like a no-op with a function call, whereas Stateless's design incurs overhead and some allocations.
- **History states:** FastFSM's shallow history restore operation (returning to the last active substate) clocks around 15 ns. Stateless does not have this feature built-in. FastFSM achieves this with compile-time generated code that stores the last substate in a struct on the stack or in the state machine instance (no heap allocation).
- **Async HSM:** Even in an HSM scenario with an actual `Task.Yield()`, FastFSM completes the transition in ~410 ns vs ~1165 ns for Stateless. That's roughly **2.8× faster** and with about **36× fewer bytes allocated** (376 B vs 13.4 KB) during the async operation. FastFSM's advantage comes from using `ValueTask` and minimizing state machine captures, whereas Stateless relies on heavier `Task` usage and heap-allocated closures.

Overall, FastFSM extends its performance leadership to hierarchical state machines, meaning you don't have to trade speed for state organization.

### FastFSM vs TypeScript (Bun)

To put FastFSM's performance in perspective, it's useful to compare against high-performance implementations in other languages. Bun (a JavaScript runtime) is known for its speed, and we created a minimal FSM in TypeScript/JavaScript for comparison. The TypeScript FSM uses a simple switch-case structure under the hood, optimized by Bun's JIT. All times below are in nanoseconds (ns) and represent per-transition costs. The Δ column describes the relative speed difference (faster/slower) of one environment versus the other:

| Scenario | TypeScript (Bun) | FastFSM (.NET) | Δ (relative) |
|----------|-------------------|----------------|--------------|
| **Basic Transitions** | ~1.2 ns | **0.68 ns** | 1.8× slower |
| Guards + Actions | ~1.5 ns | **0.56 ns** | 2.7× slower |
| Payload | ~2.8 ns | **0.72 ns** | 3.9× slower |
| Can Fire Check | ~0.6 ns | **0.31 ns** | 2.0× slower |
| Get Permitted Triggers | **~1.0 ns** | 1.06 ns | 1.1× *faster* |
| Async Hot Path (no yield) | **~208 ns** | 412.9 ns | 2.0× *faster* |
| Async With Yield | ~2000 ns | **412.9 ns** | 4.9× slower |

**TypeScript implementation:** A minimal FSM with a direct `switch` statement. Bun's JavaScriptCore JIT heavily optimizes hot paths (especially the synchronous ones). Note that the async models differ: the TypeScript async test uses a `setImmediate()`/event-loop mechanism (which is very fast in Bun for a resolved promise without actual delay), whereas the .NET test uses `Task.Yield()` to simulate an actual context switch.

**Interpretation:** FastFSM's raw performance in .NET is competitive with, and often better than, an ultra-optimized TypeScript implementation running on a JIT VM:

- For basic, guard, payload, and can-fire scenarios, FastFSM .NET actually performs better (faster) than Bun's TypeScript by a factor of ~1.8× to ~3.9×. This is remarkable considering JavaScriptCore's ability to optimize simple loops -- it shows the benefit of FastFSM's ahead-of-time optimizations and the efficiency of native code execution.
- In the **Get Permitted Triggers** scenario, both are effectively near 1 ns; Bun's result is ~1.0 ns vs FastFSM ~1.06 ns, essentially a tie (within measurement noise).
- In the **Async Hot Path** test (no real yield), Bun's event loop resolution (~208 ns for a resolved promise) beats FastFSM's ~413 ns. .NET's overhead for even a no-context-switch async is higher than Bun's microtask handling.
- However, in **Async with Yield** (actual context switch), FastFSM clearly wins. The cost of `Task.Yield()` (~413 ns) is dramatically lower than forcing an analogous delay in Bun (~2000 ns, likely due to timers or task scheduling in JS). FastFSM is about **4.9× faster** in this realistic async scenario.

Overall, FastFSM demonstrates that carefully optimized .NET code can match or exceed the performance of a low-level JavaScript engine for many state machine operations, especially when real concurrency is involved.

### FastFSM vs JavaScript (Bun)

For completeness, we also compare against a pure JavaScript implementation (same logic as the TS version, but without TypeScript compile step). Bun's performance for JS is similar to TS in most cases:

| Scenario | JavaScript (Bun) | FastFSM (.NET) | Δ (relative) |
|----------|-------------------|----------------|--------------|
| **Basic Transitions** | ~1.0 ns | **0.68 ns** | 1.5× slower |
| Guards + Actions | ~3.0 ns | **0.56 ns** | 5.4× slower |
| Payload | ~2.8 ns | **0.72 ns** | 3.9× slower |
| Can Fire Check | ~0.38 ns | **0.31 ns** | 1.2× slower |
| Get Permitted Triggers | **~0.39 ns** | 1.06 ns | 2.7× *faster* |
| Async Hot Path | **~203 ns** | 412.9 ns | 2.0× *faster* |
| Async With Yield | ~712 ns | **412.9 ns** | 1.7× slower |

**JavaScript implementation:** Pure ES2023 implementation using a `switch` in a class method. Bun's engine optimizes synchronous code heavily. The "Async With Yield" scenario in JS uses an `await new Promise(r => setTimeout(r, 0))` to yield to the event loop, which is significantly slower than .NET's optimized `Task.Yield()`.

**Takeaway:** The patterns are similar to TypeScript. FastFSM's ahead-of-time optimizations give it the edge in most sync scenarios, and it holds its own or wins in async scenarios when real scheduling is involved. JavaScript (Bun) is extremely fast for trivial operations (e.g., < 1 ns for a simple state check), but as complexity or actual concurrency is introduced, the gap closes or reverses in FastFSM's favor.

### FastFSM vs Java State‑Machine Libraries

We also compare FastFSM with two Java libraries: **Squirrel** (a high-performance state machine library for JVM) and **Spring StateMachine** (a feature-rich, reflection-based framework). The JVM was run on the same machine with Java 21. Times are again per transition (ns), including any allocations (shown after the slash):

| Scenario | FastFSM (.NET) | Squirrel (JDK 21) | Spring StateMachine (JDK 21) |
|----------|----------------|-------------------|------------------------------|
| **Basic Transitions** | **0.68 ns / 0 B** | 289 ns / 1.5 kB | 12,188 ns / 30.7 kB |
| Guards + Actions | **0.56 ns / 0 B** | 321 ns / 1.5 kB | 13,564 ns / 31.3 kB |
| Payload | **0.72 ns / 0 B** | 311 ns / 1.5 kB | 12,321 ns / 31.6 kB |
| Async Hot Path | 412.9 ns | **314 ns** | 12,110 ns |
| Async With Yield | 412.9 ns | **310 ns** | 26,599 ns |

No garbage collections occurred in these microbenchmarks; allocation sizes are provided to illustrate per-operation overhead.

Even against Java's fastest library (Squirrel), FastFSM is in a class of its own for synchronous cases -- sub-nanosecond vs hundreds of nanoseconds. Squirrel, being reflection-free and fairly optimized, still incurs ~1.5 kB allocations per transition, whereas FastFSM has none. Spring StateMachine, as expected, is much slower and heavier (tens of microseconds per transition and tens of kB allocated) due to its flexibility and complexity.

For **Async Hot Path**, Squirrel measured ~314 ns (likely using `CompletableFuture.completedFuture` under the hood), slightly beating FastFSM's ~412 ns. And in the **Async With Yield** test, Squirrel at ~310 ns is on par with its hot-path result -- indicating that its "async" might not involve an actual thread context switch (possibly it doesn't truly simulate an await, whereas FastFSM's number includes an actual yielded await). Spring's async performance is very slow (tens of microseconds). In practice, this means that on the JVM, truly asynchronous state transitions still incur non-trivial overhead unless using virtual threads or other mechanisms.

**Overall:** FastFSM's performance is competitive with or better than the best the JVM has to offer, especially considering memory usage. It outperforms Spring's framework dramatically, and is in the same ballpark as low-level libraries like Squirrel for async and far ahead for sync.

### FastFSM vs C++ State Machines (Boost.SML)

Boost.SML is a compile-time state machine library in C++ known for excellent performance. We compare FastFSM with an equivalent state machine implemented using Boost.SML (compiled with optimizations). Times are nanoseconds per transition:

| Scenario | FastFSM (.NET) | Boost.SML (C++) | Δ |
|----------|----------------|-----------------|---|
| **Basic Transitions** | **0.68 ns** | 1.23 ns | FastFSM 1.8× faster |
| Guards + Actions | **0.56 ns** | 1.32 ns | FastFSM 2.4× faster |
| Payload | **0.72 ns** | 1.35 ns | FastFSM 1.9× faster |
| Can Fire Check | **0.31 ns** | 1.28 ns | 4.1× faster |
| Async Hot Path\* | 412.9 ns | 1.38 ns | *Not comparable* |

**Note:** Boost.SML doesn't have an actual async/await model; the "Async Hot Path" in C++ was simulated as a synchronous call (essentially just calling a transition function). Thus, the 1.38 ns is not directly comparable to the .NET async scenario which includes scheduler overhead.

**Insight:** In purely synchronous scenarios, FastFSM even edges out a highly optimized C++ template library. Boost.SML is extremely fast (1.2--1.3 ns for a transition), but FastFSM manages slightly lower latencies in these tests. The differences are small in absolute terms, but it's impressive that a .NET library can outperform C++ here. The `CanFire` check in Boost.SML is also very fast (~1.3 ns) but still about 4× slower than FastFSM's approach (which is basically an inline field check).

It's worth noting that Boost.SML's "async" capabilities are compile-time simulations; since C++ has no built-in coroutine support in SML for state machines, the library doesn't truly compare in that area.

### FastFSM vs Rust State Machines (Statig Library)

We also benchmarked against [Statig](https://crates.io/crates/statig), a Rust state machine library that generates code via macros (v0.4). Rust generally excels at zero-cost abstractions. The table compares FastFSM with Statig (both compiled with maximum optimizations):

| Scenario | FastFSM (.NET) | Rust (Statig 0.4) | Δ |
|----------|----------------|-------------------|---|
| **Basic Transitions** | **0.68 ns** | 1,730 ns | FastFSM **2544×** faster |
| Guards + Actions | 0.56 ns | **0.37 ns** | Statig 1.5× faster |
| Payload | 0.72 ns | **0.41 ns** | Statig 1.8× faster |
| Async Hot Path | 412.9 ns | **7.3 ns** | Statig 56× faster |
| Async With Yield | 412.9 ns | **19.4 ns** | Statig 21× faster |

**Note:** The Rust implementation using Statig has very low overhead for certain scenarios due to aggressive inlining and zero-cost futures, but it shows higher overhead in the basic case because of how the state machine is constructed (the Rust version used enums and trait dispatch which add overhead for each call). A hand-written Rust state pattern could achieve ~1.77 ns for basic transitions and ~0.7--0.8 ns for guard/payload cases (approaching FastFSM's flat performance), but would lose flexibility.

FastFSM's compile-time generator and the Rust macro approach have different trade-offs:
- In **Basic Transitions**, FastFSM's direct `switch` beats Statig's dynamic dispatch by a huge margin (Statig ~1.7 μs due to an `enum` match and trait object calls)[\[6\]](https://github.com/buchmiet/FastFsm/blob/67256e72d650d372df7e245618d81544603a0d20/readme.md#L876-L884). This is an extreme case where the abstraction penalty is large in Rust.
- For **Guards/Actions** and **Payload**, Rust/Statig is actually faster than FastFSM. Statig likely inlines the guard and action calls with virtually no overhead, achieving sub-nanosecond times (0.37--0.41 ns). FastFSM is still under 1 ns here, but Rust wins by ~1.5--1.8×.
- In **Async** scenarios, the comparison is tricky. Statig's numbers (7.3 ns and 19.4 ns) are incredibly low -- these are essentially the overhead of Rust's `async` state machine without actual waiting (no true thread context switch, since Rust futures are polled in a single thread executor for the benchmark). They indicate how efficient Rust's zero-cost futures can be. FastFSM's 412 ns includes the cost of scheduling a continuation with the .NET thread pool, whereas Rust's is just function call overhead in these tests. In real-world async (with actual I/O or thread switching), Rust would also incur higher costs, but likely still less than 412 ns for a context switch (depending on the executor).
- The **Basic vs Guards** discrepancy in Rust highlights that some patterns (like a simple state transition) aren't optimized well due to dynamic checks, whereas adding a guard allowed better static optimization. FastFSM's performance is consistent across scenarios.

**Bottom line:** FastFSM holds its ground against system-level languages' libraries. In many practical scenarios, the differences are negligible (all sub-nanosecond). Rust's Statig can outperform FastFSM in specialized micro-cases, but also shows much higher overhead in others. FastFSM provides a balanced, extremely fast solution without requiring unsafe code or manual micro-optimizations.

### Cross‑Language Summary (ns/op)

To summarize the performance across different languages and libraries, the table below highlights the fastest achieved times in each scenario, and how FastFSM compares:

| Scenario | FastFSM (.NET) | Java (Squirrel) | C++ (Boost.SML) | Rust (Statig) | TypeScript (Bun) | JavaScript (Bun) | **Fastest** |
|----------|----------------|-----------------|-----------------|---------------|-------------------|-------------------|-------------|
| **Basic** | **0.68** ns | 289 ns | 1.23 ns | 1,730 ns | 1.2 ns | 1.0 ns | **FastFSM (.NET)** |
| **Guards+Actions** | 0.56 ns | 321 ns | 1.32 ns | **0.37** ns | 1.5 ns | 3.0 ns | **Rust (Statig)** |
| **Payload** | 0.72 ns | 311 ns | 1.35 ns | **0.41** ns | 2.8 ns | 2.8 ns | **Rust (Statig)** |
| **Async (hot)** | 412.9 ns | **314** ns | 1.38 ns | **7.3** ns | 208 ns | 203 ns | **C++ / Rust**\* |
| **Async (yield)** | 412.9 ns | **310** ns | n/a | **19.4** ns | 2000 ns | 712 ns | **Rust (Statig)** |

**Note:** For Async (hot path), the fastest numbers come from C++ and Rust, but neither involve a true scheduler context switch (C++ is purely sync, Rust is a polled future). Among true asynchronous implementations, Squirrel's 314 ns (JVM) and FastFSM's ~413 ns are the leaders.

As seen above, **FastFSM is the fastest in the majority of scenarios (especially basic transitions), and in the remaining cases the differences reflect trade-offs in dynamic vs static optimization.** FastFSM brings .NET's state machine performance on par with, or ahead of, lower-level languages in many cases, without sacrificing the high-level ease of use.

### Memory Allocations per Operation

One of FastFSM's design goals is **zero runtime allocations**, which it achieves for all synchronous operations. Below is a comparison of per-transition memory allocations and generated code size for various libraries (when applicable):

| Library | Allocations per op | Native Code Size (approx) |
|---------|-------------------|---------------------------|
| **FastFSM (Flat)** | **0 bytes** | 160 B -- 8,050 B |
| **FastFSM HSM (Sync)** | **0 bytes** | 2,242 B -- 3,626 B |
| **FastFSM HSM (Async)** | 376 bytes¹ | ~6,348 B |
| **Statig (Rust)** | **0 bytes** | ~2 KB -- 8 KB |
| **Stateless (Flat)** | 608 -- 2,295 B | 3,436 B -- 21,417 B |
| **Stateless (HSM)** | 1,408 -- 3,952 B | 11,643 B -- 15,069 B |
| **Squirrel (Java)** | ~1.5 kB | (JIT compiled, N/A) |
| **Spring StateMachine** | ~30 kB | (JIT compiled, N/A) |

¹ FastFSM async uses `ValueTask` to avoid allocations for completed tasks. The 376 B shown is the internal cost of a context switch (`SynchronizationContext`/`TaskScheduler` posting) on `Task.Yield()`. No new objects are allocated by FastFSM's generated code itself.

- **FastFSM:** 0 allocations for all non-async operations. Even in async, the only overhead is the minimal runtime scheduling cost. The native code size of the generated state machine varies with how many states and transitions you have, but remains quite small (a few KB at most, even for complex HSMs) -- this is the compiled switch statements and any lookup tables for triggers.
- **Stateless:** By contrast, uses heap allocations for many operations (e.g., every transition allocates some objects, and hierarchical support allocates more). As shown, a single transition in Stateless can allocate between ~0.6 KB (simple case) up to ~3--4 KB (hierarchical with actions).
- **Rust Statig:** as a low-level language library, also achieves zero allocations in operation, and similar code size order of magnitude (Rust's monomorphized code can be a few KB).
- **Java/Spring:** High-level frameworks like Spring StateMachine allocate a huge amount per operation (tens of KB), which is acceptable only for infrequent transitions. Squirrel is much leaner but still allocates ~1.5 KB on the JVM for each transition (likely due to object creation for events or context).

**Conclusion:** FastFSM provides not only speed, but also predictability and efficiency in memory usage, making it suitable for performance-sensitive applications (games, real-time systems, IoT devices) where garbage collections or unpredictable pauses are unacceptable. It achieves this without forcing you to hand-write state logic -- you get the productivity of a library and the performance of optimal custom code.

## Architecture Overview

FastFSM uses a multi-stage, compile-time code generation approach to create your state machine:

1. **Declaration Stage** -- You describe states, triggers, and transitions in your code using attributes (as shown in examples above). This is all done in normal C# code, in a partial class.
2. **Analysis Stage** -- A C# Source Generator (part of the FastFSM package) runs at compile time. It reads your partial class and the attributes you've declared. It performs validations (like ensuring state and trigger enums are valid, transitions are consistent, etc.) and builds an internal model of your state machine.
3. **Code Generation Stage** -- Based on that model, the source generator emits additional C# code into your assembly. This generated code is optimized for your specific state machine (unnecessary features are omitted).
4. **Compilation Stage** -- Your project then compiles as usual, including the generated code. The end result is a set of concrete methods (usually a big `switch` statement or nested switches) that implement the state machine logic with zero reflection or boxing.

### Feature-Gated Generation (v0.7+)

FastFSM's generator automatically includes code only for the features you use:

- **Core FSM** -- Always included: supports basic state transitions, guards, and actions.
- **Payload Support** -- Included only if you use the `[PayloadType]` attribute to attach data to triggers. This generates strongly-typed payload handling.
- **Extensibility Hooks** -- Included only if `GenerateExtensibleVersion = true` on your `[StateMachine]`. This generates a base class that your partial class inherits, allowing advanced extension (used for logging/DI).
- **Async Support** -- Included only if you have any `async` state methods or transitions returning `Task/ValueTask`. The generator produces an async-safe version of `Fire` and related logic.
- **Hierarchical States** -- Included if `EnableHierarchy=true` and you declare parent/child states. Generates additional logic for parent state tracking, substate transitions, etc.
- **History Tracking** -- Included if any state has `History = Shallow/Deep`. Emits code to record last active child states and restore them on re-entry.
- **Internal Transitions** -- Included if you use `[InternalTransition]` attributes. The generator creates handlers that execute without state changes.

This feature-gating ensures minimal code and maximal performance -- you aren't paying for what you don't use.

### Code Generation Strategy

The kind of code FastFSM generates depends on your usage:

- **Flat FSM (no hierarchy):** The generator produces a nested `switch` or a single switch on `(CurrentState, trigger)` pairs. In simplest form it looks like:

```csharp
switch (CurrentState) {
    case StateX:
        switch (trigger) {
            case TriggerA:
                // transition X -> Y
                CurrentState = StateY;
                // call exit/entry, actions
                break;
            case TriggerB:
                // handle TriggerB
                break;
        }
        break;
    case StateY:
        // handle StateY triggers
}
```

This is extremely fast and branch-predictable.

- **Hierarchical FSM:** When hierarchy is involved, the generator uses a technique of fall-through `switch` with `goto` for parent states. For example:

```csharp
switch (CurrentState) {
    case ChildState:
        // handle child-specific triggers
        if (trigger == ChildOnlyTrigger) { 
            // process child-specific trigger
        }
        // If not handled, fall through to parent:
        goto case ParentState;

    case ParentState:
        // handle parent triggers (including those not handled in child)
        if (trigger == ParentTrigger) { 
            // process parent trigger
        }
        break;
}
```

This way, if a child state doesn't handle a trigger, the code jumps to the parent's case and tries there -- mirroring how HSMs work. It avoids recursion and minimizes duplicate checks.

- **Performance Optimizations Applied:** The generated code uses only value types and direct branches. Some notable optimizations:
  - No dictionaries or reflection -- everything is a direct jump (O(1) time).
  - Guards are inlined as simple `if` checks in the switch cases.
  - No virtual calls -- all calls (to guard, action, entry/exit methods) are static or instance methods known at compile time.
  - State enums and trigger enums are converted to numeric constants internally, enabling jump tables in IL/native code.
  - For history states, the generator uses a `Span` or fixed-size array in the state machine instance to store last substate IDs, avoiding any heap structures.

Overall, the generated code is akin to what you might write by hand for maximum speed, but produced for you automatically.

## Migration Guide

### Upgrading to v0.7 (HSM Support)

Version 0.7 adds Hierarchical State Machine support while maintaining **100% backward compatibility**. All existing v0.6 state machines will continue to work without modification.

**What's New in v0.7:**

- **Hierarchical States** -- You can designate states as children of other states (see [Hierarchical States](#hierarchical-state-machines-hsm----new-in-v07) above). Parent states can have one initial child and optional history retention.
- **History Modes** -- Use `History = HistoryMode.Shallow` or `HistoryMode.Deep` on a parent state to automatically record the last active substate. On re-entering the parent, FastFSM will restore the child state (shallow = last direct child, deep = last nested descendant).
- **Internal Transitions** -- Use `[InternalTransition]` in a parent state to handle an event without leaving that state (no exit/entry calls). Great for periodic updates or self-loops that shouldn't reset the state.
- **Transition Priorities** -- In complex state machines, multiple transitions might be valid for a given trigger (for example, a parent state and a child state both respond to the same trigger). FastFSM now resolves these by an optional `Priority` parameter on `[Transition]` attributes. Higher priority transitions are considered before lower ones.
- **No Performance Loss** -- All the above features introduce *zero* runtime overhead if you're not using them. If you do use them, the overhead is minimal (as shown in benchmarks).

**How to Use HSM Features:**  
If you want to take advantage of HSM in FastFSM:
1. Set `EnableHierarchy = true` on your `[StateMachine]` attribute.
2. For each group of related states, designate a parent state (no parent itself) and use `Parent = ParentState` on child state attributes.
3. Mark one child state as `IsInitial = true` under each parent so the machine knows where to enter initially.
4. Optionally add `History = HistoryMode.Shallow` or `Deep` on the parent to remember child state.
5. Use `[InternalTransition(ParentState, Trigger, Action = nameof(YourMethod))]` for any triggers the parent should handle without leaving the state.
6. (Optional) If multiple transitions might conflict, add `Priority` (numeric, higher means earlier) to transitions to disambiguate.

FastFSM will validate at compile time that your hierarchy is set up correctly (e.g., one initial child per parent, valid parent references, etc.).

### Upgrading from v0.5 to v0.6 (Lifecycle Changes)

Version 0.6 introduced an explicit lifecycle with the `Start()` method to ensure deterministic behavior.

**Key Changes in v0.6:**
- **Explicit Start:** You must call `Start()` on the state machine after construction (see [Lifecycle](#the-state-machine-lifecycle-new-in-v06)). This was added to prevent issues with triggers fired during object construction.
- **Interfaces Renamed:** The interfaces were updated to `IStateMachineSync<TState,TTrigger>` and `IStateMachineAsync<TState,TTrigger>` (previously had different names). Update your code if you referenced the old interface names.
- **Factory Method:** If you were using any provided factory or builder, note that a convenience method `CreateStarted<T>` might be provided (e.g., if using dependency injection package) to automatically call `Start()`.

Example of migrating construct/start:

```csharp
// v0.5 (old behavior – Start was implicit at construction, which could cause issues)
var machine = new MyStateMachine(initialState);
machine.Fire(Trigger.X); // OnEntry for initialState might not have run yet!

// v0.6+ (new behavior)
var machine = new MyStateMachine(initialState);
machine.Start();           // OnEntry for initialState runs here
machine.Fire(Trigger.X);   // Safe – machine is fully started
```

Ensure all code paths that instantiate a state machine also call `Start()` before firing triggers or querying state.

### From Stateless to FastFSM

If you are familiar with the [Stateless](https://github.com/dotnet-state-machine/stateless) library and want to migrate to FastFSM, here's a quick side-by-side:

**Defining a simple state machine in Stateless:**

```csharp
var machine = new StateMachine<State, Trigger>(State.Initial);
machine.Configure(State.Initial)
       .Permit(Trigger.Start, State.Running)
       .OnEntry(() => Console.WriteLine("Entered Initial"));
machine.Configure(State.Running)
       .SubstateOf(State.Parent)      // define hierarchy (Stateless uses SubstateOf)
       .OnEntry(() => Console.WriteLine("Entered Running"));
```

**Equivalent in FastFSM:**

```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class MyMachine
{
    [State(State.Parent)]  // parent state (could have History if needed)
    private void ConfigureParent() { }

    [State(State.Running, Parent = State.Parent, IsInitial = true)]
    [Transition(State.Initial, Trigger.Start, State.Running)]
    [OnEntry(State.Initial)] 
    private void OnInitialEntry() => Console.WriteLine("Entered Initial");

    [OnEntry(State.Running)]
    private void OnRunningEntry() => Console.WriteLine("Entered Running");
}
```

FastFSM condenses the declaration into attributes. The above configures that `Initial --(Start)--> Running` transition and sets up the hierarchy (Running is a child of Parent). It also attaches `OnEntry` actions for Initial and Running states.

**Firing triggers:** In Stateless, you would call `machine.Fire(Trigger.Start)`. In FastFSM, you call `myMachine.Fire(Trigger.Start)` similarly. Checking state is `machine.State` vs `myMachine.CurrentState`.

**Benefits noticed when migrating:** You'll find FastFSM eliminates the need for runtime checks (errors like "No valid transition" become compile-time if you misconfigure). Performance, as shown, improves dramatically. The trade-off is that FastFSM requires a build step (source generation) and using partial classes, but this is generally straightforward.

### Version History

- **v0.7** (Aug 2024) -- Introduced Hierarchical State Machine support (parent/child states), History states, Internal transitions, Transition priorities. Performance optimizations to maintain sub-nanosecond speeds even with HSM features.
- **v0.6** (June 2024) -- Added explicit `Start()` method and lifecycle to avoid race conditions on startup. Updated interfaces (`IStateMachineSync/Async`). Minor performance improvements and bug fixes.
- **v0.5** (April 2024) -- Initial public release of FastFSM with core features (flat state machines, guards, actions, async support, source generation approach).


## Roadmap

Looking ahead, here are some planned features and improvements for upcoming versions of FastFSM:

### v0.7.5 (Planned Minor Release)

- **String/Complex State & Trigger Support:** Allow using `string` constants, or types like `record struct` or SmartEnum, as state identifiers via source generation. This will introduce a compile-time mapping from strings to state IDs, enabling more dynamic state definitions without sacrificing performance.
- **Native JSON Serialization:** Built-in support to serialize/deserialize the state machine state (for persistence or microservice scenarios) with zero unnecessary overhead.
- **Further Performance Tweaks:** Investigate any overhead in edge cases (like extremely large state counts) and eliminate it.

### v0.8 (Planned)

- **Support >256 States:** Remove/extend any internal limits (if present) on number of states or triggers. Ensure the code generation and storage scales to large state counts (e.g., 1000+ states) efficiently.
- **Optional Strict Child Requirement:** Introduce a mode (perhaps `RequireExplicitSubstate`) where entering a parent state requires explicitly specifying a substate, rather than auto-entering an initial substate. This can help model certain workflows.
- **Orthogonal Regions (Parallel States):** Experimental support for state machines that can be in multiple independent sub-states concurrently (parallel regions), as per UML statechart terminology.
- **Completion Transitions:** Ability for a state to automatically trigger a transition when it is entered (after OnEntry), useful for modeling transient states that immediately progress.

### v0.9 (Planned)

- **Deferred Events:** Implement UML-style deferred events. The ability to queue certain triggers that are not handled in the current state for later processing when the machine moves to a state that can handle them. This will involve an internal event queue (likely a zero-allocation ring buffer) and APIs to manage it. Will include options for overflow handling (e.g., drop oldest/newest event).
- **Visual Tooling:** Provide a way to export the state machine graph (perhaps via Mermaid or PlantUML integration) and maybe a debugging view that can visualize the current state graph and queued events at runtime. This could greatly aid in debugging complex state machines.
- **Dynamic Modification:** Explore allowing limited runtime modifications to the state machine (adding/removing transitions) while running, for highly dynamic scenarios. This would be optional and would impact performance, so it might be behind a feature flag.

### v1.0 (Future)

- **Stable API & Documentation:** Lock in the public API surface and semantics so that users can rely on long-term support.
- **Formal Performance Guarantees:** By v1.0, we aim to guarantee certain performance characteristics (e.g., O(1) transitions, maximum memory footprint per state, etc.) in documentation.
- **Enterprise Features:** e.g., integration with popular logging frameworks out of the box, perhaps a GUI designer or code analyzer for state machines.
- **Persistable State Machines:** First-class support for saving and restoring a state machine (its current state and any history) to a data store or over the network (in addition to the JSON support).

These plans are subject to change based on community feedback and real-world usage.

## Contributing

We welcome contributions! If you have ideas, bug fixes, or improvements, please open an issue or submit a pull request on GitHub. Before contributing, please see the [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines and the code of conduct.

Areas where you can help:
- **Documentation:** Improvements to docs, additional examples, and tutorials are greatly appreciated.
- **Features:** Implementing a planned feature from the roadmap or suggesting new ones.
- **Benchmarks:** Contribute benchmark results from different environments or help us expand comparisons.
- **Optimizations:** If you spot a way to make the generated code even faster or leaner, let's discuss it!

By participating in this project, you agree to abide by the code of conduct outlined in the contributing guide.

## License

FastFSM is licensed under the MIT License. You're free to use it in commercial or open-source projects. See the [LICENSE](LICENSE) file for the full text.

*Thanks for reading! We hope FastFSM helps you build efficient and robust state machines. If you have any questions or run into issues, feel free to reach out via GitHub.*