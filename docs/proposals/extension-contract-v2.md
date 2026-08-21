# Extension contract v2

> **Status:** accepted design, not yet implemented. This document is not part of the current FastFsm API contract; it defines the contract that replaces it.
>
> **Revision 3.** See *Revision history* below.
>
> **Scope:** everything required before `IStateMachineExtension` can be treated as a public interop surface, and before `FastFsm.Sharp.Observability` can be built as an ordinary client of FastFsm rather than another special path in the generator.

## Position

FastFsm 0.9.1 ships an extension mechanism that is structurally sound — opt-in generation, deterministic hook order, exception isolation, sync and async. Its *data contract* is not sound.

The decisive finding is not that the hook surface is incomplete. It is that **the data delivered to extensions is untrue**: a trigger handled on an ancestor is reported as a state change that never occurred, a transition into a composite state reports the composite rather than the leaf actually entered, and a failed attempt reports a machine position the machine is not in.

Adding hooks on top of that model would multiply the problem. The priority is therefore:

> first guarantee that the hooks tell the truth, then widen them.

Accordingly: **`FastFsm.Sharp.Observability` is not started until Extension Contract v2 lands.**

## Revision history

### Changes in revision 3

Three residual places where the model still fabricated or misplaced information.

| | rev 2 | rev 3 |
|---|---|---|
| internal transition target | `TState DeclaredTarget`, necessarily filled | `TState? DeclaredTarget`, null for internal (**DEC-17**) |
| matched vs selected | `OnTransitionSelected` emitted *after* the guard, while the result claimed a selection for `GuardRejected` | `OnTransitionMatched` / `MatchedTransition`, emitted *before* the guard (**DEC-18**) |
| guard and callback faults | assumed producible in PR 2 | blocked by a blanket `catch` in the extensions path; recorded as **D13** and added to PR 2 scope |

### Changes in revision 2

Revision 1 modelled an attempt as a single `TransitionContext` carrying `HandledAtState`, `DeclaredTarget` and `Kind`, with `Kind = None` where no transition matched. That was internally inconsistent: `TState` has no absent value, and `default(TState)` is an ordinary state because enum member zero is a real state.

| | rev 1 | rev 2 |
|---|---|---|
| attempt model | one `TransitionContext` with transition fields | `TransitionAttemptContext` + optional `TransitionInfo` (**DEC-12**) |
| failure position | `ResolvedTarget == SourceState when not Succeeded` | `FinalState` distinct from nullable `ResolvedTarget` (**DEC-13**) |
| snapshot scope | per dispatch | per attempt, array and mask published as one object (**DEC-14**) |
| state vs callback | `OnStateExiting(ctx, state, callbackName)` | state lifecycle separate from callback lifecycle (**DEC-15**) |
| machine stop | `OnMachineStopped` in PR 3 | out of scope for 0.10 (**DEC-16**) |
| PR 1 / PR 2 split | PR 2 introduced result types | PR 1 ships final shapes, PR 2 adds semantics only |

Revision 1's `TransitionKind.None` is removed: kind is a property of a transition, and where there is no transition there is no `TransitionInfo`.

## Current behaviour the contract must state

Three facts about 0.9.1 that the contract must describe accurately rather than assume away. All were checked against the generator.

**Single-candidate selection.** FastFsm does not try a second transition when the first one's guard returns `false`. Both emission paths select exactly one candidate per (state, trigger) at compile time and `break` — `StateMachineCodeGenerator.cs:458-463` and `UnifiedStateMachineGenerator.cs:1618-1622`; the HSM extensions path takes `.OrderByDescending(t => t.Priority).First()` at `:1562`. `Priority` orders candidates for that compile-time selection; it does not produce runtime fallthrough.

Therefore `TransitionOutcome.GuardRejected` means **the guard of the single matched transition returned false**, not "every candidate was rejected". The contract must say so, or Observability will export a meaning FastFsm does not implement.

Whether guard fallthrough *should* exist is a core-semantics question — UML prescribes it — and is out of scope here. It is called out because the rev 2 model makes adding it later non-breaking for extensions: candidates are already carried per guard hook rather than folded into the attempt.

**The extensions variant swallows exceptions the plain variant propagates.** `WriteTransitionLogicSyncWithExtensions` wraps the whole transition body — guard call included — in `try { … } catch { RunAfterTransition(false); return false; }`, with an unfiltered `catch`. This is visible in emitted code. The non-extensions variant has no such wrapper, and guard exceptions propagate unless `FASTFSM_SAFE_GUARDS` is defined, which nothing in the repository defines by default.

Three consequences, recorded as **D13**:

- a guard that throws is indistinguishable from a guard that returns `false`;
- an `OperationCanceledException` from a guard is reported as rejection, not cancellation — `GuardGenerationHelper` is invoked with `treatCancellationAsFailure: false`, and `FASTFSM_SAFE_GUARDS` catches `OperationCanceledException` explicitly and returns `false`;
- adding extensions to a machine changes its exception semantics, which contradicts DEC-3's requirement that extensions cannot alter the outcome of a transition.

`TransitionOutcome.Faulted`, `TransitionOutcome.Canceled` and `TransitionStage` are therefore **not producible without generator work**: the information is destroyed at the blanket `catch` before any hook could observe it. This is scope for PR 2, and it is the one place where PR 2 must touch emission rather than only wiring.

**State changes before entry callbacks run.** For an external transition the generator emits exit, action, **state change**, then entry (`UnifiedStateMachineGenerator.cs`, "State change BEFORE OnEntry"). If `OnEntry` throws or is cancelled, `_currentState` has already been assigned and the machine returns `false`:

```
A → B
_currentState = B
OnEntryB() throws
TryFire() == false
CurrentState == B
```

This is why rev 1's `ResolvedTarget == SourceState when not Succeeded` was itself a lie, and why `FinalState` exists.

## Decisions taken

Settled; not revisited during implementation. Each records the alternative it forecloses.

### DEC-1 — The primary contract is generic in `TState` and `TTrigger`

```csharp
public interface IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
```

The current design uses generic *methods* constrained to `IStateMachineContext`, forcing enum values through `object`. This cannot be changed after publication without breaking every extension ever written. It is changed now, while the package has no dependent users.

### DEC-2 — No untyped extension interface in core

A machine-agnostic extension is an **open generic type** registered as such in DI:

```csharp
public sealed class ObservabilityExtension<TState, TTrigger>
    : IStateMachineExtension<TState, TTrigger>
```

Where an inspector aggregates events across many machine types, conversion to a machine-agnostic event model happens **in the sink, not in core**:

```
typed FastFsm hot path
      ↓
ObservabilityExtension<TState, TTrigger>
      ↓
machine-agnostic ObservabilityEvent
      ↓
OTel / logger / inspector / recorder
```

Parallel typed and untyped APIs would preserve the boxed path v2 exists to remove and double the surface that has to stay correct.

### DEC-3 — Extension dispatch is synchronous and inert

No `IStateMachineExtensionAsync`. Normatively:

> Extension callbacks are synchronous, short and non-blocking. They do not participate in cancellation and cannot alter the outcome of a transition.

`CancellationToken` is not exposed: transition cancellation belongs to the machine, and observability must not influence it. `TransitionOutcome.Canceled` conveys the fact without handing over the control.

### DEC-4 — `Guid InstanceId`, not `string`

Machine identity is a `Guid` **generated once** per machine instance. `Guid.NewGuid()` is a value type and is not a managed allocation; the allocation being avoided is the `ToString()` that the current contract performs unconditionally per attempt. A `Guid` is formatted only if and when a tracer actually needs the string, which for sampled tracing is a small fraction of attempts.

A caller-supplied *correlation* identifier — order id, tenant id, saga id — is not machine identity and does not belong on the context. An extension that needs one holds it and tags its own spans.

### DEC-5 — `TransitionKind` distinguishes external from internal; self-transition is not a third kind

```csharp
public enum TransitionKind { External, Internal }
```

A self-transition is `External` with `SourceState == ResolvedTarget` — which is exactly what distinguishes it from a true internal transition, which runs no exit or entry callbacks. A `Self` kind would invite consumers to treat the distinction as a property of the transition rather than of its effects.

Kind is emitted from `TransitionModel.IsInternal` at compile time. The runtime `From == To` heuristic disappears, and with it the `IStateSnapshot` boxing it required.

### DEC-6 — Declared, resolved and final position are three different facts

```
DeclaredTarget  what the FSM definition names as the target   (null for internal)
ResolvedTarget  where composite / history resolution landed   (null for internal, and where resolution never ran)
FinalState      where the machine actually is when the attempt ends   (always present)
```

Fixing the target must not be done by discarding the declared one; all three are diagnostically distinct, and the third is the only one that is always true. See DEC-17 for why the first two are nullable.

### DEC-7 — Core exposes a monotonic start timestamp, not a computed duration

The attempt context carries `long StartTimestamp` from `Stopwatch.GetTimestamp()`. Extensions needing duration call `Stopwatch.GetElapsedTime(attempt.StartTimestamp)` themselves. One monotonic read, zero allocations, no `DateTime` misused as a stopwatch, no histogram policy imposed by core.

The timestamp is captured **only after the extension snapshot shows that attempt hooks are actually wanted**. Taking it unconditionally would violate the requirement that a machine with no registered extensions costs one volatile read and one branch.

`DateTime Timestamp` is removed from the contract. A logging provider timestamps its own entries.

### DEC-8 — Extension set and hook mask are one atomically published object

Superseded in scope by DEC-14, retained for the storage shape: a copy-on-write array read without a lock, mutated by copying under a lock.

### DEC-9 — The zero-allocation requirements are contract preconditions

Once Observability is the first official extension, the extension path stops being an exotic slow path. Contract v2 must satisfy, as acceptance criteria: no per-attempt `Guid` string allocation, no enum boxing in dispatch, no closure or delegate allocation in guard hooks, no `List` enumeration races. These are achievable with the typed contract and not achievable as a patch on the current one.

### DEC-10 — The descriptor is a separate track

`IStateMachineDescriptor<TState, TTrigger>` describes *what a machine is*; the lifecycle stream describes *what just happened*. It has its own unresolved questions — metadata volume, binary size, always-on versus opt-in, callback names, payload metadata, HSM topology, trimming and AOT — and its own benchmarks. Tracing, metrics and logging do not depend on it; a full structural inspector does. **It appears in none of the PRs below.**

### DEC-11 — Logging keeps its own dispatch

The logging generator uses `LoggerMessage.Define` and is allocation-free on its hot path; routing it through the extension pipeline would regress that, and it is not a precondition for Observability. What converges is **vocabulary**: event names, `TransitionOutcome`, `TransitionStage`, the meaning of source / declared / resolved / final / handled-at, and tag names. Dispatch stays separate and may converge later only if a benchmark shows no regression.

### DEC-12 — An attempt and a transition are separate structures

The unit of correlation is the **attempt** — one `Fire` or `TryFire` call. A transition may or may not exist within it.

```csharp
public readonly struct TransitionAttemptContext<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    public Guid InstanceId { get; }
    public long AttemptId { get; }
    public TState SourceState { get; }
    public TTrigger Trigger { get; }
    public object? Payload { get; }
    public long StartTimestamp { get; }
}

public readonly struct TransitionInfo<TState>
    where TState : unmanaged, Enum
{
    public TState HandledAtState { get; }
    public TState? DeclaredTarget { get; }   // null when Kind == Internal
    public TransitionKind Kind { get; }
}
```

This is what makes the outcomes coherent rather than contradictory:

| outcome | transition exists? |
|---|---|
| `UnhandledTrigger` | no |
| `InvalidPayload` | may not — payload is validated before selection |
| `GuardRejected` | matched, then rejected |
| `Succeeded`, `Faulted`, `Canceled` | matched |

`OnTransitionStarting` is renamed `OnAttemptStarting`: an unhandled trigger is not a transition that started.

### DEC-13 — `FinalState` is distinct from `ResolvedTarget`

Because state assignment precedes entry callbacks (see *Current behaviour* above), a failed attempt can leave the machine in the target state. The result therefore reports where the machine *is*, separately from where target resolution *landed*, and the latter is nullable because resolution may never have happened.

### DEC-14 — The extension set is snapshotted once per attempt

Per-hook `Volatile.Read` is insufficient. It permits an extension to receive `OnAttemptStarting` and then be removed before `OnAttemptCompleted` — a tracer opens an `Activity` and never closes it — and equally permits an extension added mid-attempt to receive a completion for a start it never saw.

Normatively:

> The extension set and the hook mask are captured exactly once at the beginning of an attempt and remain fixed for that attempt.

The array and the mask must also be published **together**, since two independent volatile writes let a reader observe a new array with an old mask:

```csharp
private sealed class ExtensionSet<TState, TTrigger>
{
    public readonly IStateMachineExtension<TState, TTrigger>[] Items;
    public readonly ExtensionHooks Hooks;
}

private ExtensionSet<TState, TTrigger> _extensionSet;
```

Mutation builds a new `ExtensionSet` under a lock and publishes it with a single `Volatile.Write`. An attempt performs a single `Volatile.Read` and passes that same reference through every hook — which is also precisely the "one volatile read and one branch" the performance requirements demand.

### DEC-15 — State lifecycle is separate from callback lifecycle

A state can be exited or entered with no callback attached, and in HSM a single transition exits and enters several states of which only some have callbacks. An inspector must see `A exited` / `B entered` regardless of whether `OnExitA` or `OnEntryB` exists. Revision 1 conflated FSM semantics with user-callback invocation by passing `callbackName` on the state hooks.

```csharp
void OnStateExiting(in TransitionAttemptContext<TState, TTrigger> attempt, TState state);
void OnStateEntered(in TransitionAttemptContext<TState, TTrigger> attempt, TState state);

void OnCallbackExecuting(in TransitionAttemptContext<TState, TTrigger> attempt,
                         TransitionStage stage, string callbackName);
void OnCallbackFaulted(in TransitionAttemptContext<TState, TTrigger> attempt,
                       TransitionStage stage, string callbackName, Exception exception);
```

The asymmetry is deliberate: exit is announced while the machine is still in the source state, entry once the target state is actually active. Actions need no dedicated hook — they are `TransitionStage.Action` on the callback hooks.

### DEC-16 — `OnMachineStopped` and `Stop()` are out of scope for 0.10

`Stop()` does not exist on `StateMachineBase`, and defining it pulls a new FSM feature into an observability project: whether it runs exit callbacks, whether it is idempotent, whether a machine can be restarted, how it relates to `IDisposable`, what `StopAsync` means, and what happens to a pending async transition. None of that is Extension Contract v2.

0.10 ships `OnMachineStarted` only. Default interface implementations make `OnMachineStopped` addable later without a break. The dead `MachineStopped` event in the current logging generator — which nothing raises — must not dictate core semantics.

### DEC-17 — An internal transition has no target, and the contract says so

`InternalTransition(A, Refresh)` has no target. `A` is where the transition is *handled*, not where it points. The generator's own model sets `ToState = FromState` for internal transitions (`StateMachineParser.cs:1191-1196`), which is an implementation convenience, not a semantic fact — surfacing it as `DeclaredTarget = A` would fabricate a target exactly as D3 fabricated one for composite entry.

```
External:  DeclaredTarget = target        ResolvedTarget = actual leaf
Internal:  DeclaredTarget = null          ResolvedTarget = null
Always:    FinalState     = the state the machine is in
```

`TState` has no absent value and `default(TState)` is an ordinary state, so absence must be carried by `Nullable<TState>` rather than a sentinel. This also makes `DeclaredTarget` and `ResolvedTarget` symmetric: both are nullable for the same reason, and `FinalState` remains the one value that is always true.

### DEC-18 — A transition is *matched* before its guard runs, not selected after it

Revision 2 was internally inconsistent: it stated that `GuardRejected` carries the transition, while placing `OnTransitionSelected` after `OnGuardEvaluated`, so a rejected guard produced a result naming a transition whose hook never fired.

The word is *matched*. FastFsm chooses a transition from `(state, trigger, priority)`, and the guard then decides whether it may execute. Matching and guarding are separate steps, and matching is what happens first:

```
OnAttemptStarting
OnTransitionMatched
OnGuardEvaluating
OnGuardEvaluated
…
OnAttemptCompleted
```

The result property is `MatchedTransition` for the same reason. Under current single-candidate selection, matched and selected are the same event; naming it `Matched` avoids implying a selection step that does not exist.

This composes with the fallthrough question in *Current behaviour*: were guard fallthrough added later, `OnTransitionMatched` would fire per candidate and a distinct `OnTransitionSelected` for the winner could be added additively under DEC-1's default interface implementations, without breaking anything written against this contract.

The `candidate` parameter is retained on both guard hooks even though it is redundant today, so that a stateless extension can act on a guard hook without having stashed state from `OnTransitionMatched`.

## Packaging constraints

- `ExtensionRunner.cs` is `Compile Remove`d from `FastFsm.dll` and shipped as `contentFiles/cs/any` with `BuildAction=Compile` (`Fsm.Core.csproj`). It is compiled into each consuming assembly as an internal type. **It is an implementation detail and must never appear in the extension contract.**
- `FastFsm.Sharp.Logging` and `FastFsm.Sharp.DependencyInjection` are content-only packages (`IncludeBuildOutput=false`); their types are likewise compiled into the consumer.
- Therefore **every type an extension author references must be compiled into `FastFsm.dll` or `Abstractions.dll`** — the two assemblies actually shipped in `lib/net10.0`.
- `FastFsm.Sharp.Observability` must ship as a normal `lib/net10.0` assembly. Content-only distribution would duplicate its types in every consuming assembly and make the `System.Diagnostics.DiagnosticSource` dependency unversionable. This is a deliberate departure from the Logging and DI convention.

## Defects v2 must eliminate

Each is observable in 0.9.1 and is pinned by a test in PR 0.

| id | defect | location |
|----|--------|----------|
| D1 | `InstanceId` is a fresh GUID string per attempt, so nothing can correlate events of one machine | generator `:934, 1282, 1516, 1641, 2346, 2372` |
| D2 | internal-transition classification is a runtime `From == To` heuristic: false positive on external self-transition, false negative on HSM ancestor-internal | `ExtensionRunner.cs:135` |
| D3 | `ToState` is the declared target; the context is built before composite resolution | generator `:1281-1285` vs `:1389` |
| D4 | HSM contexts describe a transition that did not occur (`FromState = leaf`, `ToState = ancestor`) | consequence of D2 + D3 |
| D5 | `bool success` conflates guard rejection, unhandled trigger, payload mismatch, callback exception and cancellation | `IStateMachineExtension.OnAfterTransition` |
| D6 | `AddExtension` / `RemoveExtension` exist only on the concrete class; `IExtensibleStateMachine` is an empty marker and DI hands out `TInterface` | `ExtensionsFeatureWriter.cs:25-37`, `StateMachineFactory.cs:23` |
| D7 | `StateMachineContext<TState, TTrigger, TPayload>` is never emitted; payloads always arrive boxed | dead code in `Runtime/StateMachineContext.cs` |
| D8 | `DateTime.UtcNow` per context: non-monotonic, costed on every attempt, no hook carries elapsed time | `Runtime/StateMachineContext.cs` |
| D9 | capturing lambdas in guard hooks allocate per call; `IStateSnapshot` boxes both enums on every success | `ExtensionRunner.cs` guard hooks and `:135` |
| D10 | `_extensionsList` and `_extensions` are the same `List<>`; mutation during dispatch is undefined | `ExtensionsFeatureWriter.cs:9-10, 17-18` |
| D11 | tests assert only `context.GetType().Name`; no test asserts any context value, which is why D1–D4 survived | `Tests.Fsm/Extensions/ExtensionsStandaloneTests.cs` |
| D12 | a failed attempt reports a machine position the machine is not in, because state assignment precedes entry callbacks | generator, state change before `OnEntry` |
| D13 | the extensions variant wraps the transition body, guard included, in an unfiltered `catch`, so a throwing guard is indistinguishable from a rejecting one, a cancelled guard reports as rejection, and adding extensions changes the machine's exception semantics | `WriteTransitionLogicSyncWithExtensions`; `GuardGenerationHelper` called with `treatCancellationAsFailure: false` |

## Target contract

All types live in `FastFsm.Contracts` inside `FastFsm.dll`.

### Result

```csharp
public readonly struct TransitionResult<TState>
    where TState : unmanaged, Enum
{
    public TransitionOutcome Outcome { get; }

    public TState FinalState { get; }                    // always true
    public TState? ResolvedTarget { get; }               // null for internal, and if resolution never ran
    public TransitionInfo<TState>? MatchedTransition { get; }

    public TransitionStage? Stage { get; }
    public Exception? Exception { get; }
}

public enum TransitionOutcome
{
    Succeeded,
    GuardRejected,
    UnhandledTrigger,
    InvalidPayload,
    Canceled,
    Faulted
}
```

`Stage` is nullable because it is meaningful for `Faulted` and possibly `Canceled`, and must not carry a placeholder on success.

### Hook sequence

```
Fire / TryFire
      │
      ▼
TransitionAttemptContext
 InstanceId, AttemptId, SourceState, Trigger, Payload, StartTimestamp
      │
      ├── OnAttemptStarting(attempt)
      │
      ├── OnTransitionMatched(attempt, matched: TransitionInfo)
      │
      ├── OnGuardEvaluating (attempt, candidate: TransitionInfo, guardName)
      ├── OnGuardEvaluated  (attempt, candidate, guardName, result)
      │
      ├── OnStateExiting / OnCallbackExecuting / OnStateEntered / OnCallbackFaulted
      │
      ▼
      OnAttemptCompleted(attempt, result)
```

This resolves, in one model: unhandled trigger, invalid payload, guard rejection, internal-on-ancestor, composite and history resolution, failure after the state has already changed, and trace correlation.

### Primary interface

```csharp
public interface IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    ExtensionHooks Hooks => ExtensionHooks.Transitions;

    void OnAttemptStarting(in TransitionAttemptContext<TState, TTrigger> attempt) { }

    void OnTransitionMatched(in TransitionAttemptContext<TState, TTrigger> attempt,
                             in TransitionInfo<TState> matched) { }

    void OnAttemptCompleted(in TransitionAttemptContext<TState, TTrigger> attempt,
                            in TransitionResult<TState> result) { }

    void OnGuardEvaluating(in TransitionAttemptContext<TState, TTrigger> attempt,
                           in TransitionInfo<TState> candidate, string guardName) { }

    void OnGuardEvaluated(in TransitionAttemptContext<TState, TTrigger> attempt,
                          in TransitionInfo<TState> candidate, string guardName, bool result) { }

    void OnStateExiting(in TransitionAttemptContext<TState, TTrigger> attempt, TState state) { }

    void OnStateEntered(in TransitionAttemptContext<TState, TTrigger> attempt, TState state) { }

    void OnCallbackExecuting(in TransitionAttemptContext<TState, TTrigger> attempt,
                             TransitionStage stage, string callbackName) { }

    void OnCallbackFaulted(in TransitionAttemptContext<TState, TTrigger> attempt,
                           TransitionStage stage, string callbackName, Exception exception) { }

    void OnMachineStarted(Guid instanceId, TState initialState) { }
}
```

Default interface implementations do two things: an extension implements only the hooks it needs, and **new hooks can be added in later versions without breaking existing extensions**. That is what makes a wide surface affordable as a published contract, and what makes DEC-16 safe.

`OnCallbackFaulted` exists because of `ExceptionDirective.Continue`: when a callback throws and the directive is `Continue`, the exception is swallowed and the attempt succeeds, so `OnAttemptCompleted` reports `Succeeded` and the fault would otherwise be invisible.

### Hook mask

```csharp
[Flags]
public enum ExtensionHooks
{
    None        = 0,
    Transitions = 1 << 0,   // attempt starting, transition matched, attempt completed
    Guards      = 1 << 1,
    States      = 1 << 2,   // state exiting, entered
    Callbacks   = 1 << 3,   // callback executing, faulted
    Hierarchy   = 1 << 4,
    Lifecycle   = 1 << 5,   // machine started
    All         = Transitions | Guards | States | Callbacks | Hierarchy | Lifecycle
}
```

`States` and `Callbacks` are separate flags, following DEC-15: an inspector wanting the state stream should not have to pay for callback names.

The mask lives on the `ExtensionSet` (DEC-14) and gates every hook site:

```csharp
if ((set.Hooks & ExtensionHooks.Callbacks) != 0) { /* dispatch */ }
```

Default interface implementations alone do not solve the cost problem: without the mask the machine still constructs and dispatches data no registered extension consumes.

**Footgun to document:** the mask is declarative and authoritative. An extension overriding a hook without declaring its flag is silently never called. This must be stated in `docs/extensions.md` and covered by a test.

### Machine identity and management

```csharp
public interface IStateMachineIdentity
{
    Guid InstanceId { get; }
}

public interface IExtensibleStateMachine<TState, TTrigger> : IStateMachineIdentity
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    IReadOnlyList<IStateMachineExtension<TState, TTrigger>> Extensions { get; }
    void AddExtension(IStateMachineExtension<TState, TTrigger> extension);
    bool RemoveExtension(IStateMachineExtension<TState, TTrigger> extension);
}
```

`IExtensibleStateMachineSync<TState, TTrigger>` and `IExtensibleStateMachineAsync<TState, TTrigger>` inherit it. The empty `IExtensibleStateMachine` marker is removed.

### Types removed

`IStateMachineExtension`, `IStateMachineContext`, `IStateMachineContext<TState, TTrigger>`, `IStateSnapshot`, `StateMachineContext<TState, TTrigger>`, `StateMachineContext<TState, TTrigger, TPayload>`, `IExtensibleStateMachine`.

Hard break, no compatibility shim. The package has no dependent users, and a bridge would preserve the `object`-based contract v2 exists to remove.

## Open design item: payload

The one item still to resolve, and it must be resolved **inside PR 1**, before publication.

`TPayload` must not become a third parameter of the extension interface — a single machine can declare different payload types per trigger, so `IStateMachineExtension<TState, TTrigger, TPayload>` does not model the domain.

1. **`object? Payload` on the attempt context.** One dispatch, simplest, but boxes value-type payloads, contradicting DEC-9 for machines with struct payloads.
2. **Generic hook method**, `OnAttemptStarting<TPayload>(in TransitionAttemptContext<TState, TTrigger, TPayload>)`, with a `NoPayload` empty struct. No boxing, but reintroduces generic-method dispatch on an interface, grows instantiations per payload type, and complicates the default implementations.
3. **No payload in the contract.** Extensions needing payloads use a machine-specific typed extension outside the general contract.

Recommendation: ship (1) for v2, documented explicitly as boxing value-type payloads and unsuitable for hot-path payload inspection, and leave (2) as an additive hook if demand appears.

**Observability ignores payloads by default** regardless of the choice — both because of cost and because payloads routinely carry large objects, personal data and secrets that must not reach a telemetry backend by accident. Payload capture must be opt-in and explicitly scoped.

## Architecture acceptance criterion

Implementation of `FastFsm.Sharp.Observability` begins only when this holds and is demonstrated by tests and benchmarks:

> For every attempt, FastFsm emits a zero- or near-zero-allocation, typed and semantically truthful lifecycle stream. Every event carries a stable `InstanceId` and `AttemptId`. External, self and internal transitions are distinguished explicitly. Where a transition exists, HSM reports the state where it was handled, the declared target and the resolved target; where none exists, no transition is reported rather than a fabricated one. Every completed attempt reports the state the machine is actually in. Extension dispatch is synchronous, does not participate in cancellation, and cannot change the outcome. The extension set is captured once per attempt and is fixed for its duration.

## Test matrix

Normative, not advisory — D11 is the reason.

Every hook is asserted across the product of:

- topology: flat FSM; HSM with composite target; HSM with history; HSM trigger handled on an ancestor;
- execution: sync, async;
- payload: none; single payload; multi-payload with valid and invalid payload type;
- outcome: success, guard rejection, unhandled trigger, invalid payload, callback exception with `Propagate`, callback exception with `Continue`, cancellation;
- kind: external, internal on leaf, internal on ancestor, external self-transition.

Assertions must cover **content**, not merely invocation:

- `InstanceId` identical across every hook of every attempt on one machine, and distinct between instances;
- `AttemptId` identical across every hook of one attempt, strictly increasing across attempts, in sync and async;
- `MatchedTransition` absent for `UnhandledTrigger` and for `InvalidPayload` rejected before matching; present for `GuardRejected`, and `OnTransitionMatched` actually raised in that case;
- `OnTransitionMatched` raised **before** `OnGuardEvaluating`, and exactly once per attempt;
- `HandledAtState` and `Kind` correct for every topology, including internal-on-ancestor;
- `DeclaredTarget` and `ResolvedTarget` both null for internal transitions, on the leaf and on an ancestor;
- `ResolvedTarget` null where resolution never ran; correct leaf for composite and history targets;
- **`FinalState` equal to `CurrentState` immediately after the attempt returns, in every outcome** — including entry-callback failure, where it must equal the target and not the source;
- `Outcome` and `Stage` correct for every failure mode;
- `OnCallbackFaulted` raised exactly when a callback throws under `ExceptionDirective.Continue`, with `OnAttemptCompleted` still reporting `Succeeded`;
- state hooks raised for states without callbacks, and for every state in an HSM multi-level exit and entry;
- hook ordering, including exit, callback and entry relative to `OnAttemptCompleted`;
- an extension removed mid-attempt still receives `OnAttemptCompleted`, and one added mid-attempt receives nothing for that attempt;
- an extension overriding a hook without declaring its flag is not called.

## Performance requirements

Acceptance criteria, verified by benchmarks in `src/Benchmark`, which currently contains none for extensions.

- A machine compiled without `GenerateExtensibleVersion` produces byte-identical generated code to 0.9.1. Enforced by a golden-file test.
- A machine compiled with extensions but with none registered allocates zero bytes per attempt; its cost over a non-extensible machine is one volatile read and one branch. `StartTimestamp` is not taken on this path (DEC-7).
- A machine with N registered extensions allocates zero bytes per attempt on every path, including guard evaluation and every failure path.
- The DEC-9 preconditions hold.

**Benchmarks are measurement, not CI gates.** Where a benchmark does become a CI assertion it must gate on an absolute figure — nanoseconds per transition, bytes per attempt — never on a ratio between two sub-nanosecond measurements. The existing `GuardEvaluation_PerformanceImpact_Improved` demonstrates the failure mode: it currently fails on Ubuntu at 158.6% against a 150% ratio threshold while the underlying absolute difference is 1.5 ns per transition, and it fails *after* the paired-run stabilisation already landed. PR 0 must not add more tests of that shape.

## PR plan

| PR | scope | goal |
|----|-------|------|
| **PR 0** — characterize extension contract | full matrix flat/HSM × sync/async × payload × guards × self/internal/composite; complete assertions on context content and ordering; extension benchmarks establishing the 0.9.1 baseline | stop refactoring blind |
| **PR 1** — Extension Contract v2 | the complete final public shape: `TransitionAttemptContext`, `TransitionInfo`, `TransitionResult`, `TransitionOutcome`, `TransitionKind`, typed extension interface with every hook declared, `ExtensionHooks`, identity, `ExtensionSet` management. Behaviour for `Succeeded`, `GuardRejected`, `UnhandledTrigger`, `InvalidPayload`. Payload decision resolved | **model correctness** |
| **PR 2** — outcomes and failures | no API shape change. Semantics only: `Faulted`, `Canceled`, `TransitionStage`, `Exception`, `ExceptionDirective.Continue`, raising `OnCallbackFaulted`. Includes the D13 emission fix, without which none of these outcomes is producible | behaviour completion, not a second redesign |
| **PR 3** — lifecycle surface | `OnMachineStarted`; state and callback hooks wired at every site; hook mask enforced; monotonic `StartTimestamp`; zero-closure dispatch | event stream sufficient for tracing and metrics |
| **PR 4** — HSM observability semantics | audit and removal of the existing stubs; only well-defined HSM events survive; sync/async parity | full HSM credibility |
| **PR 5** — `FastFsm.Sharp.Observability` | `ActivitySource`, `Meter`, optional `ILogger` sink, runtime inspection stream, as an open generic extension in a real `lib/net10.0` assembly | first public extension |

The descriptor track runs independently and blocks none of them.

### Notes on individual PRs

**PR 1 ships the final type shapes, including outcome members it does not yet raise.** This is deliberate: revision 1 deferred the result types to PR 2, which invited a temporary contract that PR 2 would then break again. PR 2 must be a behaviour extension, not a second redesign.

PR 1 also clears two pieces of accumulated friction: the `GenerateExtensibleVersion` default-value inconsistency noted in `docs/extensions.md`, and the redundant `ExtensionsOn => HasExtensions || IsExtensionsVariant()`, whose operands read the same field.

**PR 2 is the one PR that must change emission, not only wiring.** D13 destroys the guard and callback exception before any hook could see it, so `Faulted`, `Canceled` and `Stage` cannot be raised until the blanket `catch` in the extensions path is replaced by filtered handling that distinguishes a thrown guard from a rejecting one and an `OperationCanceledException` from either. Restoring parity with the non-extensions variant is part of the fix, since under DEC-3 adding an extension must not change what the machine does with an exception.

**PR 4 starts from deletion.** The five stubs in `ExtensionRunner` — `RunTransitionCompleted`, `RunBubbleToParent`, `RunInitialSubstateEntered`, `RunHistoryRestore`, `RunAncestorPathChanged` — have no call sites anywhere in the generator, and `ReadOnlySpan<TState>` cannot be retained by an extension, which is exactly what an inspector needs to do.

Several may prove unnecessary. With `SourceState`, `HandledAtState`, `DeclaredTarget`, `ResolvedTarget`, `FinalState` and `Kind` in the model, and per-state `OnStateExiting` / `OnStateEntered` from DEC-15, an exit and entry path is already observable as a sequence; whole paths should not be pushed into hot-path events. Whether the LCA belongs on the attempt context — where it costs on every HSM attempt and is currently computed only under `ShouldGenerateLogging` — or only in a mask-gated hierarchy event, is decided here.

**Release:** the contract is complete after PR 4 and ships as `0.10.0`, with the break recorded in `CHANGELOG.md` and `docs/extensions.md` rewritten against the delivered contract — normative ordering, the synchronous-hook requirement, the per-attempt snapshot semantics, the hook-mask footgun, and the meaning of every `TransitionOutcome` including that `GuardRejected` refers to the single matched transition. The existing constructor example must also be corrected: it shows a `logger:` parameter that non-logging machines do not have. Observability ships separately once PR 5 lands.
