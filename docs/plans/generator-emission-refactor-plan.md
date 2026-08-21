# Generator emission layer refactor plan

Status: draft for review

Scope: `src/Generator/Generator.Core` emission architecture

Research snapshot: 2026-08-21, based on `origin/docs/extension-contract-v2`

## Objective

Make generator behavior easier to change and debug without replacing one large
class with a distributed collection of equally coupled services.

The central constraint is legitimate: state-machine generation is sequential and
ordering-sensitive. The refactor should therefore make the sequence explicit and
give each policy one owner. It should not attempt to make transition stages
independent when their semantics are not independent.

The intended outcome is:

- one transition-resolution algorithm per topology (flat and hierarchical);
- one canonical transition protocol after a transition has been selected;
- typed callback and failure policies instead of long lists of booleans;
- logging and extensions as observers of shared semantics, not alternate
  implementations of those semantics;
- local, explicit emission state instead of temporal coupling through generator
  instance fields;
- optimized paths that are proven equivalent to the canonical path.

This document deliberately orders characterization and reversible cleanup before
structural work.

## Current architecture: verified observations

At the research snapshot:

- `StateMachineCodeGenerator.cs` is approximately 2,805 lines.
- `UnifiedStateMachineGenerator.cs` is approximately 3,360 lines.
- `Generator.cs` is approximately 1,025 lines and contains both the registered
  incremental pipeline and an older, unregistered execution pipeline.
- The two main emitter classes contain approximately 102 references to
  `IsAsyncMachine`, 32 to `ExceptionHandler`, 30 to
  `ShouldGenerateLogging`, and 23 emitted `FASTFSM_SAFE_ACTIONS` branches.
- `WriteTryFireStructureHierarchical` accepts a transition-writer delegate but
  does not use it. Its signature suggests a customization boundary that does not
  exist in the hierarchical path.
- Extension-enabled machines use
  `WriteTryFireStructureWithExtensions`, a separate flat/HSM resolver, rather
  than decorating the normal resolver.
- Generator-local state such as `_smCtxCreated`,
  `_attemptResultIndex`, and `UsesEndTryFireLabel` is mutated across nested
  writer calls and callbacks.
- `GenerateHsmPermittedTriggerArrays` materializes every trigger-mask
  combination: `2^n` arrays for `n` distinct triggers.
- The generated-source golden suite currently hashes one simple
  flat/sync/non-payload/non-extension machine. Runtime tests are much broader,
  but there is no focused differential oracle for all emission routes.

These observations explain the high blast radius of small feature changes: the
same semantic event is implemented in several control-flow paths, and feature
flags choose both syntax and behavior.

## Repository history: the existing Planning layer

`Generator.Core/Planning` is not merely an unused skeleton:

1. The planner and plan emitter were introduced on 2025-08-10 in the HSM work
   (`c1d2da6` / equivalent branch commit `0873a64`).
2. Planner calls were removed on 2025-08-13 in `e532f24`, whose subject was
   `5 hsm test fail`.
3. The plan types and `GetPlanner()` remained, while later direct emitters
   continued to evolve.

The commit subject does not prove that the planner caused every failure, but the
history makes direct revival unsafe.

The current plan also differs from active semantics:

| Path | External-transition order |
| --- | --- |
| Current flat/general HSM path | Guard -> Exit -> commit state -> Entry -> Action |
| Existing flat/HSM planner | Guard -> Exit -> Action -> commit state -> Entry |
| Current HSM fast path | history -> declared-source Exit -> Action -> commit state -> declared-target Entry |

The hierarchical planner additionally calculates LCA and exit/entry chains from
the declared source state at generation time. A transition declared on an
ancestor can be handled while the runtime active state is a deeper leaf, so the
required exit path contains runtime information.

Decision for this plan: remove the abandoned implementation from live product
code during mechanical cleanup. Retain the commits as research evidence. If a
transition IR is introduced later, design it from the semantic contract rather
than treating the old `PlanStep` representation as authoritative.

## Target architecture: a narrow semantic waist

```text
Parsed StateMachineModel
        |
        v
Normalized immutable MachineSemantics
        |
        +--> API/data emitters
        |
        v
TransitionResolverEmitter (Flat or HSM)
        |
        v
Resolved transition / winner slots
        |
        v
Canonical transition protocol
Match -> Guard -> ExitPath -> CommitState -> EntryPath -> Action -> Complete
        |
        +--> CallbackInvocationEmitter
        +--> FailureFlowEmitter
        +--> StatePathEmitter
        +--> TransitionObserverEmitter (logging + extensions)
```

### Responsibilities

#### MachineSemantics

An immutable, normalized view derived once from `StateMachineModel`.

- Normalize payload mode, callback signatures, state ordinals, hierarchy facts,
  exception behavior, and enabled observers.
- Reject impossible combinations before emission.
- Do not contain mutable writer state.

This need not replace the parser model immediately. It can begin as a small
adapter for one emission slice.

#### TransitionResolverEmitter

Exactly two semantic implementations are allowed:

- flat resolution;
- hierarchical resolution, including priority, distance from active state, and
  declaration-order tie breaking.

Extensions and logging may observe resolution, but enabling either feature must
not select a different resolver.

For HSM, the resolver may populate runtime winner slots such as transition ID,
declared source, target, action ID, and internal/external kind. Runtime-derived
values such as active leaf, LCA, and resolved history target remain runtime
operands.

#### Canonical transition protocol

Owns the ordering contract:

1. match notification;
2. guard evaluation and rejection;
3. exit path;
4. state commit/history resolution;
5. entry path;
6. transition action;
7. completion outcome.

Exception and cancellation handling are structured control flow around protocol
stages. They are not simple steps that can be inserted later without regard to
scope.

#### CallbackInvocationEmitter

Owns only callback-call construction:

- callback kind;
- analyzed signature and overload;
- payload binding;
- cancellation-token binding;
- sync or async invocation;
- `ConfigureAwait`.

Replace boolean parameter bags with typed values, for example
`PayloadBinding`, `TokenBinding`, and `InvocationMode`.

This component must not decide transition outcome, state rollback/retention, or
which protocol stage follows.

#### FailureFlowEmitter

Owns:

- cancellation behavior;
- exception-handler invocation;
- stage and `stateAlreadyChanged` metadata;
- propagate/continue/fail behavior;
- local exit mechanism (`return`, structured result, or a scoped label).

Sync and async differ here as well as in invocation syntax. Therefore a single
global “async rendering profile” is insufficient.

#### StatePathEmitter

Owns:

- flat exit/entry;
- HSM runtime LCA;
- exit from active leaf;
- history recording and target resolution;
- top-down entry into the resolved leaf.

#### TransitionObserverEmitter

A no-op or composite observer for:

- logging;
- extension lifecycle hooks;
- transition outcomes.

Observers receive typed stage data. They do not own candidate selection,
state mutation, or exception policy.

#### EmissionScope

A narrow, method-local syntactic context:

- code writer;
- temporary-name allocator;
- local label/control-flow bookkeeping.

It must not carry `StateMachineModel`, all feature flags, or semantic decisions.
Those belong in immutable specifications passed explicitly to emitters. This
prevents `EmissionScope` from becoming a new god object.

## Phased implementation

Each phase is independently reviewable and revertible. Structural phases must
not begin while unrelated feature work is uncommitted in the same files.

### Phase 0 - Semantic characterization

Blast radius: tests only.

Create a focused behavior oracle that records:

- selected transition;
- guard calls and values;
- exit order;
- point of state commit;
- entry order;
- action order;
- extension/logging observer order where observable;
- final state and transition outcome;
- exception stage and cancellation behavior.

Use a risk-based matrix rather than the full Cartesian product. Required cases:

- flat sync and async;
- no payload, single payload, and representative multi-payload fallback;
- internal and external transitions;
- self-transition;
- rejected guard and multiple candidates;
- HSM transition handled on an ancestor while a deeper leaf is active;
- HSM priority, child-distance, and declaration-order tie breaking;
- initial, shallow-history, and deep-history entry;
- exception at Guard, OnExit, OnEntry, and Action;
- cancellation before and during callbacks;
- extensions disabled, empty extension set, and active observers;
- one machine that activates each fast path;
- one deliberately complex machine combining HSM, async, payload, extensions,
  and an exception handler.

Add normalized generated-source snapshots for a small set of representative
routes, but treat behavior traces and compilation as the primary oracle.

Do not start with random-model fuzzing. A fuzzer becomes useful after invariants,
valid-model generation, and the behavioral oracle are established.

Exit criteria:

- every known emission route is named and mapped to at least one test;
- canonical ordering is asserted rather than implied;
- the complex machine has an end-to-end trace;
- current fast paths are characterized before any optimization rewrite.

### Phase 1 - Mechanical cleanup

Blast radius: low; no intended generated-source changes.

Split cleanup into small commits:

1. Remove the unregistered legacy pipeline in `Generator.cs`:
   `Execute`, its source-index helper, old discovery helpers, old logging helper,
   and diagnostic descriptors used only by that pipeline.
2. Remove empty suppressed-diagnostic blocks that remain in the registered
   pipeline.
3. Remove the disconnected `Planning` implementation and `GetPlanner()`.
4. Remove identical member hiding in `UnifiedStateMachineGenerator`
   (`WriteMethodAttribute`, `GetTypeNameForUsage`, and
   `GetConfigureAwait`) after output-diff confirmation.
5. Remove redundant overrides/empty virtual calls only when call-site analysis
   proves them behaviorally inert.

Exit criteria:

- all Phase 0 tests pass;
- representative generated sources are byte-identical after normalization;
- each cleanup commit can be reverted independently;
- no cleanup commit contains an active feature change.

### Phase 2 - Localize emission mechanics

Blast radius: medium; semantics-neutral.

- Introduce method-local `EmissionScope`.
- Replace `_attemptResultIndex` with a scoped name allocator.
- Replace global label state with scoped control-flow bookkeeping.
- Replace `_smCtxCreated` with transition-local state or eliminate it by making
  match emission explicit in the protocol.
- Make hidden dependencies visible in method parameters without passing the full
  model everywhere.

Pilot on one generated method. Expand only after normalized output remains
identical.

Exit criteria:

- no generator-instance mutable field is needed to coordinate two transition
  candidates;
- writer methods can be exercised in isolation with an explicit scope;
- Phase 0 traces and snapshots remain unchanged.

### Phase 3 - Typed callback invocation

Blast radius: medium.

- Introduce an immutable callback-call specification.
- Move overload, payload, token, await, and `ConfigureAwait` selection behind
  that specification.
- Migrate OnExit first, then OnEntry, Action, and Guard.
- Keep failure control flow outside the invocation emitter.
- Remove the legacy callback path only after every caller supplies analyzed
  signature data.

Exit criteria:

- callback emitters no longer accept a long boolean parameter list;
- sync/async call syntax has one owner;
- cancellation and exception tests remain behaviorally identical.

### Phase 4 - Unify transition resolution

Blast radius: high.

- Make all flat variants use one flat resolver.
- Make all HSM variants use one HSM resolver.
- Route logging and extensions through observers of the shared resolver.
- Remove `WriteTryFireStructureWithExtensions`.
- Remove the unused transition-writer delegate from the HSM API or make the
  boundary real.

This phase changes structure before introducing a general transition IR because
duplicated resolution currently permits feature flags to change winner
semantics.

Exit criteria:

- extensions on/off cannot choose a different winner for the same model and
  runtime inputs;
- HSM ancestor, priority, depth, and source-order traces are identical;
- there is one owner for unhandled-trigger behavior per topology.

### Phase 5 - Canonical transition protocol

Blast radius: very high; timeboxed spike before production integration.

Build two disposable prototypes:

1. flat, sync, no payload, no extensions;
2. HSM, async, payload, extensions, exception handler, with an ancestor-handled
   transition from a deeper active leaf.

The prototype may use a small typed program representation, but it must support
runtime operands and nested control flow. A flat `List<PlanStep>` is not a
requirement.

Proceed only if both prototypes can express:

- the canonical stage order;
- exception scopes and continuation;
- cancellation;
- runtime LCA/history;
- observer ordering;
- optimized callback invocation without semantic duplication.

If the complex prototype requires feature-specific copies of the protocol, stop
and retain the Phase 2-4 improvements without forcing an IR.

Exit criteria:

- both prototypes match Phase 0 behavior;
- the production migration replaces one complete route at a time;
- old and new backends can be compared until the last route is migrated.

### Phase 6 - Optimization and bounded generated data

Blast radius: high but isolated behind the canonical behavior oracle.

- Re-express fast paths as optimizations of proven canonical semantics.
- Verify callback/action ordering and full HSM exit/entry paths in every fast
  path.
- Replace `2^n` permitted-trigger arrays with a bounded strategy.
- Measure generated source size, generator time, compile time, allocations, and
  runtime throughput before retaining an optimization.

Exit criteria:

- disabling an optimization changes performance, not observable behavior;
- generated data growth is bounded;
- benchmark deltas are recorded with the PR.

### Phase 7 - Parser architecture (separate project)

Do not restructure parsers concurrently with emission.

After the emission layer stabilizes, normalize Attribute and Fluent parser
outputs into the same immutable semantics. Handler-per-fluent-operation may use
the existing Rules project as a structural reference, but parser work requires a
separate plan and safety net.

## Review gates for every structural PR

Every PR must state:

- emission routes affected;
- semantic stages affected;
- expected generated-source deltas;
- behavior traces executed;
- build/test commands;
- performance impact or why it is not applicable;
- rollback boundary.

Unexpected generated-source changes require explanation. Byte equality is useful
for mechanical phases, but green hashes alone are not proof of semantic
equivalence.

## Explicit non-goals

- No inheritance tree for async/payload/HSM/extension combinations.
- No mutable orchestrator context carrying the entire model between services.
- No Roslyn `SyntaxFactory` rewrite merely to replace
  `IndentedStringBuilder`; syntax technology is not the primary coupling.
- No simultaneous parser and emitter rewrite.
- No forced IR adoption if the complex prototype demonstrates that it merely
  relocates feature branching.
- No cleanup mixed into feature commits.

## First implementation PRs after plan approval

Recommended initial sequence:

1. Phase 0A: transition-order and failure-stage trace fixtures.
2. Phase 0B: HSM ancestor-resolution and fast-path equivalence fixtures.
3. Phase 1A: remove the unregistered `Generator.cs` pipeline.
4. Phase 1B: remove disconnected `Planning`.
5. Phase 1C: remove duplicate/hiding helpers.
6. Phase 2 pilot: method-local emission scope for one TryFire route.

No production architecture should be selected solely from the simple flat
prototype. The complex HSM prototype is the go/no-go gate.
