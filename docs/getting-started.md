# Getting started

FastFsm targets **.NET 10**. The repository pins the SDK in `global.json` (currently `10.0.400`).

## Prerequisites

```bash
dotnet --version   # 10.0.x
```

## Create a project

```bash
dotnet new console -n MyFsm -f net10.0
cd MyFsm
dotnet add package FastFsm.Net
```

Optional packages:

```bash
dotnet add package FastFsm.Net.Logging
dotnet add package FastFsm.Net.DependencyInjection
```

## Minimal attribute-based machine

```csharp
using Abstractions.Attributes;

public enum LightState { Off, On }
public enum LightTrigger { Toggle }

[StateMachine(typeof(LightState), typeof(LightTrigger))]
public partial class LightSwitch
{
    [Transition(LightState.Off, LightTrigger.Toggle, LightState.On)]
    [Transition(LightState.On, LightTrigger.Toggle, LightState.Off)]
    private void ConfigureTransitions() { }

    [State(LightState.On, OnEntry = nameof(OnLightOn))]
    private void ConfigureOnState() { }

    private void OnLightOn() => Console.WriteLine("Light is ON");
}

var light = new LightSwitch(LightState.Off);
light.Start();
light.Fire(LightTrigger.Toggle);
```

## Minimal Fluent machine

See the [Fluent API guide](fluent-api.md) or the [README](../README.md) quick example.

## Lifecycle: `Start`, `Fire`, `TryFire`

| Method | Behavior |
|--------|----------|
| `Start()` | Activates the machine and runs `OnEntry` for the initial state. Must be called before transitions. |
| `CanFire(trigger)` | Returns whether a transition is defined for the current state and trigger (guards may still fail at fire time). |
| `TryFire(trigger[, payload])` | Attempts a transition; returns `false` if no transition applies or a guard fails. |
| `Fire(trigger[, payload])` | Same as `TryFire`, but throws `InvalidOperationException` when the transition cannot be taken. |

**Unhandled triggers are not silently ignored.** Use `TryFire` when failure is expected; use `Fire` when an invalid trigger should fail fast.

Async machines expose `StartAsync`, `TryFireAsync`, and `FireAsync` with the same semantics (`FireAsync` throws on failure). Sync `Fire`/`TryFire` on async machines throw `SyncCallOnAsyncMachineException` — use the async API instead.

## `GenerateExtensibleVersion` default

`[StateMachine(..., GenerateExtensibleVersion = …)]` defaults to **`true`** in `StateMachineAttribute`. When enabled, the generator emits an extensible variant that accepts `IStateMachineExtension` instances (see [extensions.md](extensions.md)). Set it to `false` if you do not need extension hooks and want the leanest generated surface.

## Optional logging

Install `FastFsm.Net.Logging` and pass an `ILogger<TMachine>` to the machine constructor when logging codegen is enabled (`FsmGenerateLogging`). See [logging.md](logging.md).

## Optional dependency injection

Install `FastFsm.Net.DependencyInjection`, define `FSM_DI_ENABLED` (the package props do this automatically), and register machines via `AddStateMachine<TInterface, TImplementation, TState, TTrigger>`. See [dependency-injection.md](dependency-injection.md).

## Building this repository

Contributors typically build with **project references**, not pre-published NuGet packages:

- `Directory.Build.props` sets `UsePackages=false` by default.
- Test projects reference `FastFsm` and `Generator` analyzers directly.

```bash
dotnet build Generator/Generator.csproj
dotnet test FastFsm.Tests/FastFsm.Tests.csproj
```

## Next steps

- [Fluent API](fluent-api.md)
- [Attribute API](attribute-api.md)
- [Hierarchical state machines](hsm.md)
- [Diagnostics](diagnostics.md)
