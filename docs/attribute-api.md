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
| `[StateMachine(stateType, triggerType, …)]` | Marks the class; required |
| `[Transition(from, trigger, to, Guard = …, Action = …)]` | External transition |
| `[State(state, OnEntry = …, OnExit = …, Parent = …, IsInitial = …, History = …)]` | State metadata / HSM |
| `[InternalTransition(stateOrAncestor, trigger, Action = …)]` | Internal transition |
| `[PayloadType(trigger, payloadType)]` | Typed payload for a trigger |

## `StateMachineAttribute` options

| Property | Default | Description |
|----------|---------|-------------|
| `GenerateExtensibleVersion` | **`true`** | Emit extensible variant with `IStateMachineExtension` support |
| `GenerateStructuralApi` | `false` | Extra introspection helpers (`HasTransition`, etc.) |
| `EnableHierarchy` | `false` | HSM features (auto-enabled when HSM attributes are present) |
| `DefaultPayloadType` | unset | Default payload type for payload-enabled machines |
| `ContinueOnCapturedContext` | `false` | Async: capture synchronization context |

When `GenerateExtensibleVersion` is `true` (the default), constructors accept optional `IEnumerable<IStateMachineExtension>` and an optional `ILogger<T>` when logging is enabled.

Set `GenerateExtensibleVersion = false` only when you explicitly want the non-extensible generated type.

## Guards and actions

Reference methods by name:

```csharp
[Transition(S.Idle, T.Start, S.Running, Guard = nameof(CanStart), Action = nameof(OnStart))]
private void Transitions() { }

private bool CanStart() => _enabled;
private void OnStart() { _enabled = false; }
```

Payload machines accept guard/action overloads that take the payload type. See [payloads.md](payloads.md).

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

## Fluent vs attributes

Both APIs compile to the same generator backend. Choose based on team preference:

- **Fluent** — readable linear configuration, strong DSL validation ([fluent-api.md](fluent-api.md))
- **Attributes** — familiar declarative style, easy to scan in code review

You can mix projects: some machines Fluent, some attribute-based.

## Diagnostics

Invalid signatures, duplicate transitions, and HSM misconfiguration are reported at compile time. See [diagnostics.md](diagnostics.md).
