# Dependency injection

Install the DI package:

```bash
dotnet add package FastFsm.Net.DependencyInjection
```

The package defines `FSM_DI_ENABLED` and `FSM_LOGGING_ENABLED` and compiles `FsmServiceCollectionExtensions` into your project.

## Register a state machine

`AddStateMachine` requires **four** type parameters: interface, implementation, state enum, trigger enum.

```csharp
using FastFsm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

public interface IOrderWorkflow { /* generated/sync API surface */ }
// OrderWorkflow : partial class with [StateMachine]

services.AddStateMachine<IOrderWorkflow, OrderWorkflow, OrderState, OrderTrigger>(
    ServiceLifetime.Singleton);
```

The implementation type must be the generated `partial` state machine class.

## Initial state

By default the first enum value is used. Override with:

```csharp
services.ConfigureStateMachineInitialState<OrderState>(
    _ => OrderState.Draft);
```

## Register extensions

```csharp
services.AddStateMachineExtension<AuditExtension>(ServiceLifetime.Singleton);
```

Extensions registered in DI are supplied to machines created through the factory when the extensible variant is generated (`GenerateExtensibleVersion = true`, the default).

## Factory

Registration also exposes `IStateMachineFactory<TInterface, TState, TTrigger>` for explicit creation with a chosen initial state.

## Related types

Defined in `FastFsm/DependencyInjection/`:

- `FsmServiceCollectionExtensions`
- `StateMachineFactory<TInterface, TImplementation, TState, TTrigger>`
- `IInitialStateProvider<TState>`

## Tests

See `FastFsm.DependencyInjection.Tests` for registration and factory coverage.
