# Extension contract redesign

**Status:** accepted and implemented in FastFsm 0.9.2.

This document records the design decision that produced the current extension API. It is historical architecture context, not a second or versioned public extension surface. Consumer documentation is in [extensions.md](../extensions.md).

## Context

The pre-0.9.2 extension API used an untyped `IStateMachineExtension` surface and a generic context abstraction. That shape was sufficient for simple notifications, but it could not represent several distinct facts required by HSMs and observability without inference or boxing. In particular, active source state, transition-owning state, configured target, resolved leaf and final machine state are not interchangeable. A single success boolean also could not describe rejection, unhandled triggers, invalid payloads, cancellation and faults.

The extension surface was still new enough to replace rather than preserve as a compatibility layer.

## Decision

FastFsm exposes one typed extension contract:

```csharp
IStateMachineExtension<TState, TTrigger>
```

The contract separates three concepts:

- `TransitionAttemptContext<TState,TTrigger>` — one `Fire` / `TryFire` attempt, including stable `InstanceId`, monotonic `AttemptId`, source state, trigger, payload and monotonic start timestamp;
- `TransitionInfo<TState>` — a matched transition, including `HandledAtState`, nullable `DeclaredTarget` and explicit `TransitionKind`;
- `TransitionResult<TState>` — the completed attempt, including `TransitionOutcome`, `FinalState`, nullable `ResolvedTarget`, matched transition, failure stage and exception.

For hierarchical machines, the contract reports the active source leaf, transition owner, declared target, resolved target and final state independently. Internal transitions have no target. External self-transitions remain external transitions.

Extension callbacks are synchronous and observational: they do not participate in cancellation and cannot change transition outcomes. The extension set and hook mask are captured once per attempt. Hook masks are checked at producer sites so unused lifecycle data is not constructed.

## Compatibility

This was a breaking API change in 0.9.2. The previous untyped extension interface and its context types were removed. FastFsm does not expose a legacy extension endpoint alongside the current typed contract.

## Consequences

- Extensions can consume state and trigger values without an untyped core API.
- Observability can be implemented as a normal extension instead of a generator-specific path.
- HSM lifecycle and failure reporting do not require consumers to reconstruct machine state heuristically.
- Extension registration remains opt-in and preserves the no-extension generated path.
- Any machine-agnostic representation belongs above the typed core contract, for example in `FastFsm.Sharp.Observability`.
