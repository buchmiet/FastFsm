# State-machine semantics conformance audit and specification

Status: draft for review

Scope: behavioral semantics of the FastFsm feature set implemented as of `main` at `adfec0c053289b39610bf9ec25103c226dbfb559`.

This document is a prerequisite for resuming the generator-emission refactor proposed in PR #15. It does not change runtime or generated behavior. Its purpose is to decide which behavior the refactor must preserve and which existing behavior is an accidental divergence that must be corrected deliberately rather than frozen by characterization tests.

## Position

FastFsm should not invent state-machine semantics where a mature standard already defines the corresponding concept.

The semantic baseline is:

1. **UML 2.5.1 StateMachines** as the primary model for hierarchical state-machine concepts.
2. **OMG PSSM 1.0** as the precise executable-semantics companion to UML where UML prose leaves operational details difficult to test directly.
3. **W3C SCXML 1.0** as an independent executable cross-check for concepts shared with FastFsm: enabled transitions, hierarchical selection, guards, exit/effect/entry ordering, compound-state entry, and history.

Official references:

- UML 2.5.1: <https://www.omg.org/spec/UML/2.5.1>
- UML 2.5.1 normative PDF: <https://www.omg.org/spec/UML/2.5.1/PDF>
- PSSM 1.0: <https://www.omg.org/spec/PSSM/1.0>
- SCXML 1.0: <https://www.w3.org/TR/scxml/>

FastFsm is not claiming full UML, PSSM, or SCXML conformance. It implements a deliberately smaller feature set. The rule is narrower:

> When a FastFsm feature has a direct standard state-machine meaning, FastFsm follows that meaning unless a documented FastFsm extension explicitly says otherwise.

A deviation must therefore be one of:

- **intentional extension** — deliberate, specified, tested and named as FastFsm-specific;
- **unsupported standard feature** — outside the supported subset and not emulated accidentally;
- **accidental divergence** — current behavior that conflicts with the semantic baseline and should not become normative merely because tests happen to encode it.

## Supported semantic subset

This audit covers the constructs FastFsm already exposes:

- flat states and triggered transitions;
- guard conditions;
- multiple candidate transitions for one `(source, trigger)`;
- explicit FastFsm `Priority`;
- hierarchical parent/child states;
- ancestor-owned transitions;
- external transitions;
- targetless action-only internal transitions;
- external self-transitions;
- entry and exit callbacks;
- transition actions;
- composite initial entry;
- shallow and deep history;
- payload binding;
- synchronous and asynchronous callbacks;
- cancellation and exception policy;
- logging and extension observation.

The following standard features are not made part of the contract by this document merely because UML or SCXML define them:

- orthogonal/parallel regions;
- event queues and full UML run-to-completion macrostep semantics;
- eventless/completion transitions;
- deferred events;
- `doActivity`;
- choice, junction, fork, join, terminate and other pseudostates;
- submachine states;
- multiple simultaneously active atomic states.

Those require separate feature proposals.

## Terminology

The following terms are normative for FastFsm going forward.

### Candidate transition

A transition is a **candidate** for a firing attempt when:

- its trigger matches the attempted trigger; and
- its source state is eligible for the current configuration:
  - the active state for a flat machine;
  - the active leaf or one of its ancestors for an HSM.

Candidate does **not** imply that the guard succeeded.

### Enabled transition

A candidate transition is **enabled** when:

- it has no guard; or
- its guard evaluates to `true`.

A guard that returns `false` makes that candidate not enabled. It does not, by itself, terminate resolution.

This follows UML firing semantics and SCXML section 3.13, where condition truth is part of transition enabledness.

### Selected transition

The **selected transition** is the enabled transition chosen by the deterministic FastFsm selection rules below.

There is at most one selected transition in the currently supported FastFsm subset because FastFsm does not support orthogonal regions.

### Transition effect

FastFsm currently calls the transition behavior an `Action`. Semantically this corresponds to the UML transition `effect` and to executable content on an SCXML transition.

The API name may remain `Action`; this specification uses **transition effect/action** when discussing ordering.

## Normative transition-selection semantics

### S1 — Guards participate in enabledness

For every candidate at the source level currently being considered, guard evaluation occurs before transition selection is finalized.

If a guard returns `false`, resolution continues to the next candidate according to the ordering rules.

Example:

```text
A --Go [false]--> B
A --Go [true] ---> C
```

`Go` selects `A -> C`.

FastFsm must not implement this as “select `A -> B`, then reject the whole firing attempt because its guard returned false”.

### S2 — HSM descendant precedence is structural

For an HSM, resolution searches the active hierarchy from the active leaf outward through its ancestors.

An enabled transition owned by a descendant has higher semantic priority than a conflicting enabled transition owned by an ancestor.

Example:

```text
Parent: Go -> P
Child:  Go -> C
```

When `Child` is active, the enabled `Child` transition wins.

If no candidate owned by `Child` is enabled, search continues at `Parent`.

Example:

```text
Parent: Go [true]  -> P
Child:  Go [false] -> C
```

The `Parent` transition wins.

This is the UML implicit firing priority and is also the operational order used by SCXML: atomic state first, then ancestors.

### S3 — FastFsm `Priority` is an intra-source deterministic extension

Explicit `Priority` is a FastFsm extension. It must not override UML hierarchical descendant precedence.

For candidates owned by the **same source state**:

1. evaluate candidates in descending explicit `Priority`;
2. candidates whose guards are false are skipped;
3. the first enabled priority group wins;
4. declaration order is the deterministic tie-breaker when enabled candidates have the same explicit priority.

Equivalent formulation:

```text
source depth first (leaf -> ancestors)
    then Priority descending within that source
        then declaration order
            with guard truth deciding enabledness
```

A parent transition with `Priority = 100` does not preempt an enabled child transition with `Priority = 1`.

If cross-hierarchy numeric priority is ever desired, it requires a separately named feature because it changes standard HSM semantics.

### S4 — Flat-machine selection uses the same enabledness rule

A flat machine has only one eligible source state, so resolution reduces to:

```text
matching candidates
    -> Priority descending
    -> declaration order
    -> first enabled transition
```

Guard false therefore falls through to a later candidate in flat machines as well as HSMs.

### S5 — Guard exceptions and cancellation are not guard-false

An exception or cancellation raised while evaluating a guard is not equivalent to a guard returning `false` unless an explicit FastFsm policy says so.

In particular, enabling extensions, logging, payload support or another generation feature must not silently convert a guard exception/cancellation into rejection and continue or terminate differently.

Exception/cancellation policy is FastFsm-specific, but candidate selection must receive the same semantic result regardless of generated variant.

## Normative transition-execution semantics

For a selected external transition FastFsm adopts the standard ordering:

```text
Guard resolution
-> Exit state(s)
-> Transition effect/action
-> Commit/enter target configuration
-> Entry state(s)
-> Complete attempt
```

SCXML section 3.5.2 and 3.13 explicitly place transition executable content after all applicable `onexit` handlers and before applicable `onentry` handlers. UML models the same semantic distinction as state exit behavior, transition effect and state entry behavior.

### S6 — Exit path precedes the transition effect

For a flat external transition, the source `OnExit` runs before the transition action.

For an HSM external transition, applicable states exit from the active leaf outward according to the source/target boundary before the transition action runs.

### S7 — Transition effect precedes target entry callbacks

The transition `Action` runs before target `OnEntry` callbacks.

Current FastFsm behavior that emits `OnEntry` before `Action` is not normative and must be treated as an accidental divergence.

### S8 — `CurrentState` is the last stable configuration until target commit

UML/PSSM can model a transition while the source configuration has been exited and the target configuration is not yet fully entered. FastFsm exposes a single `CurrentState` value and has no public “between configurations” state.

FastFsm therefore defines `CurrentState` as the **last committed stable leaf** during transition execution:

- during source exit callbacks, `CurrentState` remains the source leaf;
- during the transition effect/action, `CurrentState` remains the source leaf;
- target/history resolution may be computed without mutating `CurrentState`;
- immediately before target entry begins, FastFsm commits the resolved target leaf;
- target `OnEntry` callbacks observe the committed target leaf.

This is a FastFsm observability rule, not a claim that UML defines a `CurrentState` property this way.

A consequence is that an action/effect failure before target commit leaves `FinalState` at the source leaf. An entry failure after commit leaves `FinalState` at the target leaf.

This consequence is intentionally review-visible because it differs from the current extension outcome tests, which presently assume that action failure can occur after the target has already been assigned.

### S9 — External self-transition exits and re-enters

An external self-transition is not an internal transition merely because source and target enum values are equal.

It performs the applicable exit path and entry path.

For an HSM transition owned by an ancestor and targeting that ancestor, the active subtree is exited and then re-entered according to normal external-transition semantics.

Transition kind must come from the model, not from `source == target`.

### S10 — FastFsm internal transitions are a restricted targetless subset

FastFsm's existing `InternalTransition` construct is defined as targetless, action-only behavior that does not change the active configuration and does not invoke state exit/entry callbacks.

This is compatible with the no-state-change behavior expected of targetless/internal state-machine transitions, but FastFsm does **not** claim to implement every UML/SCXML form named “internal transition”. In particular, SCXML's `type="internal"` source-to-descendant semantics are not introduced by this document.

Internal transition ordering is therefore:

```text
Guard resolution
-> Action
-> Complete attempt
```

with no state commit and no state lifecycle notifications.

## Hierarchy, initial entry and history

### S11 — Active leaf and transition owner are distinct

For an HSM attempt:

- the active source is the current leaf;
- the selected transition owner is the state on which the transition is declared;
- the declared target is the target named by the model;
- the resolved target is the leaf reached after composite initial/history resolution.

These values must not be inferred from one another.

This matches the direction already established by Extension Contract v2.

### S12 — External HSM exit/entry paths use hierarchy semantics, not generated-path shortcuts

For an external transition:

- exits start at the active leaf and proceed outward to the transition boundary;
- entries proceed from the boundary toward the resolved target leaf;
- ancestor-owned transitions use the declared owner when determining the transition domain/boundary;
- runtime active-leaf information must participate in exit-path construction.

A generator optimization is valid only if it is observationally equivalent to this path.

### S13 — Composite targets resolve before entry callbacks

A transition targeting a composite state resolves the actual entry configuration through initial-state or history semantics before target entry callbacks execute.

The API should preserve both facts where observability needs them:

- `DeclaredTarget` — the configured composite or leaf;
- `ResolvedTarget` — the leaf actually entered.

### S14 — Initial, shallow-history and deep-history behavior follow the supported UML/SCXML subset

For the hierarchy shape FastFsm supports:

- entering a composite with no applicable stored history descends through its configured initial child path;
- shallow history restores the last active direct child, then follows normal initial descent as required;
- deep history restores the remembered descendant leaf/path;
- history is recorded from the active configuration before an external transition exits that configuration.

Orthogonal-region history is not covered because orthogonal regions are not supported.

## Attempt outcomes and observability

UML and SCXML do not define FastFsm's public `TryFire`, `TransitionOutcome`, exception policy, cancellation tokens or extension hooks. These are FastFsm extensions. They must nevertheless report the standard transition semantics truthfully.

### S15 — Observers cannot alter resolution or execution

For identical model and runtime inputs, enabling or disabling:

- extensions;
- logging;
- observability;
- generated diagnostic helpers

must not change:

- which candidates are evaluated;
- guard evaluation order;
- which transition is selected;
- exit/effect/entry order;
- final state;
- exception/cancellation outcome.

Generated variants may differ syntactically and in instrumentation only.

### S16 — Candidate guard observation may occur more than once per attempt

Once standard enabledness is adopted, one firing attempt may evaluate several candidates before one becomes enabled or resolution reaches an ancestor.

Therefore extension guard hooks must be capable of observing multiple candidate evaluations in deterministic order.

A callback that means “a transition has been selected” must occur only after enabled-transition selection succeeds.

### S17 — `MatchedTransition`/`OnTransitionMatched` require contract cleanup

The current Extension Contract v2 vocabulary assumes a single syntactically matched transition before guard evaluation. That assumption conflicts with S1-S4.

The target semantics are:

- guard hooks observe **candidate** transitions;
- there is no selected transition until an enabled candidate wins;
- the result's transition reference represents the selected transition, not a candidate whose guard was false;
- when candidates existed but all guards were false, the result contains no selected transition.

Before implementation, review must choose one of two API migrations:

1. rename `MatchedTransition` / `OnTransitionMatched` to `SelectedTransition` / `OnTransitionSelected`; or
2. retain the names but normatively redefine them to mean the selected enabled transition and move the callback after successful guard resolution.

The first option is clearer if the public surface can still accept a breaking correction.

### S18 — `GuardRejected` is a diagnostic FastFsm outcome, not transition selection

FastFsm may retain `TransitionOutcome.GuardRejected` because it is diagnostically useful to distinguish:

- no candidate matched the trigger/source scope; from
- candidates matched, but none were enabled because guards returned false.

Normatively:

```text
UnhandledTrigger:
    no trigger/source candidate existed

GuardRejected:
    one or more candidates existed,
    every candidate considered by resolution was disabled by guard false,
    no transition was selected
```

A guard exception or cancellation does not produce `GuardRejected` merely because no transition was ultimately selected.

### S19 — Payload validation is a FastFsm precondition

Payload typing is outside UML/SCXML state-machine semantics.

FastFsm defines invalid payload as a pre-resolution failure:

- no guard is evaluated;
- no transition is selected;
- no exit/effect/entry behavior executes;
- result is `InvalidPayload` where the extensible outcome contract is active.

Payload support must not otherwise alter candidate selection.

### S20 — Async execution preserves the same semantic protocol

Async is an invocation mechanism, not a different state-machine model.

Sync and async variants must have the same:

- candidate order;
- enabledness rules;
- selected transition;
- state lifecycle order;
- effect/entry ordering;
- hierarchy semantics.

Cancellation and scheduling details are FastFsm-specific additions around that common protocol.

## Conformance audit of current `main`

The following audit is against `adfec0c053289b39610bf9ec25103c226dbfb559` and names implementation methods rather than relying on unstable line numbers.

| Area | Baseline | Current FastFsm | Classification | Required direction |
| --- | --- | --- | --- | --- |
| Guard enabledness | guard truth is part of enabledness | flat paths choose the first priority-ordered candidate and stop; guard false ends that candidate path | **accidental divergence** | guard false must continue candidate search |
| Multiple flat candidates | choose an enabled transition deterministically | `WriteTryFireStructureFlat` and extensible flat resolver break after the first candidate | **accidental divergence** | one flat resolver; evaluate until enabled candidate |
| HSM descendant precedence | enabled descendant transition preempts ancestor | non-extension HSM ranks explicit `Priority` before depth; sufficiently high parent priority can beat child | **accidental divergence** | hierarchy depth/source ownership before numeric priority |
| Extensible HSM resolution | same semantics as plain HSM | `WriteTryFireStructureWithExtensions` walks leaf-to-parent but selects `.OrderByDescending(Priority).First()` at each source before guard outcome is known | **accidental divergence** | one shared HSM resolver |
| Explicit `Priority` | not a UML numeric cross-hierarchy mechanism | currently participates globally in non-extension HSM winner selection | **intentional feature with incorrect scope** | restrict to same-source candidate ordering |
| Declaration order | deterministic implementation choice is acceptable | used as tie-breaker in several paths | **intentional extension** | keep after source depth and explicit priority |
| Exit before effect | standard | exit callback precedes action | **conformant in examined paths** | preserve |
| Effect before entry | standard | generated paths currently commit state and run `OnEntry` before `Action` | **accidental divergence** | change to exit -> action -> commit/entry |
| External self-transition | exits/re-enters | HSM extension tests explicitly require subtree exit/re-entry | **conformant subset** | preserve through shared protocol |
| Targetless internal transition | no state exit/entry | FastFsm internal action path leaves state unchanged and omits lifecycle | **conformant restricted subset** | document restricted meaning |
| HSM active leaf vs owner | distinct concepts | Extension Contract v2 carries `SourceState` and `HandledAtState` separately | **conformant direction** | preserve |
| Composite declared vs resolved target | distinct | Extension Contract v2 carries both | **conformant direction** | preserve |
| Initial composite descent | recursive initial entry | implemented and runtime-tested | **conformant supported subset** | preserve |
| Shallow/deep history | restore recorded configuration according to history mode | implemented and runtime-tested for the single-region hierarchy FastFsm supports | **conformant supported subset** | preserve; add conformance traces |
| Observer invariance | observers are not state-machine semantics | extension-enabled machines currently have a separate resolver | **architectural violation with semantic consequences** | instrumentation must decorate shared semantics |
| Guard result terminology | false candidate is not a selected transition | `OnTransitionMatched` / `MatchedTransition` currently materialize before guard and `GuardRejected` carries it | **accidental vocabulary divergence** | separate candidate observation from selected transition |
| Action-failure final state | standard does not define FastFsm exceptions | current state is committed before action, so action failure can report target as final | **FastFsm-specific behavior coupled to nonstandard ordering** | review S8 migration explicitly |
| Payloads | outside standard | precondition + callback binding | **intentional extension** | ensure selection parity |
| Async/cancellation | outside standard | separate generated control-flow paths | **intentional extension** | preserve common semantic protocol |
| Orthogonal regions | UML/SCXML feature | unsupported | **unsupported** | no accidental partial semantics |
| Eventless/completion transitions | UML/SCXML feature | unsupported | **unsupported** | separate proposal if added |
| Full RTC event queue | UML/PSSM concept | direct `Fire`/`TryFire` API rather than full event-queue model | **intentional subset** | do not claim full UML RTC conformance |

## Verified current-code evidence

### First-candidate termination in flat resolution

`StateMachineCodeGenerator.WriteTryFireStructureFlat` sorts candidates by explicit priority and declaration order, then calls `writeTransitionLogic` for only the first candidate in each trigger group and immediately `break`s.

`UnifiedStateMachineGenerator.WriteTryFireStructureWithExtensions` repeats the same first-candidate structure for extensible flat machines.

Because guard evaluation lives inside the transition writer, a false guard cannot return control to the resolver to try the next candidate.

### Divergent HSM resolvers

Non-extension HSM generation in `StateMachineCodeGenerator.WriteTryFireStructureHierarchical` evaluates candidates while tracking winner slots. Its comparison currently prefers:

```text
Priority
-> depth from current
-> declaration order
```

so explicit numeric priority can override the UML descendant-source priority.

Extensible HSM generation does not use that resolver. `UnifiedStateMachineGenerator.WriteTryFireStructureWithExtensions` walks the active leaf toward ancestors, but for each `(source, trigger)` chooses one `.OrderByDescending(Priority).First()` transition and immediately emits it.

The two generated variants therefore do not share a single semantic selection algorithm.

### Current effect ordering

The active emitters assign/resolve the target state, invoke target `OnEntry`, and only then invoke the transition `Action` in important flat/payload/extension paths.

That ordering is observable both to user callbacks and to Extension Contract v2 failure reporting. It conflicts with the standard exit -> transition effect -> entry ordering and must not be silently preserved by the refactor characterization oracle.

## Required decisions before implementation

This proposal recommends accepting S1-S20 as the semantic target. Review should pay particular attention to four breaking points.

### D1 — Accept standard guard fallthrough

Recommended: **yes**.

A false guard disables one candidate; it does not reject the attempt while later candidates or ancestors could be enabled.

This changes existing behavior for machines that define several transitions for the same trigger and rely on first-candidate rejection.

### D2 — Restrict numeric `Priority` to the same source state

Recommended: **yes**.

HSM descendant precedence remains structural and standard. Numeric `Priority` resolves alternatives declared on that same source state.

This changes existing plain-HSM behavior when a higher-priority ancestor currently beats an enabled descendant.

### D3 — Move transition `Action` between exit and entry

Recommended: **yes**.

Adopt:

```text
exit -> action/effect -> target commit -> entry
```

rather than the current:

```text
exit -> target commit -> entry -> action
```

This is a visible behavioral correction. Code that reads `CurrentState` inside `Action`, and exception tests that depend on state having already changed before action, will change.

### D4 — Correct extension selection vocabulary

Recommended: prefer explicit `SelectedTransition` / `OnTransitionSelected` if compatibility permits; otherwise redefine the existing `Matched*` vocabulary precisely and document the semantic break.

The guard hooks already carry candidate transition information, so the extension contract does not need to pretend a guard-false candidate was selected.

## Compatibility impact

Adopting this specification can change behavior only in cases that currently depend on one of the identified divergences. The highest-risk shapes are:

1. several transitions with the same source and trigger where an earlier/higher-priority guard is false and a later guard is true;
2. a child and ancestor both defining enabled transitions for the same trigger while the ancestor has a larger FastFsm numeric priority;
3. transition actions that assume `OnEntry` has already run;
4. transition actions that inspect `CurrentState` and expect the target;
5. exception/cancellation policy tests that assume action failures occur after target commit;
6. extensions that assume `OnTransitionMatched` occurs before guard evaluation or that `GuardRejected` owns one selected/matched transition.

These are semantic changes and must not be hidden inside a mechanical refactor PR.

## Test oracle required before PR #15 resumes

The old Phase 0 principle from PR #15 remains correct, but the oracle must assert **approved semantics**, not blindly freeze current output.

### Standard-derived selection cases

At minimum:

- flat: first guard false, second true;
- flat: multiple true guards with different `Priority`;
- flat: equal priority resolved by declaration order;
- HSM: child enabled, parent enabled -> child;
- HSM: child guard false, parent enabled -> parent;
- HSM: child lower numeric priority, parent higher numeric priority -> child;
- HSM: multiple candidates on child, guard fallthrough remains on child before bubbling to parent;
- no enabled candidate after all false guards -> `GuardRejected`, no selected transition;
- no candidate at any source -> `UnhandledTrigger`.

### Standard-derived execution cases

Trace exact ordering for flat and HSM:

```text
Guard
Exit lifecycle/callbacks
Action/effect
Target commit
Entry lifecycle/callbacks
Completion
```

Include:

- external sibling transition;
- external self-transition;
- ancestor-owned transition from a deeper active leaf;
- composite target initial descent;
- shallow history restore;
- deep history restore;
- targetless internal transition.

### Variant parity matrix

For the semantic cases above, compare at least:

- extensions off / on;
- logging off / on where generator route changes;
- sync / async;
- no payload / representative payload;
- exception handler absent / present for failure-stage cases.

The assertion is semantic equality, not generated-source text equality.

## Generator-refactor consequences

Acceptance of this specification changes the target of PR #15 in several important ways.

### Characterization must distinguish norm from bug

Phase 0 should have two classes of tests:

- **conformance tests** — expected standard/approved FastFsm behavior, allowed to fail on current `main` and become the target of an explicit semantic-correction PR;
- **compatibility characterization tests** — FastFsm-specific behavior that is intentional and must remain unchanged through structural refactors.

Current divergent behavior must not be converted into golden output merely to make a refactor easier.

### One resolver is required per topology

All generation variants must call the same semantic resolver for a topology:

```text
Flat resolver
HSM resolver
```

Extensions and logging observe those resolvers; they do not select alternate ones.

### Resolution and execution are separate seams

The architecture should distinguish:

```text
candidate discovery
-> guard-enabledness evaluation
-> winner selection
-> transition execution
```

Guard evaluation is part of resolution because it determines whether a candidate is enabled.

The post-selection executor then owns:

```text
exit path
-> effect/action
-> target resolution/commit
-> entry path
-> completion
```

This corrects the earlier PR #15 sketch that placed `Guard` inside a protocol that started only after a single transition had already been selected.

### Old `Planning` remains non-authoritative

The abandoned `Generator.Core/Planning` implementation must not be revived as the semantic source of truth. Its transition order differs from active code, and neither old planning nor active code is authoritative where this specification marks an accidental divergence.

A future IR/planner, if any, must be derived from this semantic contract and verified against the conformance oracle.

## Proposed implementation sequence after approval

No semantic correction should be mixed into a broad structural rewrite.

Recommended order:

1. **Approve this semantic specification.**
2. **Add conformance tests** for S1-S20 that expose known divergences explicitly.
3. **Correct transition selection** in a focused PR: guard fallthrough, descendant precedence, scoped numeric priority, plain/extensible parity.
4. **Correct execution ordering** in a separate focused PR: exit -> action -> commit/entry, including exception/final-state contract updates.
5. **Correct extension vocabulary/ordering** if D4 requires API changes.
6. **Reopen/revise PR #15** against the corrected semantic baseline.
7. Perform mechanical cleanup and emission refactoring only after the semantic test oracle is green.

This ordering separates product behavior changes from architecture changes, so a review can answer independently:

- “Is this the correct state-machine behavior?”
- “Does the refactor preserve that behavior?”

## Review checklist

Please review the proposal primarily for semantic correctness, not implementation convenience.

- [ ] UML/PSSM is the correct primary reference model for FastFsm's HSM semantics.
- [ ] SCXML is acceptable as an executable cross-check, without claiming SCXML compatibility.
- [ ] Guard false means candidate disabled and resolution continues.
- [ ] Enabled descendant transitions outrank ancestor transitions.
- [ ] Numeric `Priority` is restricted to candidates owned by the same source state.
- [ ] Declaration order is an acceptable deterministic tie-breaker.
- [ ] Transition action/effect runs after exits and before target entry.
- [ ] `CurrentState` last-stable-configuration rule is acceptable during exit/effect.
- [ ] Targetless FastFsm internal transitions remain action-only with no exit/entry.
- [ ] Composite/initial/history semantics are correctly scoped to the supported single-active-path HSM model.
- [ ] `GuardRejected` remains a diagnostic outcome but owns no selected transition.
- [ ] Extension candidate observation and selected-transition observation are separated.
- [ ] Exceptions, cancellation, payloads and async remain FastFsm-specific policies layered around one common transition protocol.
- [ ] PR #15 must be revised after this specification is accepted rather than revived unchanged.
