# Observability

`FastFsm.Sharp.Observability` is the first official client of Extension Contract v2. It ships as a normal `lib/net10.0` assembly and observes transitions through `IStateMachineExtension<TState,TTrigger>` — no generator changes required.

## Install

```bash
dotnet add package FastFsm.Sharp.Observability
```

Your state machine must be generated with `GenerateExtensibleVersion = true`.

## Minimal setup

```csharp
using FastFsm.Observability;

var extension = new ObservabilityExtension<MyState, MyTrigger>(
    new FastFsmObservabilityOptions
    {
        Tracing = true,
        Metrics = true,
        EventStream = true
    },
    eventSink: myRecorder);

var machine = new MyMachine(initialState, extensions: [extension]);
machine.Start();
machine.TryFire(MyTrigger.Next);
```

OpenTelemetry collectors listen to the shared `ActivitySource` (`FastFsm`) and `Meter` (`FastFsm`) exposed by `ObservabilityTelemetry`.

## Configuration

`FastFsmObservabilityOptions` toggles each surface independently:

| Option | Default | Effect |
|---|---|---|
| `Tracing` | `false` | One `Activity` per transition attempt |
| `Metrics` | `false` | `System.Diagnostics.Metrics` counters and duration histogram |
| `EventStream` | `false` | Machine-agnostic `ObservabilityEvent` callbacks |
| `Logging` | `false` | Optional `ILogger` sink (separate from `FastFsm.Sharp.Logging`) |
| `IncludeStateTriggerMetricTags` | `false` | Adds `source_state` / `trigger` metric dimensions (cardinality cost) |
| `IncludeGuardEvents` | `false` | Subscribes to guard hooks |
| `IncludeStateEvents` | `false` | Subscribes to HSM state lifecycle hooks |
| `IncludeCallbackEvents` | `false` | Subscribes to callback hooks |
| `CapturePayload` | `false` | Exports payload only with `PayloadFormatter` |

`ExtensionHooks` is computed from these flags. Metrics-only mode requests `Transitions` only; guard/state/callback hooks are not enabled unless you opt in.

## Tracing

- **One attempt → one span** named `fsm.transition`
- Starts in `OnAttemptStarting`, ends in `OnAttemptCompleted`
- Correlated by diagnostic tags `fastfsm.instance_id` and `fastfsm.attempt_id` (trace only — not metric dimensions)
- Semantic tags use v2 contract fields: `source_state`, `handled_at_state`, `declared_target`, `resolved_target`, `final_state`, `transition.kind`, `outcome`
- `Faulted` → `ActivityStatusCode.Error` with exception event
- `Canceled` → OK status with `fastfsm.canceled=true` (not a fault)
- `GuardRejected`, `UnhandledTrigger`, `InvalidPayload` → OK status (not exception errors)
- Guard/state/callback phases can appear as **Activity events** when the corresponding include flags are set

## Metrics

Instruments (meter name `FastFsm`):

| Instrument | Name |
|---|---|
| Counter | `fastfsm.transition.attempts` |
| Counter | `fastfsm.transition.completed` (tag: `outcome`, optional `transition_kind`, optional `source_state`/`trigger`) |
| Histogram | `fastfsm.transition.duration` (seconds) |
| Counter | `fastfsm.transition.failures` |
| Counter | `fastfsm.transition.cancellations` |
| Counter | `fastfsm.transition.guard_rejected` |
| Counter | `fastfsm.transition.unhandled` |

**Never used as metric tags:** `InstanceId`, `AttemptId`, payload, exception messages, arbitrary callback names.

Enable `IncludeStateTriggerMetricTags` only when you accept higher cardinality.

## Event stream (no OpenTelemetry required)

Implement `IObservabilityEventSink` to receive `ObservabilityEvent` values. States, triggers, and outcomes are strings at this boundary — the typed core contract stays inside the extension hot path.

Use for test recorders, runtime inspectors, or custom exporters.

## ILogger

When `Logging = true`, the extension writes structured messages via `LoggerMessage.Define` helpers. This is **optional** and **independent** from `FastFsm.Sharp.Logging` generator integration. Both can coexist on the same machine.

## Dependency injection

```csharp
services.AddFastFsmObservability<MyState, MyTrigger>(options =>
{
    options.Tracing = true;
    options.Metrics = true;
});
services.AddSingleton<IObservabilityEventSink, MyRecorder>();
services.AddStateMachine<IMyMachine, MyMachine, MyState, MyTrigger>();
```

`AddFastFsmObservability` registers `ObservabilityExtension<TState,TTrigger>` using the same open-generic extension pattern as core DI.

## Payload privacy

Payload is **ignored by default**. Observability does not call `ToString()` on payloads. To export payload data you must set both `CapturePayload = true` and a user-supplied `PayloadFormatter`.

## HSM fields

Observability reads v2 semantics directly from the extension contract:

| Field | Meaning |
|---|---|
| `SourceState` | active leaf when the attempt started |
| `HandledAtState` | state that owns the matched transition |
| `DeclaredTarget` | configured target; `null` for internal transitions |
| `ResolvedTarget` | leaf actually entered; `null` when resolution never ran |
| `FinalState` | machine position when the attempt completed |
| `TransitionKind` | `Internal` vs `External` from the contract, not from `From == To` |

Internal ≠ external self. An ancestor-owned transition may have `SourceState != HandledAtState`. Composite and history targets expose distinct declared and resolved values.

## Benchmarks

See `src/Benchmark/ObservabilityBenchmarks.cs` for flat and HSM scenarios: baseline, all-disabled registration, metrics-only, tracing without listener, tracing with listener, and combined tracing+metrics.

## Related docs

- [extensions.md](extensions.md) — Extension Contract v2 hook surface
- [dependency-injection.md](dependency-injection.md) — core DI registration
- [logging.md](logging.md) — generator-integrated logging (separate track)
