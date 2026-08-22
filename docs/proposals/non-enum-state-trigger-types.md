# Proposal: non-enum state and trigger types

Status: **investigation / feasibility proposal**

This document evaluates whether FastFsm can support non-enum state and trigger types—especially `record struct` values and smart-enum-style closed symbolic types—without giving up the properties that define the library: compile-time validation, deterministic source generation, trimming/Native AOT compatibility, predictable performance, and zero-allocation transition paths.

The conclusion is positive, with an important qualification:

> FastFsm should not make arbitrary user values the runtime execution model. It should separate the public state/trigger representation from compact generated internal identifiers.

The feature should therefore be framed as **support for closed symbolic state and trigger types through generated dense internal identifiers**, not merely as “allow smart enums”. Smart enums, record structs, and string-backed symbolic values can then be supported as representations of the same closed compile-time vocabulary.

## Executive summary

Technical feasibility is high. The generator already has a useful architectural property: after parsing, much of its semantic model is name-based rather than dependent on enum numeric values. `TransitionModel` stores source state, target state, and trigger as symbolic names, while `StateMachineModel` stores state/hierarchy relationships by symbolic keys.

The strongest enum coupling is at the edges:

- the Attribute and Fluent APIs reject non-enum types;
- validation rules are explicitly enum-oriented;
- runtime and public contracts use `where TState : unmanaged, Enum` and `where TTrigger : unmanaged, Enum`;
- HSM runtime code uses the public enum numeric value directly as an array index;
- generated HSM code casts public states to `int` and casts internal indices back to `TState`;
- typed extensions and observability inherit the same enum constraints.

This means the feature is not a small constraint removal, but it also does **not** require replacing the generator architecture or moving to dictionary-driven runtime dispatch.

The recommended design is:

```text
public state / trigger value
        │
        ▼
generated representation adapter
        │
        ▼
dense StateId / TriggerId
        │
        ▼
switch dispatch / HSM arrays / transition protocol
        │
        ▼
generated representation adapter
        │
        ▼
public state / trigger value
```

The internal IDs are implementation details and must never become observable API semantics.

## Goals

A successful design should:

- support a closed set of symbolic states and triggers that are not CLR enums;
- preserve compile-time validation of all referenced states and triggers;
- preserve deterministic generated output;
- preserve trimming and Native AOT compatibility without runtime reflection;
- keep the transition hot path allocation-free;
- retain compact switch/array-based dispatch rather than introducing general runtime dictionaries;
- support both Fluent and Attribute configuration APIs, even if the Attribute syntax must differ for non-enum values;
- support flat FSM, HSM, sync, async, payloads, extensions, logging, observability, DI, and structural APIs before the feature is considered complete;
- preserve existing enum behavior and performance within a defined regression budget.

## Non-goals for the first implementation

The first implementation should not attempt to support arbitrary open-ended state domains such as unconstrained runtime strings or arbitrary objects created by application code.

For example, this should not imply that every value of the following type is automatically a valid FastFsm state:

```csharp
public readonly record struct State(string Name);
```

If callers can construct `new State(input)` at runtime and the generator cannot know the complete value set, FastFsm loses its closed-world compile-time model.

Likewise, `TState == string` or `TTrigger == string` should be treated as a separate future design problem. A **string-backed smart enum** is compatible with this proposal because the externally visible value may contain a string while the set of legal values remains closed and discoverable at compile time.

## Current architecture and why the feature is feasible

Both configuration APIs converge on the same generator pipeline. The current model already separates type identity from most transition semantics:

- `StateMachineModel.StateType` and `.TriggerType` are fully-qualified type names;
- transitions store `FromState`, `ToState`, and `Trigger` symbolically;
- hierarchy maps are keyed by symbolic state names;
- state metadata has an enum-specific `OrdinalValue`, but most of the semantic model does not require a CLR enum.

This is a favorable starting point. The generator does not need to be redesigned around arbitrary runtime objects. Instead, enum-specific assumptions can be isolated into a representation layer around an internal state/trigger identity model.

## Current hard enum coupling

### Attribute entry point

`StateMachineAttribute` currently rejects any state or trigger type for which `Type.IsEnum` is false. The analyzer and generator rules independently enforce the same requirement.

These checks must evolve from “is enum” to “is a supported closed symbolic type”.

### Fluent API

The compile-time-only Fluent builders use constraints such as:

```csharp
where TState : Enum
where TTrigger : Enum
```

The syntax itself is already suitable for symbolic members:

```csharp
FSM.State(DoorState.Closed)
   .On(DoorTrigger.Open)
   .GoTo(DoorState.Open);
```

For a smart enum or record-struct vocabulary, Roslyn can resolve `DoorState.Closed` and `DoorTrigger.Open` to symbols even when they are not enum constants. The Fluent API is therefore the easiest path for an initial prototype.

### Runtime and contracts

`StateMachineBase<TState,TTrigger>`, the async base, state-machine interfaces, extension interfaces, transition context types, DI helpers, and observability all inherit enum constraints.

Removing or generalizing these constraints is a public API change and must be handled as a coherent product change rather than piecemeal edits.

### HSM indexing

This is the most important implementation coupling.

The current HSM code treats the numeric value of the public enum as an internal state index. It uses operations such as:

```csharp
Convert.ToInt32(_currentState)
(int)state
Enum.ToObject(typeof(TState), index)
```

Generated arrays such as parent, depth, initial-child, and history arrays are therefore coupled to public enum numeric values.

Non-enum support requires this relationship to disappear.

Even for enum users, separating public enum values from internal dense indices is architecturally cleaner: public representation should not be required to equal an internal table index.

## Recommended internal model

Introduce generated dense identities conceptually equivalent to:

```csharp
internal readonly struct StateId
{
    public int Value { get; }
}

internal readonly struct TriggerId
{
    public int Value { get; }
}
```

They do not necessarily need to exist as runtime structs; plain generated integers may be preferable. The key requirement is the semantic separation.

For a vocabulary:

```csharp
public sealed class DoorState
{
    public static readonly DoorState Closed = new("closed");
    public static readonly DoorState Open = new("open");
    public static readonly DoorState Locked = new("locked");
}
```

the generator may produce an internal mapping equivalent to:

```text
DoorState.Closed -> 0
DoorState.Open   -> 1
DoorState.Locked -> 2
```

The numeric IDs are generated implementation details. They must be assigned deterministically and must not depend on object hash codes, runtime registration order, reflection enumeration order, or process-specific identity.

The same model can be used for enums. Existing enum numeric values remain public values, while HSM arrays and transition planning use generated dense IDs.

## Representation adapters

The generated machine needs two representation boundaries:

1. public value -> internal ID;
2. internal ID -> public value.

For enums, the adapter can remain a generated switch and should be extremely cheap.

For smart-enum-style reference types, v1 should favor canonical `static readonly` singleton fields. These have stable identity and can be mapped without reflection. A generated mapping may use direct identity checks where appropriate.

For value types such as `readonly record struct`, generated equality at the representation boundary is acceptable. Equality should not become the internal transition-dispatch mechanism. Once mapped, HSM and transition resolution continue using dense IDs.

A machine may retain both the current public `TState` and current internal state ID if that produces the best hot-path characteristics. For example:

```text
_currentState    : TState
_currentStateId  : int
```

A successful transition updates both together. HSM operations use the ID; `CurrentState` and typed extension callbacks use the public value.

This avoids repeatedly mapping the current state on every trigger attempt.

## What counts as a supported closed symbolic type

The generator must be able to prove the legal vocabulary at compile time.

A conservative v1 contract should recognize canonical static members declared on the state/trigger type, initially restricted to patterns such as:

```csharp
public static readonly DoorState Closed = ...;
public static readonly DoorState Open = ...;
```

For value types:

```csharp
public readonly record struct DoorState(string Value)
{
    public static readonly DoorState Closed = new("closed");
    public static readonly DoorState Open = new("open");
}
```

The exact discovery rules must be specified before implementation. Important questions include:

- fields only vs fields and properties;
- required accessibility;
- whether inheritance is allowed;
- whether aliases are allowed;
- whether duplicate equal values are legal;
- whether class-based values must be canonical singletons;
- deterministic member ordering across partial declarations;
- how diagnostics identify duplicate or ambiguous symbolic members.

The safest initial scope is static readonly fields declared directly on the state/trigger type, with unique member names and no runtime discovery.

## Fluent API feasibility

The Fluent API is highly feasible because Roslyn can resolve symbolic member expressions directly.

Example target syntax:

```csharp
[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    private void Configure() => FSM
        .State(DoorState.Closed)
            .On(DoorTrigger.Open).GoTo(DoorState.Open)
        .State(DoorState.Open)
            .On(DoorTrigger.Close).GoTo(DoorState.Closed);
}
```

The parser should stop asking “which enum member is this?” and instead ask “which canonical state/trigger symbol does this expression reference?”.

This suggests a useful generator abstraction independent of the public representation:

```text
ResolvedSymbolicValue
- owning type
- canonical member symbol
- display name
- generated dense ID
- representation kind
```

The existing semantic model can continue to store canonical symbolic names or can evolve toward a stronger internal symbol identity object.

## Attribute API feasibility and language limitation

The Attribute API cannot preserve identical syntax for classic smart-enum instances.

C# attribute arguments are restricted to metadata-compatible constant values. A `static readonly DoorState` object cannot be passed as an attribute argument even though the current attributes accept `object`.

Therefore this form cannot work for a class-based smart enum:

```csharp
[Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Open)]
```

A practical representation is symbolic names:

```csharp
[Transition(
    nameof(DoorState.Closed),
    nameof(DoorTrigger.Open),
    nameof(DoorState.Open))]
```

Because `[StateMachine(typeof(DoorState), typeof(DoorTrigger))]` already establishes the vocabulary types, the generator can resolve those names to canonical members at compile time.

This requires no runtime reflection and preserves compile-time diagnostics.

The Attribute API should emit a diagnostic when a string/`nameof` does not resolve to a declared canonical member.

## Compile-time validation

The current enum-specific diagnostics should evolve into representation-neutral rules.

Examples:

- state/trigger type is neither enum nor supported closed symbolic type;
- symbolic member does not belong to the declared state/trigger vocabulary;
- duplicate or ambiguous symbolic value;
- non-canonical instance expression used in Fluent configuration;
- unsupported static-property form when v1 supports fields only;
- an attribute name does not resolve to a canonical member;
- an open value type is supplied where a closed symbolic vocabulary is required.

The important invariant is unchanged:

> If a state or trigger is used in the machine definition, the generator must be able to identify it deterministically during compilation.

## HSM implications

HSM is the main architectural gate.

All hierarchy arrays should be indexed by generated `StateId`, never by the public representation. That includes:

- parent;
- depth;
- initial child;
- history mode;
- last active child;
- lowest common ancestor calculations;
- active-path traversal;
- entry/exit planning.

Conversion back to `TState` happens only when returning public API values or calling typed hooks.

This design removes the need for `Enum.ToObject` and `Convert.ToInt32` in representation-neutral HSM code.

## Typed extensions and observability

Extensions should continue to receive public `TState` and `TTrigger` values. Internal IDs are an implementation detail and should not leak into the extension contract.

The current typed extension surface and observability package use enum constraints and must be generalized as part of full feature parity.

One API detail needs explicit redesign: nullable `TState` is currently convenient for representing an optional declared target in transition metadata because `TState` is constrained to an unmanaged enum. Supporting both class and struct symbolic representations makes `TState?` representation-dependent.

A representation-neutral model should be considered, for example:

```csharp
bool HasDeclaredTarget { get; }
TState DeclaredTarget { get; }
```

or a dedicated optional value type.

This should be resolved before broadening public generic constraints.

## Performance model

The feature should not turn FastFsm into a runtime dictionary-based state machine.

The expected cost model is:

```text
Fire(public trigger)
    -> generated trigger-to-ID mapping
    -> dense ID dispatch
    -> generated transition protocol
    -> update state ID + public state
```

HSM traversal and transition resolution remain integer/switch/array based.

A boundary equality or identity check for non-enum triggers is acceptable if it is generated, bounded by the compile-time vocabulary, allocation-free, and benchmarked. General-purpose dictionaries, reflection, or runtime registration should not be introduced into the normal transition path.

The existing enum path should remain specialized when that materially improves code generation or runtime cost.

## AOT and trimming

The design is naturally compatible with trimming and Native AOT if all vocabulary discovery occurs in Roslyn and the generated code contains explicit references to the canonical state/trigger members.

The runtime must not depend on:

- `Assembly.GetTypes`;
- reflection-based static-member enumeration;
- dynamic code generation;
- runtime type scanning;
- convention-based activation discovered only at runtime.

The generated references themselves keep the required members statically visible to the compiler/linker.

## Proposed implementation sequence

### Phase 0 — behavior oracle

Before changing representation semantics, add characterization tests for:

- flat sync and async dispatch;
- HSM entry/exit, history, and active paths;
- payloads and guards;
- structural APIs;
- extensions and observability;
- existing enum values with non-trivial underlying numeric values.

The last category is important because internal IDs should be explicitly decoupled from public numeric values.

### Phase 1 — dense IDs for existing enums

Introduce an internal state/trigger identity layer while keeping the public API enum-only.

Goals:

- HSM arrays use dense generated IDs;
- transition planning is representation-neutral;
- public enum values remain unchanged;
- no measurable regression in existing enum benchmarks;
- all current tests remain valid.

This is the architectural foundation and should land independently if it is valuable on its own.

### Phase 2 — Fluent flat/sync prototype

Add one tightly-scoped non-enum vocabulary shape, preferably canonical `static readonly` members.

Scope:

- Fluent API only;
- flat synchronous machine;
- no extensions/observability initially;
- compile-time diagnostics for unsupported/open forms;
- zero-allocation transition path;
- dedicated benchmark against the enum equivalent.

This phase is the feasibility go/no-go gate.

### Phase 3 — async and HSM

Extend the representation-neutral execution model through:

- async transition paths;
- hierarchy;
- initial substates;
- shallow/deep history;
- active-path APIs.

HSM parity is the most important correctness gate.

### Phase 4 — Attribute API

Add `nameof`/symbol-name resolution for non-enum vocabularies and establish parity with Fluent configuration.

### Phase 5 — ecosystem parity

Generalize:

- typed extensions;
- observability;
- logging;
- DI;
- structural APIs;
- payload metadata;
- exception contexts and transition metadata.

### Phase 6 — AOT, trimming, packaging, benchmarks

Run clean consumer smoke tests and Native AOT/trimming verification for both enum and symbolic machines.

Add benchmark scenarios for representative vocabulary sizes, for example 4, 16, and 64 states/triggers.

## Acceptance criteria

The feature should not move from investigation to roadmap commitment until a prototype demonstrates all of the following:

- no runtime reflection for state/trigger discovery;
- deterministic generated IDs and deterministic generated source;
- compile-time rejection of unknown/non-canonical values;
- zero allocations on the normal transition path;
- HSM correctness independent of public representation and public enum numeric values;
- Native AOT and trimming smoke tests pass;
- existing enum API remains source-compatible unless a separately documented breaking change is approved;
- existing enum benchmark performance remains within an agreed regression budget;
- non-enum trigger mapping cost is measured and documented;
- Fluent and Attribute API semantics are equivalent once both are implemented.

## Risks

### Public API blast radius

Enum constraints exist throughout core contracts and satellite packages. Broadening them affects more than the generator and should be treated as a coordinated API evolution.

### Equality semantics

Class-based singleton smart enums, value-based record structs, and other symbolic patterns have different equality models. FastFsm must define supported representation contracts rather than accepting arbitrary types and hoping `EqualityComparer<T>.Default` produces suitable semantics.

### Code size

Generated value-to-ID adapters grow with vocabulary size. The implementation should measure switch/if-chain code size and consider generated lookup strategies only when they preserve AOT, determinism, and allocation goals.

### Attribute ergonomics

Non-enum Attribute API syntax cannot be identical to enum syntax because of C# attribute-argument restrictions. Documentation must make this explicit rather than hiding it behind reflection or runtime registration.

### Generic nullability

Current APIs use nullable enum values in places where “no value” is semantically meaningful. Supporting both value-type and reference-type symbolic states requires a representation-neutral optional-value design.

## Feasibility assessment

| Area | Assessment | Notes |
|---|---|---|
| Roslyn discovery | High | canonical static members are visible without reflection |
| Fluent API | High | existing expression shape maps naturally to symbolic members |
| Attribute API | High with syntax change | `nameof(...)`/symbol names can preserve compile-time validation |
| Flat runtime | High | generated dense IDs retain switch-based execution |
| HSM | Medium | public-value-as-array-index coupling must be removed |
| AOT/trimming | High | generated explicit member references are linker-friendly |
| Compile-time diagnostics | High | Roslyn has sufficient symbol information |
| Enum performance preservation | High | enum path can remain specialized |
| Non-enum performance | Medium/High | boundary mapping must be benchmarked |
| Public API blast radius | High cost | constraints span core, extensions, observability, DI, and metadata types |

Overall technical feasibility: **high**.

Full feature parity is a medium-to-large architectural change, while a Fluent/flat prototype is comparatively small once dense internal IDs exist.

## Recommendation

Keep this work as an investigation until the dense-ID foundation and a benchmarked prototype exist.

If pursued, define the feature as:

> **Support closed symbolic state and trigger types through generated dense internal identifiers.**

Do not define it as support for one specific SmartEnum library or pattern. The architecture should support a closed symbolic vocabulary independent of whether the public representation is a CLR enum, a `record struct`, or a smart-enum-style type.

The key invariant is:

```text
public representation != internal execution identity
```

Once that seam exists, smart enums become a representation problem at the generator boundary rather than a second runtime architecture.