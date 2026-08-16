# Attribute API

Declare a `partial` class with `[StateMachine(typeof(TState), typeof(TTrigger))]` and configure transitions with attributes on private methods.

```csharp
using Abstractions.Attributes;

public enum Traffic { Red, Green }
public enum Signal { Timer, Manual }

[StateMachine(typeof(Traffic), typeof(Signal))]
public partial class TrafficLight
{
    [Transition(Traffic.Red, Signal.Timer, Traffic.Green)]
    [Transition(Traffic.Green, Signal.Timer, Traffic.Red)]
    private void ConfigureTransitions() { }

    [State(Traffic.Green, OnEntry = nameof(OnGreen))]
    private void ConfigureGreen() { }

    private void OnGreen() => Console.WriteLine("Go");
}
```

## Core attributes

| Attribute | Purpose |
|-----------|---------|
| `[StateMachine(stateType, triggerType, …)]` | Declares the state-machine class |
| `[Transition(from, trigger, to, Guard = …, Action = …)]` | Declares an external transition |
| `[State(state, OnEntry = …, OnExit = …, Parent = …, IsInitial = …, History = …)]` | Declares state metadata and HSM relationships |
| `[InternalTransition(stateOrAncestor, trigger, Action = …)]` | Declares an internal transition |
| `[PayloadType(trigger, payloadType)]` | Associates a payload type with a trigger |

## `StateMachineAttribute` options

| Property | Default | Description |
|----------|---------|-------------|
| `GenerateExtensibleVersion` | `true` | Generates the variant with `IStateMachineExtension` support |
| `GenerateStructuralApi` | `false` | Generates structural query methods such as `HasTransition` |
| `EnableHierarchy` | `false` | Enables HSM support; HSM metadata can also cause hierarchy support to be enabled by the generator |
| `DefaultPayloadType` | unset | Sets the default payload type for payload-enabled machines |
| `ContinueOnCapturedContext` | `false` | Controls synchronization-context capture on asynchronous paths |

When `GenerateExtensibleVersion` is `true`, generated constructors accept optional `IEnumerable<IStateMachineExtension>` parameters. Logging-enabled variants can also accept `ILogger<T>` where applicable.

Set `GenerateExtensibleVersion = false` to generate the non-extensible variant.

## Guards and actions

Reference callback methods by name:

```csharp
[Transition(S.Idle, T.Start, S.Running, Guard = nameof(CanStart), Action = nameof(OnStart))]
private void Transitions() { }

private bool CanStart() => _enabled;
private void OnStart() { _enabled = false; }
```

Payload-enabled machines support callback signatures that accept the configured payload type. See [payloads.md](payloads.md).

## HSM with attributes

```csharp
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
public partial class HsmMachine
{
    [State(HState.A, History = HistoryMode.Shallow)]
    [State(HState.A1, Parent = HState.A, IsInitial = true)]
    [State(HState.A2, Parent = HState.A)]
    private void DefineStates() { }

    [InternalTransition(HState.A, HTrigger.Refresh, nameof(OnRefresh))]
    private void InternalDefs() { }

    [Transition(HState.A1, HTrigger.Next, HState.A2, Guard = nameof(Always))]
    private void ExternalDefs() { }

    private void OnRefresh() { }
    private bool Always() => true;
}
```

See [hsm.md](hsm.md) for hierarchy semantics.

## Relation to the Fluent API

Both configuration APIs are processed by the same generator backend.

- The **Attribute API** stores machine configuration in attributes attached to members.
- The **Fluent API** stores machine configuration in a `Configure()` method using the `FSM` builder.

A project can contain machines configured with either API. See [fluent-api.md](fluent-api.md) for the Fluent form.

## Diagnostics

Invalid callback signatures, duplicate transitions, and HSM configuration errors are reported during compilation. See [diagnostics.md](diagnostics.md).
