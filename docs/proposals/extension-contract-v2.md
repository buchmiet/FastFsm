# Extension contract v2

> **Status:** accepted design, not yet implemented. This document is not part of the current FastFsm API contract; it defines the contract that replaces it.
>
> **Scope:** everything required before `IStateMachineExtension` can be treated as a public interop surface, and before `FastFsm.Sharp.Observability` can be built as an ordinary client of FastFsm rather than another special path in the generator.

## Position

FastFsm 0.9.1 ships an extension mechanism that is structurally sound — opt-in generation, deterministic hook order, exception isolation, sync and async support. Its *data contract* is not sound.

The decisive finding is not that the hook surface is incomplete. It is that **the data delivered to extensions in HSM is untrue**: a trigger handled on an ancestor is reported as a state change that never occurred, and a transition into a composite state reports the composite rather than the leaf actually entered.

Adding hooks on top of that model would multiply the problem. The priority is therefore:

> first guarantee that the hooks tell the truth, then widen them.

Accordingly: **`FastFsm.Sharp.Observability` is not started until Extension Contract v2 lands.** The centre of gravity for v2 is

> correct typed transition model + stable identity + explicit transition semantics + tested lifecycle correlation

and tracing, metrics and inspection are built on that, not beside it.

## Decisions taken

These are settled and are not revisited during implementation. Each is recorded with its reason, because each forecloses an option that will look attractive again mid-implementation.

### DEC-1 — The primary contract is generic in `TState` and `TTrigger`

```csharp
public interface IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
```

The current design uses generic *methods* constrained to `IStateMachineContext`, which forces enum state and trigger values through `object`. This cannot be changed after publication without breaking every extension ever written. It is changed now, while the package has no dependent users.

### DEC-2 — No untyped extension interface in core

A machine-agnostic extension is an **open generic type**, not an untyped interface:

```csharp
public sealed class ObservabilityExtension<TState, TTrigger>
    : IStateMachineExtension<TState, TTrigger>
```

registered in DI as an open generic. Where an inspector needs to aggregate events across ten different machine types, the conversion to a machine-agnostic event model happens **in the observability sink, not in core**:

```
typed FastFsm hot path
      ↓
ObservabilityExtension<TState, TTrigger>
      ↓
machine-agnostic ObservabilityEvent
      ↓
OTel / logger / inspector / recorder
```

not

```
FastFsm → boxing → object/object/object → extension
```

Maintaining typed and untyped extension APIs in parallel would preserve the boxed path that v2 exists to remove, and would double the surface that has to stay correct.

### DEC-3 — Extension dispatch is synchronous and inert

There is no `IStateMachineExtensionAsync`. The contract states normatively:

> Extension callbacks are synchronous, short and non-blocking. They do not participate in cancellation and cannot alter the outcome of a transition.

Observability records, tags, or enqueues. It does not `await exporter.SendAsync(...)` inside a transition. `CancellationToken` is therefore **not** exposed on the context: transition cancellation belongs to the machine, and observability must not be able to influence it. `TransitionOutcome.Canceled` conveys the observable fact without handing over the control.

This is a deliberate restriction and is part of the value of the contract.

### DEC-4 — `Guid InstanceId`, not `string`

Machine identity is a `Guid` allocated once per machine instance. A string GUID allocates unconditionally; a `Guid` allocates only if and when a tracer actually formats it, which for sampled tracing is a small fraction of attempts.

A caller-supplied *correlation* identifier — order id, tenant id, saga id — is **not** machine identity and does not belong on the context. An extension that needs it holds it and tags its own spans. A constructor overload accepting a `Guid` may be offered so a caller can align machine identity with an external identifier.

### DEC-5 — `TransitionKind` distinguishes external from internal; self-transition is not a third kind

```csharp
public enum TransitionKind { None, External, Internal }
```

A self-transition is `External` with `SourceState == ResolvedTarget`. That is precisely what distinguishes it from a true internal transition, which runs no exit or entry callbacks. Introducing a `Self` kind would invite consumers to treat the distinction as a property of the transition rather than of its effects.

`None` is used only for `UnhandledTrigger`, where no transition matched and `HandledAtState`, `DeclaredTarget` and `Kind` have no meaningful value.

This kind is **emitted from `TransitionModel.IsInternal` at compile time**. The runtime heuristic in `ExtensionRunner` disappears, and with it the `IStateSnapshot` boxing it required.

### DEC-6 — Both declared and resolved target are retained

Fixing `ToState` must not be done by discarding the declared target. Both are diagnostically useful:

```
Transition(X, Trigger, Composite)
  DeclaredTarget = Composite
  ResolvedTarget = Composite.InitialLeaf   (or the leaf recovered from history)
```

### DEC-7 — Core exposes a monotonic start timestamp, not a computed duration

The context carries `long StartTimestamp` from `Stopwatch.GetTimestamp()`. Extensions that need duration call `Stopwatch.GetElapsedTime(context.StartTimestamp)` themselves.

This gives one monotonic clock read per attempt, zero allocations, no `DateTime` misused as a stopwatch, and no histogram policy imposed by core. Logging, `Activity` and OTel each keep their own time policy.

`DateTime Timestamp` is removed from the contract. A logging provider timestamps its own entries.

### DEC-8 — Extension storage is a copy-on-write snapshot

```csharp
private IStateMachineExtension<TState, TTrigger>[] _extensions;
```

Read path takes `Volatile.Read` once per attempt and indexes the array with no lock. Mutation copies the array under a lock and publishes with `Volatile.Write`. Normative semantics:

> An extension added or removed during an attempt does not affect the snapshot used by a dispatch that has already begun.

`Extensions`, `AddExtension` and `RemoveExtension` move onto `IExtensibleStateMachine<TState, TTrigger>`, so that a machine obtained from DI is not degraded relative to the concrete type.

### DEC-9 — The zero-allocation requirements are contract preconditions, not later optimisations

Once Observability is the first official extension, the extension path stops being an exotic slow path. Contract v2 must satisfy, as acceptance criteria:

- no per-attempt `Guid` allocation;
- no enum boxing in dispatch;
- no closure or delegate allocation in guard hooks;
- no `List` enumeration races.

The typed contract is what makes these achievable; they are not achievable as a patch on the current one.

### DEC-10 — The descriptor is a separate track

`IStateMachineDescriptor<TState, TTrigger>` describes *what a machine is*; the lifecycle stream describes *what just happened*. The descriptor has its own unresolved questions — how much metadata to emit, binary-size impact, always-on versus opt-in, callback names, payload metadata, HSM topology, trimming and AOT — and needs its own design and its own binary-size benchmarks.

Tracing, metrics and logging do not depend on it. A full structural inspector does. **It appears in none of the PRs below.**

### DEC-11 — Logging keeps its own dispatch

The logging generator uses `LoggerMessage.Define` and is allocation-free on its hot path. Routing it through the extension pipeline would regress that, and it is not a precondition for Observability.

What converges now is **vocabulary**, not dispatch:

- event names;
- `TransitionOutcome`;
- `TransitionStage`;
- the meaning of source, declared target, resolved target and handled-at;
- tag names.

What stays separate:

```
logging dispatch       → existing generated zero-alloc path
observability dispatch → extension path
```

They may converge later if a benchmark shows no regression. That is not a goal of this work.

## Packaging constraints

These follow from the current repository layout and constrain what the contract may contain.

- `ExtensionRunner.cs` is `Compile Remove`d from `FastFsm.dll` and shipped as `contentFiles/cs/any` with `BuildAction=Compile` (`Fsm.Core.csproj`). It is compiled into each consuming assembly as an internal type. **It is an implementation detail and must never appear in the extension contract.**
- `FastFsm.Sharp.Logging` and `FastFsm.Sharp.DependencyInjection` are content-only packages (`IncludeBuildOutput=false`); their types are likewise compiled into the consumer.
- Therefore **every type an extension author references must be compiled into `FastFsm.dll` or `Abstractions.dll`** — the two assemblies actually shipped in `lib/net10.0`.
- `FastFsm.Sharp.Observability` must ship as a normal `lib/net10.0` assembly, not as content files. Content-only distribution would duplicate its types in every consuming assembly and make the `System.Diagnostics.DiagnosticSource` dependency unversionable. This is a deliberate departure from the Logging and DI packaging convention.

## Defects v2 must eliminate

Each is observable in 0.9.1 and is pinned by a test in PR 0.

| id | defect | location |
|----|--------|----------|
| D1 | `InstanceId` is a fresh GUID per attempt, so no extension can correlate events of one machine | `UnifiedStateMachineGenerator.cs:934, 1282, 1516, 1641, 2346, 2372` |
| D2 | internal-transition classification is a runtime `From == To` heuristic: false positive on external self-transition, false negative on HSM ancestor-internal | `ExtensionRunner.cs:135` |
| D3 | `ToState` is the declared target; the context is built before composite resolution | generator `:1281-1285` vs `:1389` |
| D4 | HSM contexts describe a transition that did not occur (`FromState = leaf`, `ToState = ancestor`) | consequence of D2 + D3 |
| D5 | `bool success` conflates guard rejection, unhandled trigger, payload mismatch, callback exception and cancellation | `IStateMachineExtension.OnAfterTransition` |
| D6 | `AddExtension` / `RemoveExtension` exist only on the concrete class; `IExtensibleStateMachine` is an empty marker and DI hands out `TInterface` | `ExtensionsFeatureWriter.cs:25-37`, `StateMachineFactory.cs:23` |
| D7 | `StateMachineContext<TState, TTrigger, TPayload>` is never emitted; payloads always arrive boxed as `object?` | dead code in `Runtime/StateMachineContext.cs` |
| D8 | `DateTime.UtcNow` per context: non-monotonic, costed on every attempt, and no hook carries elapsed time | `Runtime/StateMachineContext.cs` |
| D9 | capturing lambdas in guard hooks allocate a closure and delegate per call; `IStateSnapshot` boxes both enums on every success | `ExtensionRunner.cs` guard hooks and `:135` |
| D10 | `_extensionsList` and `_extensions` are the same `List<>`; mutation during dispatch is undefined | `ExtensionsFeatureWriter.cs:9-10, 17-18` |
| D11 | tests assert only `context.GetType().Name`; no test asserts any context value, which is why D1–D4 survived | `Tests.Fsm/Extensions/ExtensionsStandaloneTests.cs` |

## Target contract

All types live in `FastFsm.Contracts` inside `FastFsm.dll`.

### Context and result

```csharp
public readonly struct TransitionContext<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    public Guid InstanceId { get; }        // stable for the machine instance lifetime
    public long AttemptId { get; }         // one per Fire / TryFire attempt

    public TState SourceState { get; }     // actual active leaf before the attempt
    public TTrigger Trigger { get; }
    public TState HandledAtState { get; }  // state owning the matched transition
    public TState DeclaredTarget { get; }  // target as written in the definition
    public TransitionKind Kind { get; }

    public object? Payload { get; }        // see "Payload" below
    public long StartTimestamp { get; }    // Stopwatch.GetTimestamp()
}

public readonly struct TransitionResult<TState>
    where TState : unmanaged, Enum
{
    public TransitionOutcome Outcome { get; }
    public TState ResolvedTarget { get; }  // leaf actually entered; == SourceState when not Succeeded
    public TransitionStage? Stage { get; } // FastFsm.Exceptions.TransitionStage
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

`Stage` is nullable because it is meaningful for `Faulted` and possibly `Canceled`, and must not be filled with a placeholder on success.

The split between context and result is not cosmetic. `SourceState`, `HandledAtState`, `DeclaredTarget` and `Kind` are known when the attempt starts; `ResolvedTarget` is knowable only after composite and history resolution. Forcing them into one struct is what produced D3. The split also maps directly onto span start and span end.

Worked example — `InternalTransition(A, Refresh)` with active leaf `A1` under composite `A`:

```
SourceState    = A1
Trigger        = Refresh
HandledAtState = A
DeclaredTarget = A
Kind           = Internal
ResolvedTarget = A1
```

Today this is reported as `A1 → A`, which is fiction.

### Primary interface

```csharp
public interface IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    ExtensionHooks Hooks => ExtensionHooks.Transitions;

    void OnTransitionStarting(in TransitionContext<TState, TTrigger> context) { }

    void OnTransitionCompleted(in TransitionContext<TState, TTrigger> context,
                               in TransitionResult<TState> result) { }

    void OnGuardEvaluating(in TransitionContext<TState, TTrigger> context,
                           string guardName) { }

    void OnGuardEvaluated(in TransitionContext<TState, TTrigger> context,
                          string guardName, bool result) { }

    void OnStateExiting(in TransitionContext<TState, TTrigger> context,
                        TState state, string callbackName) { }

    void OnActionExecuting(in TransitionContext<TState, TTrigger> context,
                           string actionName) { }

    void OnStateEntering(in TransitionContext<TState, TTrigger> context,
                         TState state, string callbackName) { }

    void OnCallbackFaulted(in TransitionContext<TState, TTrigger> context,
                           TransitionStage stage, string callbackName,
                           Exception exception) { }

    void OnMachineStarted(Guid instanceId, TState initialState) { }

    void OnMachineStopped(Guid instanceId, TState finalState) { }
}
```

Default interface implementations do two things: an extension implements only the hooks it needs, and **new hooks can be added in later versions without breaking existing extensions**. That is what makes a wide lifecycle surface affordable as a published contract.

Callback hooks are pre-execution only. Per-callback duration is out of scope for v2: fault attribution is already carried by `TransitionResult.Stage`, and a paired post-execution hook can be added additively if a concrete need appears.

`OnCallbackFaulted` exists because of `ExceptionDirective.Continue`. When a callback throws and the directive is `Continue`, the exception is swallowed and the transition succeeds — so `OnTransitionCompleted` reports `Succeeded` and the fault would otherwise be invisible to observability. This hook is the only way a swallowed fault reaches an extension.

### Hook mask

```csharp
[Flags]
public enum ExtensionHooks
{
    None        = 0,
    Transitions = 1 << 0,   // starting, completed
    Guards      = 1 << 1,
    Callbacks   = 1 << 2,   // exiting, action, entering, callback fault
    Hierarchy   = 1 << 3,
    Lifecycle   = 1 << 4,   // machine started, stopped
    All         = Transitions | Guards | Callbacks | Hierarchy | Lifecycle
}
```

The machine caches the bitwise OR of all registered extensions' `Hooks`, recomputed on add and remove alongside the copy-on-write array. Every hook site is guarded:

```csharp
if ((_hookMask & ExtensionHooks.Callbacks) != 0) { /* dispatch */ }
```

Default interface implementations alone do not solve the cost problem: without the mask the machine still constructs and dispatches data that no registered extension consumes. The mask is what makes fine-grained callback hooks affordable for machines that do not want them. It is load-bearing, not an optimisation to defer.

**Footgun to document:** the mask is declarative and authoritative. An extension that overrides a hook without declaring the corresponding flag will silently not be called. This must be stated in `docs/extensions.md` and covered by a test.

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

Hard break, no compatibility shim. The package has no dependent users, and a bridging layer would preserve the `object`-based contract v2 exists to remove.

## Open design item: payload

This is the one item still to be resolved, and it must be resolved **inside PR 1**, before publication.

`TPayload` must not become a third parameter of the extension interface — a single machine can declare different payload types per trigger, so `IStateMachineExtension<TState, TTrigger, TPayload>` does not model the domain.

Three candidates:

1. **`object? Payload` on the context.** One dispatch, simplest, but boxes value-type payloads. Contradicts DEC-9 for machines with struct payloads.
2. **Generic hook method**, `void OnTransitionStarting<TPayload>(in TransitionContext<TState, TTrigger, TPayload> context)`, with a `NoPayload` empty struct for triggers without one. No boxing, but reintroduces generic-method dispatch on an interface, grows generic instantiations per payload type, and complicates the default implementations.
3. **No payload in the contract at all.** Extensions needing payloads use a machine-specific typed extension outside the general contract.

Recommendation: ship (1) for v2, documented explicitly as boxing value-type payloads and unsuitable for hot-path payload inspection, and leave (2) as an additive hook if demand appears — default interface implementations make that non-breaking. The recommendation is contingent on the next paragraph holding.

**Observability ignores payloads by default**, regardless of which option is chosen — both because of cost and because payloads routinely carry large objects, personal data and secrets that must not reach a telemetry backend by accident. Payload capture in Observability must be opt-in and explicitly scoped.

## Architecture acceptance criterion

Implementation of `FastFsm.Sharp.Observability` begins only when this statement is true and demonstrated by tests and benchmarks:

> For every transition attempt, FastFsm emits a zero- or near-zero-allocation, typed and semantically truthful lifecycle stream. Every event carries a stable `InstanceId` and `AttemptId`. External, self and internal transitions are distinguished explicitly. HSM reports the state where the transition was handled, the declared target, and the leaf actually reached. Extension dispatch is synchronous, does not participate in cancellation, and cannot change the outcome of a transition. Dynamic add and remove use atomic snapshots.

When it holds, Observability is an ordinary client of FastFsm rather than another special path in the generator.

## Test matrix

Normative, not advisory — D11 is the reason.

Every hook is asserted across the product of:

- topology: flat FSM; HSM with composite target; HSM with history; HSM trigger handled on an ancestor;
- execution: sync, async;
- payload: none; single payload; multi-payload with valid and invalid payload type;
- outcome: success, guard rejection, unhandled trigger, callback exception with `Propagate`, callback exception with `Continue`, cancellation;
- kind: external, internal on leaf, internal on ancestor, external self-transition.

Assertions must cover context **content**, not merely hook invocation:

- `InstanceId` identical across every hook of every attempt on one machine, and distinct between instances;
- `AttemptId` identical across every hook of one attempt and strictly increasing across attempts, in sync and async;
- `SourceState`, `HandledAtState`, `DeclaredTarget`, `ResolvedTarget`, `Kind` correct for every topology, including the worked example above;
- `Outcome` and `Stage` correct for every failure mode;
- `OnCallbackFaulted` raised exactly when a callback throws under `ExceptionDirective.Continue`, with `OnTransitionCompleted` still reporting `Succeeded`;
- hook ordering, including the position of exit, action and entry relative to `OnTransitionCompleted`;
- an extension overriding a hook without declaring its flag is not called.

## Performance requirements

Acceptance criteria, verified by benchmarks in `src/Benchmark`, which currently contains none for extensions.

- A machine compiled without `GenerateExtensibleVersion` produces byte-identical generated code to 0.9.1. Enforced by a golden-file test.
- A machine compiled with extensions but with none registered allocates zero bytes per attempt; its cost over a non-extensible machine is one volatile read and one branch.
- A machine with N registered extensions allocates zero bytes per attempt on every path, including guard evaluation and every failure path.
- The four DEC-9 preconditions hold: no per-attempt `Guid` allocation, no enum boxing in dispatch, no closure or delegate allocation in guard hooks, no `List` enumeration races.

## PR plan

| PR | scope | goal |
|----|-------|------|
| **PR 0** — characterize extension contract | full matrix flat/HSM × sync/async × payload × guards × self/internal/composite; complete assertions on context content and ordering; extension benchmarks establishing the 0.9.1 baseline | stop refactoring blind |
| **PR 1** — Extension Contract v2 | `IStateMachineExtension<TState, TTrigger>`; stable `InstanceId` and `AttemptId`; correct `SourceState` / `HandledAtState` / `DeclaredTarget` / `ResolvedTarget`; explicit `TransitionKind`; typed management API on the interface; copy-on-write storage; removal of the `From == To` heuristic and of `IStateSnapshot`; payload decision resolved | **model correctness** |
| **PR 2** — outcomes and failures | `TransitionOutcome`, `TransitionResult`, nullable `TransitionStage`, exception and fault semantics, `OnCallbackFaulted` for `ExceptionDirective.Continue`, cancellation semantics | end of `bool success` |
| **PR 3** — lifecycle surface | machine start and stop; state exiting, action executing, state entering; hook mask; monotonic `StartTimestamp`; zero-closure dispatch | event stream sufficient for tracing and metrics |
| **PR 4** — HSM observability semantics | audit and removal of the existing stubs; only well-defined HSM events survive; sync/async parity | full HSM credibility |
| **PR 5** — `FastFsm.Sharp.Observability` | `ActivitySource`, `Meter`, optional `ILogger` bridge/sink, runtime inspection stream, as an open generic extension shipped in a real `lib/net10.0` assembly | first public extension |

The descriptor track runs independently of PR 0–5 and blocks none of them.

### Notes on individual PRs

**PR 0** must include benchmarks. `docs/extensions.md` currently documents hook *ordering* but nothing about the *meaning of the data* passed in the context; that gap is exactly what let D1–D4 survive.

**PR 1** also resolves two pieces of accumulated friction: the `GenerateExtensibleVersion` default-value inconsistency noted in `docs/extensions.md`, and the redundant `ExtensionsOn => HasExtensions || IsExtensionsVariant()`, whose two operands read the same field.

**PR 3** must define `Stop()`. It does not exist on `StateMachineBase` today, while the logging generator already emits a `MachineStopped` event that nothing raises. Whether `Stop` is idempotent, whether it runs exit callbacks, and how it relates to `IDisposable` are state-machine semantics decisions, not observability ones — which is why they are not folded into PR 2.

**PR 4** starts from deletion, not adaptation. The five stubs in `ExtensionRunner` — `RunTransitionCompleted`, `RunBubbleToParent`, `RunInitialSubstateEntered`, `RunHistoryRestore`, `RunAncestorPathChanged` — have no call sites anywhere in the generator, and `ReadOnlySpan<TState>` cannot be retained by an extension, which is precisely what an inspector needs to do. Each stub is either given a semantics from scratch or removed.

Some of them may prove unnecessary once the base model is correct. If the context already carries `SourceState`, `HandledAtState`, `DeclaredTarget`, `ResolvedTarget` and `Kind`, an exited and entered path can be reconstructed afterwards from the descriptor. Whole paths should not be pushed into hot-path events. Whether the LCA belongs on the base context — where it would cost on every HSM attempt, and is currently computed only under `ShouldGenerateLogging` — or only in a mask-gated hierarchy event, is decided in this PR.

**Release:** the contract is complete after PR 4 and ships as `0.10.0`, with the break recorded in `CHANGELOG.md` and `docs/extensions.md` rewritten against the delivered contract, including normative ordering, the synchronous-hook requirement, the snapshot semantics of add and remove, the hook-mask footgun, and the meaning of every `TransitionOutcome`. The existing constructor example must also be corrected: it shows a `logger:` parameter that non-logging machines do not have. Observability ships separately once PR 5 lands.
