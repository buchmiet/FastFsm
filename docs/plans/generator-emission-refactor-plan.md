# Generator Emission Layer — Phased Refactor Plan

Status: draft. Research basis: review of `src/Generator/Generator.Core` (2026-08-21).
This document defines the phase order for restructuring the state machine emission
layer. Rule #1: **semantics-neutral phases first, deep changes last**. Each phase is
an independent PR with its own verification gate.

## Problem statement (measured)

- `UnifiedStateMachineGenerator.cs` — ~3360 lines; base `StateMachineCodeGenerator.cs` — ~2805 lines.
- Single transition execution logic exists in **~7 parallel implementations**
  (flat-nonpayload base, SyncCore, WithExtensions, async, PayloadAsync,
  PayloadSyncDirect, SyncDirect + 2 fast paths).
- Variant dimensions are scattered as conditionals, not localized:
  `IsAsyncMachine` ~102 refs, `ExtensionsOn` ~30, `HierarchyEnabled` ~39,
  `ExceptionHandler` ~15, `#if FASTFSM_SAFE_ACTIONS` ~18 emission sites.
- Hidden mutable emitter state: `_smCtxCreated` (mutated inside lambdas passed to
  the dispatcher), `_attemptResultIndex`, `UsesEndTryFireLabel`, `AddedUsings`,
  plus string-ly-typed coupling via generated variable names (`SuccessVar`,
  `GuardResultVar`, `PayloadMapField` in `Strings.cs`).
- `Planning/` (ITransitionPlanner / TransitionPlan / PlanStep) is **dead code**:
  `GetPlanner()` has no call sites. This is a half-finished IR layer — revive or
  delete, decision deferred to Phase 4.
- Base class has exactly one abstract member; inheritance buys nothing today.

## Non-goals / explicit "do not do"

- No emitter inheritance hierarchies (async-extends-payload-...) — multiplies classes.
- No orchestrator passing a mutable "context" object between independent services;
  that recreates the god-context problem.
- No simultaneous restructuring of Parsers and Emission — one epicenter at a time.
- No reformatting / style-only churn mixed with structural changes.

## Verification gate (applies to every phase)

- `dotnet build` of `Generator.sln`-relevant projects green.
- Existing generator test suites green (`Generator.Tests`, `Tests.SourceGenerators`).
- For emission-affecting phases: byte-for-byte equality of generated sources on the
  golden matrix (see Phase 0), modulo explicitly whitelisted deltas.

---

## Phase 0 — Safety net (before any structural move)

**Blast radius: tests only.**

- Extend golden snapshots to the full matrix:
  `{flat, hsm} x {sync, async} x {payload, none, multi} x {extensions, none} x {exception-handler, none}`.
- Add a small model fuzzer: random valid state graphs -> run generator -> record
  output hashes; used to pre/post-diff phases with no expected-behavior change.

**Exit criteria:** matrix goldens checked in and enforced in CI; fuzzer harness runs in tests.

## Phase 1 — Mechanical housekeeping (THIS PR starts it)

**Blast radius: minimal, semantics-neutral, reviewable line-by-line.**

1. Dead/duplicated helpers:
   - [x] Deduplicate `MakeSafeMemberSuffix` (identical implementations in base and
     Unified; base's zombie-named `..._MemberSuffixWrapper` renamed).
   - [ ] Remove empty `// Suppressed ... diagnostic` ceremony blocks in `Generator.cs`.
   - [ ] Remove leftover dangling comments/`new`-shadowing notes where provably dead.
2. Decide fate of unused `FSM99x` diagnostic descriptors (keep-reportable vs delete).
3. Centralize `#if DEBUG ...` emission comments behind one helper.

**Exit criteria:** build + tests green; zero changes in golden outputs.

**Known issues documented, NOT fixed here** (to keep blast radius small):
- `GenerateHsmPermittedTriggerArrays` emits `2^n` arrays for n distinct triggers
  (potential blow-up of generated code). Tracked for Phase 5.
- `protected new` shadowing of `GetTypeNameForUsage` / `GetConfigureAwait` in
  Unified — flag for Phase 2 audit.

## Phase 2 — EmissionContext extraction

**Blast radius: medium; mechanical; semantics-neutral by construction.**

- Introduce an explicit `EmissionContext` carrying: `Sb`, extension-context flags
  (`_smCtxCreated`), temp-name allocator (`_attemptResultIndex`), label bookkeeping
  (`UsesEndTryFireLabel`), `AddedUsings`, and a **generated-variable registry**
  replacing string-ly-typed coupling (`SuccessVar`, `GuardResultVar`, ...).
- Thread the context through emitter methods instead of mutating class fields.
- Kill lambda side-effects: dispatcher callbacks must not reset shared state.

**Exit criteria:** goldens byte-identical; emitter methods unit-testable in isolation
(give context, assert emitted text).

## Phase 3 — Async/sync emission profile

**Blast radius: medium-high; the first structural win.**

- Observation: sync vs async variants of the same transition logic differ almost
  exclusively in callback invocation rendering (`await`/`ValueTask` wrapping,
  `ConfigureAwait`, cancellation policy), not in structure.
- Introduce `IEmissionProfile { string RenderInvocation(...) }` with Sync/Async
  profiles; collapse sync/async emission twins onto shared structure emitters.
- Pilot first on ONE slice (e.g. OnExit emission) before generalizing.

**Exit criteria:** pilot slice goldens byte-identical; `IsAsyncMachine` conditional
count measurably reduced.

## Phase 4 — Decision point: revive or delete `Planning/`

**Blast radius: high; requires Phase 2+3 as foundation.**

- Experiment (timeboxed): build a `PlanInterpreter` that emits the simplest variant
  (flat / sync / no payload / no extensions) purely from `TransitionPlan`, and diff
  against current output on goldens.
- If feasible: extend `PlanStepKind` (RecordHistory, exception-policy wrapping,
  extension hooks, payload binding) and express features as **passes over an
  immutable plan** (nanopass style):
  `Model -> Planner -> Plan -> HistoryPass -> ExceptionPolicyPass -> LoggingPass -> ExtensionPass -> Emitter(profile)`.
- If not feasible (ordering constraints not expressible in plan data):
  delete `Planning/` and document why.
- Fast paths (`IsPureBasicFastPath`, HSM guardless) stay as model-pattern detectors
  with own emitters, or become plan-level optimizations — decided in this phase.

**Exit criteria:** pilot variant generated byte-identical from plan; or documented
rejection + dead-code removal.

## Phase 5 — HSM winner selection + runtime-accreted data

- Separate "candidate evaluation" (priority/depth/decl-order, guards) from
  "execution" explicitly in generated code structure.
- Fix the `2^n` permitted-trigger mask arrays (runtime-bound lookup or bounded
  strategy) — guarded by its own goldens.

## Phase 6 — Parsers (separate epicenter, later)

- `FluentParser.cs` (~109 KB, ~49 methods) and `StateMachineParser.cs` (~107 KB):
  map fluent-call kinds to per-call handlers, modeled on the already well-structured
  `Generator.Rules` (rule + context per file).

---

## Working agreement for phase PRs

- One phase (or one checklist item) per PR; base branch decided per phase.
- Every PR states: blast radius, verification performed, golden-diff result.
- No phase merges with red CI or unexplained golden diffs.
