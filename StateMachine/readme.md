# FastFSM - High-Performance State Machines for .NET

[](https://www.nuget.org/packages/FastFSM.Net/)
[](https://www.google.com/search?q=LICENSE)
[](https://dotnet.microsoft.com/download)

FastFSM is a powerful, zero-overhead finite state machine framework for .NET that leverages C# source generators to create highly optimized state machines at compile time. It combines the ease of declarative configuration with performance that rivals hand-written code.

## Table of Contents

  - [Why FastFSM?]
  - [Key Features]
  - [Getting Started]
  - [Core Concepts]
  - [The State Machine Lifecycle (New in 0.6)]
  - [Basic Usage]
  - [API Reference]
  - [Advanced Features]
  - [Performance]
  - [Real-World Examples]
  - [Architecture Overview]
  - [Migration Guide]
  - [Contributing]
  - [License]

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

-----

## Advanced Features

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

We welcome contributions! Please see our [Contributing Guide](https://www.google.com/search?q=CONTRIBUTING.md) for details.

## License

FastFSM is licensed under the MIT License. See [LICENSE](https://www.google.com/search?q=LICENSE) for details.

[1]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/%3Futm_source%3Dchatgpt.com\) "BenchmarkDotNet: Home"
[2]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/articles/configs/diagnosers.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/articles/configs/diagnosers.html%3Futm_source%3Dchatgpt.com\) "Diagnosers - BenchmarkDotNet"
[3]: https://www.google.com/search?q=%5Bhttps://github.com/dotnet/BenchmarkDotNet/issues/1832%3Futm_source%3Dchatgpt.com%5D\(https://github.com/dotnet/BenchmarkDotNet/issues/1832%3Futm_source%3Dchatgpt.com\) "[Proposal] OperationsPerInvoke to be fed by Params #1832 - GitHub"
[4]: https://www.google.com/search?q=%5Bhttps://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask%3Fview%3Dnet-9.0%26utm_source%3Dchatgpt.com%5D\(https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask%3Fview%3Dnet-9.0%26utm_source%3Dchatgpt.com\) "ValueTask Struct (System.Threading.Tasks) | Microsoft Learn"
[5]: https://www.google.com/search?q=%5Bhttps://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/%3Futm_source%3Dchatgpt.com%5D\(https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/%3Futm_source%3Dchatgpt.com\) "Understanding the Whys, Whats, and Whens of ValueTask - .NET Blog"
[6]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/api/BenchmarkDotNet.Engines.DeadCodeEliminationHelper.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/api/BenchmarkDotNet.Engines.DeadCodeEliminationHelper.html%3Futm_source%3Dchatgpt.com\) "Class DeadCodeEliminationHelper - BenchmarkDotNet"
[7]: https://www.google.com/search?q=%5Bhttps://fransbouma.github.io/BenchmarkDotNet/RulesOfBenchmarking.htm%3Futm_source%3Dchatgpt.com%5D\(https://fransbouma.github.io/BenchmarkDotNet/RulesOfBenchmarking.htm%3Futm_source%3Dchatgpt.com\) "Rules of benchmarking - BenchmarkDotNet Documentation"
[8]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/articles/features/event-pipe-profiler.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/articles/features/event-pipe-profiler.html%3Futm_source%3Dchatgpt.com\) "EventPipeProfiler - BenchmarkDotNet"
[9]: https://www.google.com/search?q=%5Bhttps://benchmarkdotnet.org/articles/samples/IntroEventPipeProfiler.html%3Futm_source%3Dchatgpt.com%5D\(https://benchmarkdotnet.org/articles/samples/IntroEventPipeProfiler.html%3Futm_source%3Dchatgpt.com\) "Sample: EventPipeProfiler - BenchmarkDotNet"