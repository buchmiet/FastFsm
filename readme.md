# FastFSM - High-Performance State Machines for .NET

[![NuGet](https://img.shields.io/nuget/v/FastFSM.Net.svg)](https://www.nuget.org/packages/FastFSM.Net/)
[![License](https://img.shields.io/github/license/yourusername/FastFSM)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%20%7C%209.0-blue)](https://dotnet.microsoft.com/download)

FastFSM is a powerful, zero-overhead finite state machine framework for .NET that leverages C# source generators to create highly optimized state machines at compile time. It combines the ease of declarative configuration with performance that rivals hand-written code.

## Table of Contents

- [Why FastFSM?](#why-fastfsm)
- [Key Features](#key-features)
- [Getting Started](#getting-started)
- [Core Concepts](#core-concepts)
- [Basic Usage](#basic-usage)
- [API Reference](#api-reference)
- [Advanced Features](#advanced-features)
- [Performance](#performance)
- [Real-World Examples](#real-world-examples)
- [Architecture Overview](#architecture-overview)
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
- **Native AOT ready** - fully compatible with trimming and ahead-of-time compilation
- **Intuitive API** - define states with simple attributes, no complex fluent builders

## Key Features

- 🚀 **Blazing Fast** - transitions execute in ~0.6 nanoseconds
- 🗑️ **Zero Allocations** - no heap allocations during state transitions
- 🛡️ **Type Safe** - full IntelliSense support and compile-time checking
- 📦 **Modular Design** - pay only for features you use
- 🔌 **Extensible** - optional logging, dependency injection, and custom extensions
- ⚡ **Async Support** - first-class async/await support with ValueTask
- 🎯 **AOT Compatible** - works with Native AOT and aggressive trimming
- 🏗️ **Hierarchical States** - support for composite states, history modes, and transition inheritance

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

door.CanFire(DoorTrigger.Open);  // true
door.Fire(DoorTrigger.Open);      // door is now Open
door.CurrentState;                // DoorState.Open
```

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

Every state machine automatically gets these methods:

```csharp
// Current state of the machine
TState CurrentState { get; }

// Try to fire a trigger (returns true if successful)
bool TryFire(TTrigger trigger, object? payload = null)

// Fire a trigger (throws if invalid)
void Fire(TTrigger trigger, object? payload = null)

// Check if a trigger can be fired
bool CanFire(TTrigger trigger)

// Get all valid triggers from current state
IReadOnlyList<TTrigger> GetPermittedTriggers()
```

## Advanced Features

### Typed Payloads

Pass strongly-typed data with transitions:

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
var data = new ProcessData { Id = "123", IsValid = true };
processor.Fire(ProcessTrigger.Start, data);
```

### Multiple Payload Types

Different triggers can accept different payload types:

```csharp
[StateMachine(typeof(SystemState), typeof(SystemTrigger))]
[PayloadType(SystemTrigger.Configure, typeof(ConfigData))]
[PayloadType(SystemTrigger.Process, typeof(ProcessData))]
[PayloadType(SystemTrigger.Error, typeof(ErrorData))]
public partial class System
{
    // Each trigger expects its specific payload type
}
```

### Async Support

FastFSM fully supports async operations:

```csharp
[StateMachine(typeof(DownloadState), typeof(DownloadTrigger))]
public partial class FileDownloader
{
    [Transition(DownloadState.Ready, DownloadTrigger.Start, DownloadState.Downloading,
        Action = nameof(StartDownloadAsync))]
    private void Configure() { }
    
    private async ValueTask StartDownloadAsync()
    {
        await DownloadFileAsync();
    }
}

// Usage
await downloader.TryFireAsync(DownloadTrigger.Start);
```

### Cancellation semantics

- **Overload preference** – gdy wywołujesz `TryFireAsync`/`FireAsync`/`CanFireAsync`/`GetPermittedTriggersAsync` z parametrem `CancellationToken`, FastFSM wybiera przeciążenie guardu/akcji/OnEntry/OnExit, którego ostatni parametr to `CancellationToken`.  
  Jeśli takiego przeciążenia nie ma, używana jest wersja bezparametrowa.

- **One implementation is enough** – nie musisz definiować dubla bez tokenu; przeciążenie z `CancellationToken` działa również, gdy wywołujący poda `CancellationToken.None`.

- **No implicit rollback** – stan (`CurrentState`) zmienia się natychmiast po pozytywnym guardzie i *przed* wykonaniem `OnEntry` lub akcji.  
  Jeśli w tych metodach zostanie rzucony `OperationCanceledException`, wyjątek jest przekazywany dalej, a **stan nie zostaje automatycznie cofnięty**.  
  Potrzebujesz transakcyjności? Cofnij stan ręcznie w obsłudze wyjątku lub otocz logikę własną transakcją.

- **Guards in helpers** – `CanFireAsync` i `GetPermittedTriggersAsync` przekazują podany token do wszystkich ewaluowanych guardów.

### Internal Transitions

Execute actions without changing state:

```csharp
[InternalTransition(State.Active, Trigger.Refresh, nameof(RefreshData))]
private void ConfigureInternal() { }

private void RefreshData() => Console.WriteLine("Data refreshed");
```

### Hierarchical State Machines (HSM)

FastFSM supports hierarchical (composite) states with full UML statechart semantics:

```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class GameStateMachine
{
    // Define hierarchy with Parent parameter
    [State(State.Menu, Parent = State.Root, History = HistoryMode.Shallow)]
    void Menu() { }

    [State(State.MainMenu, Parent = State.Menu)]
    [InitialSubstate(State.Menu, State.MainMenu)]  // Initial child of Menu
    void MainMenu() { }

    [State(State.Settings, Parent = State.Menu)]
    void Settings() { }

    // Transitions defined on parent work for all children
    [Transition(State.Menu, Trigger.StartGame, State.Playing)]
    void MenuToGame() { }
    
    // Child can override parent transitions
    [Transition(State.Settings, Trigger.Back, State.MainMenu)]
    void SettingsBack() { }
}

// Usage
var game = new GameStateMachine(State.MainMenu);
game.IsIn(State.Menu);        // true - MainMenu is child of Menu
game.GetActivePath();          // [Root, Menu, MainMenu]
game.Fire(Trigger.StartGame); // Works from any Menu substate
```

**HSM Features:**
- **Composite states** with parent-child relationships
- **Transition inheritance** - children inherit parent transitions
- **History modes** - Shallow/Deep memory of last active substate
- **Structural queries** - `IsIn()`, `GetActivePath()`
- **LCA optimization** - minimal exit/entry sequences

See [HSM Documentation](Generator/HSM_Documentation.md) for complete guide.

### Extensions

Add cross-cutting concerns like logging or metrics:

```csharp
public class LoggingExtension : IStateMachineExtension
{
    public void OnBeforeTransition<T>(T context) where T : IStateMachineContext
        => Console.WriteLine($"Transitioning from {context.FromState}");
        
    public void OnAfterTransition<T>(T context, bool success) where T : IStateMachineContext
        => Console.WriteLine($"Transitioned to {context.ToState}: {success}");
}

// Usage with extensions
var machine = new OrderWorkflow(OrderState.New, new[] { new LoggingExtension() });
```

Below is a drop-in **Performance** section for your `README.md`. It uses your latest BenchmarkDotNet run (5 Aug 2025) and documents the methodology so others can reproduce and scrutinize the results.

---

## Performance

FastFSM focuses on predictable, allocation-free transitions generated at compile time. To make performance claims reproducible, we benchmark with [BenchmarkDotNet](https://benchmarkdotnet.org/) and publish the full methodology and raw reports. BenchmarkDotNet guards against common benchmarking pitfalls and provides statistically sound summaries. ([benchmarkdotnet.org][1])

### Environment (latest run)

* **OS / CPU / JIT:** Windows 11 24H2, x64 RyuJIT (AVX-512)
* **.NET:** .NET 9.0.5
* **BenchmarkDotNet:** 0.15.2
* **Config:** `WarmupCount=3`, `IterationCount=15`, `LaunchCount=1` (Release, no debugger)

### What we measured

We compare common scenarios across libraries:

* **Basic transition** (single state change)
* **Guards + Actions** (guard validated and action executed)
* **Payload** (typed payload passed through transition)
* **CanFire** (capability check)
* **Async action** (action uses `Task.Yield()` to simulate a real async hop)

For very fast paths we execute multiple operations per invocation and scale results back to **ns/op**, a standard approach for nano-benchmarks. ([benchmarkdotnet.org][2], [GitHub][3])

### Results (ns/op; lower is better)

| Scenario                             |       FastFSM |   Stateless | LiquidState | Appccelerate |
| ------------------------------------ | ------------: | ----------: | ----------: | -----------: |
| **Basic transition**                 |   **0.76 ns** |   269.48 ns |    25.14 ns |    244.08 ns |
| **Guards + Actions**                 |   **1.83 ns** |   265.01 ns |           – |    270.21 ns |
| **Payload**                          |   **0.61 ns** |   256.54 ns |    29.70 ns |    255.41 ns |
| **CanFire**                          |  **0.204 ns** |   131.70 ns |           – |            – |
| **Async (action with `Task.Yield`)** | **436.99 ns** | 1,055.09 ns |   482.43 ns |  1,558.96 ns |

**Allocations (B/op; lower is better)**

* Sync scenarios (Basic/Guards/Payload/CanFire): FastFSM **0 B**; Stateless \~**0.6–1.4 KB**; LiquidState up to **136 B**; Appccelerate \~**1.6 KB**.
* Async (`Task.Yield`): FastFSM **\~383 B**; Stateless **\~2.3 KB**; Appccelerate **\~28.9 KB**.

**Interpretation.** In synchronous hot paths FastFSM is *orders of magnitude* faster (hundreds of ×) and allocation-free because transitions compile down to direct code (no runtime reflection or lookup structures). In async scenarios the scheduler hop dominates total cost; FastFSM still leads (≈2–2.5×), but absolute times are largely governed by async machinery. When an async action often completes synchronously, using `ValueTask` can reduce overhead—but it should be adopted when profiling shows a benefit, not by default. ([Microsoft Learn][4], [Microsoft for Developers][5])

### Methodology & reproducibility

* **How we avoid “too fast to measure”:** for micro-operations we batch multiple transitions per iteration via `OperationsPerInvoke` and keep observable state alive to prevent dead-code elimination. ([benchmarkdotnet.org][2])

  * We use `DeadCodeEliminationHelper.KeepAliveWithoutBoxing(...)` from BenchmarkDotNet to ensure results are not optimized away. ([benchmarkdotnet.org][6])
* **Memory:** allocations are captured by BenchmarkDotNet’s memory diagnoser in Release mode. (Avoid Debug builds and attached debuggers when benchmarking.) ([fransbouma.github.io][7])
* **Profiling (optional):** when we need CPU/GC timelines we run a **separate** profiling pass using `EventPipeProfiler(CpuSampling)` on a small subset of tests, which produces compact `.nettrace`/speedscope outputs. We do **not** use ETW-based diagnosers for bulk runs to avoid multi-GB traces. ([benchmarkdotnet.org][8])

**Re-running the benchmarks**

```bash
# In the Benchmark project directory
dotnet run -c Release --framework net9.0
# Results are written to: BenchmarkDotNet.Artifacts/results
```

> If you want CPU/GC timelines for a particular test, run a separate pass with an EventPipe profile and a filter:
>
> ```bash
> dotnet run -c Release --framework net9.0 --filter *FastFsm_Basic* 
> ```
>
> (Enable an EventPipe profile in code for the targeted test only.) ([benchmarkdotnet.org][9])

### Caveats

* Results vary with CPU, OS, JIT, and library versions. BenchmarkDotNet includes confidence intervals, outlier detection, and multimodality warnings to help interpret stability; we publish the full HTML/CSV reports in `BenchmarkDotNet.Artifacts/results`. ([benchmarkdotnet.org][1])
* For async APIs, prefer `Task` as the default; consider `ValueTask` when profiling shows frequent synchronous completion and measurable wins in your workload. ([Microsoft Learn][4])

---

[1]: https://benchmarkdotnet.org/?utm_source=chatgpt.com "BenchmarkDotNet: Home"
[2]: https://benchmarkdotnet.org/articles/configs/diagnosers.html?utm_source=chatgpt.com "Diagnosers - BenchmarkDotNet"
[3]: https://github.com/dotnet/BenchmarkDotNet/issues/1832?utm_source=chatgpt.com "[Proposal] OperationsPerInvoke to be fed by Params #1832 - GitHub"
[4]: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask?view=net-9.0&utm_source=chatgpt.com "ValueTask Struct (System.Threading.Tasks) | Microsoft Learn"
[5]: https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/?utm_source=chatgpt.com "Understanding the Whys, Whats, and Whens of ValueTask - .NET Blog"
[6]: https://benchmarkdotnet.org/api/BenchmarkDotNet.Engines.DeadCodeEliminationHelper.html?utm_source=chatgpt.com "Class DeadCodeEliminationHelper - BenchmarkDotNet"
[7]: https://fransbouma.github.io/BenchmarkDotNet/RulesOfBenchmarking.htm?utm_source=chatgpt.com "Rules of benchmarking - BenchmarkDotNet Documentation"
[8]: https://benchmarkdotnet.org/articles/features/event-pipe-profiler.html?utm_source=chatgpt.com "EventPipeProfiler - BenchmarkDotNet"
[9]: https://benchmarkdotnet.org/articles/samples/IntroEventPipeProfiler.html?utm_source=chatgpt.com "Sample: EventPipeProfiler - BenchmarkDotNet"

- No dictionary overhead or boxing

## Real-World Examples

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

### TCP Connection State Machine

```csharp
[StateMachine(typeof(TcpState), typeof(TcpTrigger))]
public partial class TcpConnection
{
    private Socket? _socket;
    private Timer? _keepAliveTimer;
    
    [State(TcpState.Established,
        OnEntry = nameof(StartKeepAlive),
        OnExit = nameof(StopKeepAlive))]
    private void ConfigureStates() { }
    
    [Transition(TcpState.Closed, TcpTrigger.Open, TcpState.SynSent,
        Action = nameof(SendSyn))]
    [Transition(TcpState.SynSent, TcpTrigger.SynAck, TcpState.Established,
        Action = nameof(SendAck))]
    [Transition(TcpState.Established, TcpTrigger.Data, TcpState.Established,
        Action = nameof(ProcessData))]
    [Transition(TcpState.Established, TcpTrigger.Close, TcpState.FinWait1,
        Action = nameof(SendFin))]
    private void ConfigureTransitions() { }
    
    // Implementation details...
}
```

## Architecture Overview

FastFSM uses a multi-stage compilation approach:

1. **Declaration** - You define states and transitions using attributes
2. **Analysis** - Source generator analyzes your declarations at compile time
3. **Generation** - Optimized implementation is generated as part of your class
4. **Compilation** - Everything compiles to efficient IL code

The generator creates different variants based on features used:
- **Pure** - Just transitions (fastest)
- **Basic** - Adds OnEntry/OnExit
- **WithPayload** - Typed data support
- **WithExtensions** - Plugin support
- **Full** - All features

You automatically get the most efficient variant for your needs.

## Migration Guide

### From Stateless

```csharp
// Stateless
var machine = new StateMachine<State, Trigger>(State.Initial);
machine.Configure(State.Initial)
    .Permit(Trigger.Start, State.Running);

// FastFSM
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MyMachine
{
    [Transition(State.Initial, Trigger.Start, State.Running)]
    private void Configure() { }
}
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## License

FastFSM is licensed under the MIT License. See [LICENSE](LICENSE) for details.