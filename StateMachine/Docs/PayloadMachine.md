# FSM.NET Payload Variant - API Documentation

## Overview

The Payload variant of FSM.NET allows state machines to work with strongly-typed data during transitions. This enables passing context-specific information through state changes while maintaining type safety and performance.

## Table of Contents
1. [Basic Payload Configuration](#basic-payload-configuration)
2. [Trigger-Specific Payloads](#trigger-specific-payloads)
3. [Payload in Callbacks](#payload-in-callbacks)
4. [API Reference](#api-reference)
5. [Usage Examples](#usage-examples)
6. [Generated Interfaces](#generated-interfaces)

## Basic Payload Configuration

### Default Payload Type

You can define a default payload type for all transitions in the state machine:

```csharp
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderData))]  // Default payload for all triggers
public partial class OrderStateMachine
{
    // Machine implementation
}
```

With a default payload type:
- All triggers accept `OrderData` as payload
- Guards and Actions can receive typed payload
- OnEntry callbacks can receive payload from transitions

## Trigger-Specific Payloads

### Different Payload per Trigger

For more complex scenarios, you can specify different payload types for different triggers:

```csharp
[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
[PayloadType(ProcessTrigger.Start, typeof(StartupConfig))]
[PayloadType(ProcessTrigger.Process, typeof(ProcessingData))]
[PayloadType(ProcessTrigger.Error, typeof(ErrorInfo))]
public partial class ProcessStateMachine
{
    // Machine implementation
}
```

### Mixed Approach

You can combine default and trigger-specific payloads:

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
[PayloadType(typeof(DefaultData))]  // Default for most triggers
[PayloadType(Trigger.Special, typeof(SpecialData))]  // Override for specific trigger
public partial class MixedPayloadMachine
{
    // Machine implementation
}
```

## Payload in Callbacks

### Guards with Payload

Guards can accept typed payload to make decisions:

```csharp
[Transition(State.A, Trigger.Next, State.B, Guard = nameof(CanProceed))]
private void Configure() { }

// Without payload (always available)
private bool CanProceed() => true;

// With payload (when PayloadType is defined)
private bool CanProceed(OrderData data) => data.Amount > 0;
```

### Actions with Payload

Actions can receive and process payload data:

```csharp
[Transition(State.A, Trigger.Process, State.B, Action = nameof(ProcessOrder))]
private void Configure() { }

// Without payload
private void ProcessOrder() { }

// With payload
private void ProcessOrder(OrderData data) 
{
    // Process the order data
}
```

### OnEntry with Payload

OnEntry callbacks can receive payload from incoming transitions:

```csharp
[State(State.Processing, OnEntry = nameof(StartProcessing))]
private void ConfigureStates() { }

// Without payload (always available)
private void StartProcessing() { }

// With payload (receives payload from transition)
private void StartProcessing(ProcessData data) 
{
    // Initialize with transition data
}
```

### OnExit Callbacks

OnExit callbacks **cannot** receive payload (they don't have access to trigger context):

```csharp
[State(State.Active, OnExit = nameof(Cleanup))]
private void ConfigureStates() { }

// OnExit is always parameterless
private void Cleanup() 
{
    // Clean up state
}
```

## API Reference

### PayloadTypeAttribute

```csharp
// Default payload for all triggers
[PayloadType(typeof(TPayload))]

// Trigger-specific payload
[PayloadType(TriggerEnum.Value, typeof(TPayload))]
```

**Placement:**
- Class level: Defines default payload
- Method level: Can override for specific methods (advanced)

### Method Signatures

#### Guards
```csharp
// Option 1: Parameterless (always valid)
private bool GuardMethod() => true;

// Option 2: With typed payload (when payload type is defined)
private bool GuardMethod(TPayload payload) => payload.IsValid;
```

#### Actions
```csharp
// Option 1: Parameterless (always valid)
private void ActionMethod() { }

// Option 2: With typed payload (when payload type is defined)
private void ActionMethod(TPayload payload) { }
```

#### OnEntry
```csharp
// Option 1: Parameterless (always valid)
private void OnEntryMethod() { }

// Option 2: With typed payload (when default payload is defined)
private void OnEntryMethod(TPayload payload) { }
```

#### OnExit
```csharp
// Always parameterless
private void OnExitMethod() { }
```

### Internal Transitions with Payload

Internal transitions support the same payload patterns:

```csharp
[InternalTransition(State.Active, Trigger.Update, nameof(UpdateState))]
private void ConfigureInternal() { }

// Action can receive payload
private void UpdateState(UpdateData data) { }
```

## Generated Interfaces

### Single Payload Type

When only a default payload is defined:

```csharp
public interface IOrderMachine : IStateMachineWithPayload<OrderState, OrderTrigger, OrderData>
{
}
```

Generated methods:
```csharp
// Typed payload methods
bool TryFire(OrderTrigger trigger, OrderData payload);
void Fire(OrderTrigger trigger, OrderData payload);
bool CanFire(OrderTrigger trigger, OrderData payload);

// Base interface methods (still available)
bool TryFire(OrderTrigger trigger, object? payload = null);
```

### Multiple Payload Types

When trigger-specific payloads are defined:

```csharp
public interface IProcessMachine : IStateMachineWithMultiPayload<ProcessState, ProcessTrigger>
{
}
```

Generated methods:
```csharp
// Generic payload methods with runtime type checking
bool TryFire<TPayload>(ProcessTrigger trigger, TPayload payload);
void Fire<TPayload>(ProcessTrigger trigger, TPayload payload);
bool CanFire<TPayload>(ProcessTrigger trigger, TPayload payload);

// Base interface methods
bool TryFire(ProcessTrigger trigger, object? payload = null);
```

## Usage Examples

### Example 1: Order Processing Machine

```csharp
public class OrderData
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Customer { get; set; }
}

[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
[PayloadType(typeof(OrderData))]
public partial class OrderMachine
{
    [Transition(OrderState.New, OrderTrigger.Validate, OrderState.Validated,
        Guard = nameof(IsValidOrder), Action = nameof(LogValidation))]
    [Transition(OrderState.Validated, OrderTrigger.Process, OrderState.Processing)]
    private void Configure() { }
    
    [State(OrderState.Processing, OnEntry = nameof(StartProcessing))]
    private void ConfigureStates() { }
    
    private bool IsValidOrder(OrderData order) => order.Amount > 0;
    
    private void LogValidation(OrderData order) 
    {
        Console.WriteLine($"Order {order.OrderId} validated");
    }
    
    private void StartProcessing(OrderData order)
    {
        Console.WriteLine($"Processing order {order.OrderId}");
    }
}

// Usage
var machine = new OrderMachine(OrderState.New);
var order = new OrderData { OrderId = 123, Amount = 100 };

if (machine.TryFire(OrderTrigger.Validate, order))
{
    machine.Fire(OrderTrigger.Process, order);
}
```

### Example 2: Multi-Payload Machine

```csharp
[StateMachine(typeof(SystemState), typeof(SystemTrigger))]
[PayloadType(SystemTrigger.Configure, typeof(ConfigData))]
[PayloadType(SystemTrigger.Process, typeof(ProcessData))]
[PayloadType(SystemTrigger.Error, typeof(ErrorData))]
public partial class SystemMachine
{
    [Transition(SystemState.Idle, SystemTrigger.Configure, SystemState.Configured,
        Action = nameof(ApplyConfig))]
    [Transition(SystemState.Configured, SystemTrigger.Process, SystemState.Running,
        Guard = nameof(CanProcess), Action = nameof(StartProcessing))]
    [Transition(SystemState.Running, SystemTrigger.Error, SystemState.Failed)]
    private void Configure() { }
    
    [State(SystemState.Failed, OnEntry = nameof(HandleError))]
    private void ConfigureStates() { }
    
    private void ApplyConfig(ConfigData config) { }
    private bool CanProcess(ProcessData data) => data.IsValid;
    private void StartProcessing(ProcessData data) { }
    private void HandleError() { } // No payload - Error trigger payload not accessible in OnEntry
}

// Usage
var machine = new SystemMachine(SystemState.Idle);

// Each trigger expects its specific payload type
machine.Fire(SystemTrigger.Configure, new ConfigData());
machine.Fire(SystemTrigger.Process, new ProcessData());
machine.Fire(SystemTrigger.Error, new ErrorData());
```

### Example 3: Payload Overloading

The generator creates overloads to support both parameterless and payload variants:

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
[PayloadType(typeof(MyData))]
public partial class FlexibleMachine
{
    [Transition(State.A, Trigger.Go, State.B, 
        Guard = nameof(CanGo), Action = nameof(DoTransition))]
    private void Configure() { }
    
    // Guard - both signatures can exist
    private bool CanGo() => true; // Called when no payload
    private bool CanGo(MyData data) => data.IsReady; // Called with payload
    
    // Action - both signatures can exist
    private void DoTransition() { } // Called when no payload
    private void DoTransition(MyData data) { } // Called with payload
}

// Usage - both work
machine.Fire(Trigger.Go); // Calls parameterless overloads
machine.Fire(Trigger.Go, new MyData()); // Calls payload overloads
```

## Important Notes

1. **Type Safety**: The generator enforces type safety at compile time. Wrong payload types will cause compilation errors.

2. **Overload Resolution**: When both parameterless and payload versions exist, the generator picks the appropriate one based on whether payload is provided.

3. **OnExit Limitation**: OnExit callbacks cannot receive payload as they don't have access to the trigger context.

4. **Initial State**: OnEntry for initial state (called in constructor) always uses the parameterless version.

5. **Internal Transitions**: Support the same payload mechanisms as regular transitions.

6. **Performance**: Payload support is implemented with minimal overhead using aggressive inlining and compile-time type resolution.

