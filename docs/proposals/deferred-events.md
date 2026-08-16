# Deferred events

> **Status:** design proposal. This feature is not implemented and this document is not part of the current FastFsm API contract.

## Purpose

Deferred events allow a state machine to accept an event that should not be handled in the current state configuration and retain it for later processing after the machine leaves the configuration that defers it.

The feature is intended for workflows where dropping the event is incorrect, handling it immediately is also incorrect, and maintaining a separate application-level queue would duplicate state-machine semantics.

The historical FastFsm research for this feature was directionally useful, but several implementation details were too specific or made stronger guarantees than the design justified. This proposal keeps the useful constraints and leaves unresolved semantics explicit.

## Design constraints

A deferred-events implementation should satisfy the following constraints.

### Feature gating

Machines that do not use deferred events should not gain deferred-event fields, branches, interfaces, or constructor parameters. Generated code for an otherwise identical machine should remain unchanged when the feature is disabled.

### Bounded storage

Deferred storage should have a bounded capacity known to the generated machine. Overflow behavior must be explicit and must not change between Debug and Release builds.

Candidate policies are:

- reject the newest event;
- discard the oldest event;
- throw an exception.

The selected policy must have the same semantic result in all build configurations.

### Run-to-completion

Deferred-event processing must not introduce recursive transition execution. If retained events become eligible after a transition, processing should use an iterative mechanism with explicit ordering.

The exact point at which a retained event becomes eligible relative to newly submitted events is part of the public semantics and must be defined before implementation.

### HSM semantics

Deferral must account for the complete active state hierarchy, not only the leaf state.

Before implementation, the following precedence rules must be specified and tested against the intended UML semantics and FastFsm's existing child/parent transition resolution:

- a transition on the active leaf versus a deferral declared on that leaf;
- a transition on an ancestor versus a deferral declared on a descendant;
- a deferral on an ancestor when the leaf has no applicable transition;
- guards that evaluate to `false`;
- multiple active hierarchy levels that mention the same trigger.

The normative reference for UML state-machine semantics is the OMG UML specification: <https://www.omg.org/spec/UML/2.5.1/>.

FastFsm should not claim UML compliance for deferred events until these cases are fixed in tests and implementation.

### Payloads

The original proposal stored payloads as `object?`. That is acceptable for reference-type payloads but boxes value types and therefore cannot support a general allocation-free claim.

Possible implementation strategies are:

1. support no-payload triggers first;
2. support reference-type payloads through object storage;
3. reject value-type deferred payloads at compile time;
4. generate typed storage for value-type payloads.

The payload model should be selected before the public API is frozen. The implementation must state precisely which operations allocate and which do not.

### Allocation policy

The relevant performance target is no additional allocation on the transition hot path for supported scenarios. A heap-allocated queue created per machine instance would not satisfy a stronger "zero allocation" statement for construction.

Generated inline or fixed-capacity storage should be evaluated for .NET 10 consumers before choosing the representation.

### Sync and async behavior

Deferred events must have equivalent state semantics in synchronous and asynchronous machines.

The design must define:

- cancellation during processing of a recalled event;
- exceptions during a recalled transition;
- whether remaining retained events stay queued after failure;
- whether a recalled event can itself be deferred again;
- protection against unbounded recall/defer cycles within one public call.

## Candidate configuration API

The exact names are not frozen. Both configuration forms should express the same model.

### Attribute API

A separate state-level attribute is preferable to adding an array property to `StateAttribute`, because it keeps deferral as an optional feature and avoids expanding the core state declaration unnecessarily.

For example:

```csharp
[State(OrderState.Processing)]
[Defers(OrderTrigger.Cancel, OrderTrigger.UpdateAddress)]
private void ConfigureProcessing() { }
```

Machine-level configuration could define capacity and overflow behavior only when deferral is used.

### Fluent API

The corresponding Fluent form could be state-scoped:

```csharp
private void Configure() => FSM
    .State(OrderState.Processing)
        .Defer(OrderTrigger.Cancel)
        .Defer(OrderTrigger.UpdateAddress);
```

These names are illustrative only.

## Public execution semantics to decide

The following questions must be answered before implementation begins:

1. Does `TryFire(trigger)` return `true` when the event is accepted into deferred storage but no transition occurs?
2. When a transition makes several retained events eligible, are all of them processed before the public `Fire`/`TryFire` call returns, or only one?
3. How are retained events ordered relative to events submitted by user code from actions or callbacks?
4. Does an applicable transition always take precedence over a deferral declaration at the same active state level?
5. What is the exact child-to-parent precedence between transition lookup and deferral lookup in an HSM?
6. Can a deferred event be retained indefinitely while the machine remains in states that defer it?
7. What happens to deferred storage if the machine reaches a terminal/application-defined final condition?

These are semantic decisions, not implementation details.

## Diagnostics

Compile-time diagnostics should cover at least:

- invalid deferred trigger identifiers;
- unsupported payload representation;
- invalid capacity;
- contradictory configuration if the final semantics disallow a state from both handling and deferring the same trigger;
- unsupported combinations introduced by the selected storage model.

Diagnostic IDs should be assigned only when implementation starts; this proposal does not reserve numbers.

## Extensions and logging

Deferral should not initially require a new `IStateMachineExtension` hook unless there is a concrete use case that cannot be represented by existing transition outcome information.

Optional logging may expose deferred, recalled, and overflow events, but logging must not alter behavior or storage policy.

## Test matrix

Before release, tests should cover at least:

- flat and hierarchical machines;
- Attribute and Fluent configuration parity;
- sync and async machines;
- FIFO/order semantics;
- capacity and every supported overflow policy;
- leaf and ancestor transition/deferral precedence;
- guards returning `false`;
- re-deferral after recall;
- exceptions and cancellation;
- payload scenarios supported by the selected storage model;
- extension and logging interaction;
- generated-code absence when deferred events are unused.

## Performance requirements

Performance requirements should be measured rather than expressed as fixed nanosecond targets in the design.

Acceptance criteria should include:

- generated code is unchanged when deferred events are unused;
- no transition-path allocation in supported synchronous scenarios;
- measured overhead for an enabled machine when the current event is not deferred;
- enqueue/dequeue and recall benchmarks;
- memory cost per machine instance for each supported capacity and payload strategy.

Benchmark thresholds should be set from measurements on the implementation, not inherited from historical estimates.

## Suggested implementation sequence

1. **Semantics and model** — settle ordering, HSM precedence, `TryFire` result, capacity, and payload policy; add parser/model tests.
2. **Flat, synchronous, no-payload path** — bounded storage and iterative recall with complete behavioral tests.
3. **HSM and async integration** — hierarchy semantics, cancellation, exceptions, and re-deferral.
4. **Payloads, diagnostics, logging, and benchmarks** — add only the payload forms supported by the chosen storage representation, then measure and document the resulting behavior.

The feature should remain a proposal until the semantic questions above are resolved and encoded in tests.
