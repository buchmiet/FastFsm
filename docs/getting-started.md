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

## Minimal Attribute API machine

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

See [attribute-api.md](attribute-api.md) for the Attribute API.

## Minimal Fluent API machine

```csharp
using Abstractions.Attributes;
using Abstractions.Fluent;

public enum LightState { Off, On }
public enum LightTrigger { Toggle }

[StateMachine(typeof(LightState), typeof(LightTrigger))]
public partial class LightSwitch
{
    private void Configure() => FSM
        .State(LightState.Off)
            .On(LightTrigger.Toggle).GoTo(LightState.On)
        .State(LightState.On)
            .On(LightTrigger.Toggle).GoTo(LightState.Off);
}

var light = new LightSwitch(LightState.Off);
light.Start();
light.Fire(LightTrigger.Toggle);
```

See [fluent-api.md](fluent-api.md) for the Fluent API.

## Lifecycle: `Start`, `Fire`, `TryFire`

| Method | Behavior |
|--------|----------|
| `Start()` | Activates the machine and runs `OnEntry` for the initial state. Must be called before transition operations. |
| `CanFire(trigger)` | Evaluates whether the trigger can be taken from the current state, including applicable guards. |
| `TryFire(trigger[, payload])` | Attempts a transition; returns `false` if no transition applies or a guard rejects it. |
| `Fire(trigger[, payload])` | Attempts the same transition and throws `InvalidOperationException` if it cannot be taken. |

Use `TryFire` when the caller handles an unavailable transition as a boolean result. Use `Fire` when the caller requires an exception for an unavailable transition.

Asynchronous machines expose `StartAsync`, `TryFireAsync`, and `FireAsync`. Synchronous `Fire` and `TryFire` calls on asynchronous machines throw `SyncCallOnAsyncMachineException`.

## `GenerateExtensibleVersion`

`[StateMachine(..., GenerateExtensibleVersion = …)]` defaults to `true`. When enabled, the generated machine accepts `IStateMachineExtension` instances. See [extensions.md](extensions.md).

Set `GenerateExtensibleVersion = false` to generate the non-extensible variant.

## Optional logging

Install `FastFsm.Net.Logging` to enable logging code generation. See [logging.md](logging.md) for the package configuration and generated constructor behavior.

## Optional dependency injection

Install `FastFsm.Net.DependencyInjection` and register machines with `AddStateMachine<TInterface, TImplementation, TState, TTrigger>`. The package configures the required build properties. See [dependency-injection.md](dependency-injection.md).

## Building this repository

`Directory.Build.props` sets `UsePackages=false` by default, so repository projects use project references and direct analyzer wiring instead of resolving FastFsm from NuGet.

```bash
dotnet build Generator/Generator.csproj
dotnet test FastFsm.Tests/FastFsm.Tests.csproj
```

## Next steps

- [Fluent API](fluent-api.md)
- [Attribute API](attribute-api.md)
- [Hierarchical state machines](hsm.md)
- [Diagnostics](diagnostics.md)
