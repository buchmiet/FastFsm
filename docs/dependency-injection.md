# Dependency injection

Install the DI package:

```bash
dotnet add package FastFsm.Net.DependencyInjection
```

The package defines `FSM_DI_ENABLED` and `FSM_LOGGING_ENABLED` and compiles `FsmServiceCollectionExtensions` into the consuming project.

## Register a state machine

`AddStateMachine` requires four type parameters: interface, implementation, state enum, and trigger enum.

```csharp
using FastFsm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddStateMachine<IOrderWorkflow, OrderWorkflow, OrderState, OrderTrigger>(
    ServiceLifetime.Singleton);
```

The implementation type is the generated `partial` state-machine class.

## Initial state

By default, the registration uses the first enum value. Configure another initial state with:

```csharp
services.ConfigureStateMachineInitialState<OrderState>(
    _ => OrderState.Draft);
```

## Register extensions

```csharp
services.AddStateMachineExtension<AuditExtension>(ServiceLifetime.Singleton);
```

Extensions registered in DI are supplied to machines generated with extension support. In the current 0.9 codebase, set `GenerateExtensibleVersion = true` explicitly on those machines; see [extensions.md](extensions.md).

## Factory

Registration also exposes `IStateMachineFactory<TInterface, TState, TTrigger>` for explicit creation with a selected initial state.

## Related types

Defined in `FastFsm/DependencyInjection/`:

- `FsmServiceCollectionExtensions`
- `StateMachineFactory<TInterface, TImplementation, TState, TTrigger>`
- `IInitialStateProvider<TState>`

## Tests

See `FastFsm.DependencyInjection.Tests` for registration and factory coverage.
