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

### Internal Transitions

Execute actions without changing state:

```csharp
[InternalTransition(State.Active, Trigger.Refresh, nameof(RefreshData))]
private void ConfigureInternal() { }

private void RefreshData() => Console.WriteLine("Data refreshed");
```

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

## Performance

FastFSM achieves exceptional performance through compile-time code generation:

| Operation | FastFSM | Stateless | Improvement |
|-----------|---------|-----------|-------------|
| Basic Transition | 0.6ns | 247ns | **393x faster** |
| With Guards | 1.3ns | 255ns | **193x faster** |
| With Payload | 1.9ns | 278ns | **149x faster** |
| Can Fire Check | 0.06ns | 136ns | **2,268x faster** |

**Memory Usage:**
- Zero heap allocations during transitions
- ~40 bytes per state machine instance
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