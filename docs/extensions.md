# Extensions

Implement `IStateMachineExtension<TState, TTrigger>` to observe transition processing. Extension callbacks are synchronous, short, and non-blocking: they do not participate in cancellation and cannot change the outcome of a transition.

Extension support is opt-in. Set `GenerateExtensibleVersion` explicitly:

```csharp
[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
```

Pass extension instances to the generated machine constructor. Non-logging machines do not have a `logger` parameter:

```csharp
var machine = new MyMachine(
    initialState,
    extensions: [new AuditExtension()]);
```

## Hook interface

```csharp
public interface IStateMachineExtension<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    ExtensionHooks Hooks => ExtensionHooks.Transitions;

    void OnAttemptStarting(in TransitionAttemptContext<TState, TTrigger> attempt);
    void OnTransitionMatched(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionInfo<TState> matched);
    void OnAttemptCompleted(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionResult<TState> result);
    void OnGuardEvaluating(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionInfo<TState> candidate, string guardName);
    void OnGuardEvaluated(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionInfo<TState> candidate, string guardName, bool result);
    void OnStateExiting(in TransitionAttemptContext<TState, TTrigger> attempt, TState state);
    void OnStateEntered(in TransitionAttemptContext<TState, TTrigger> attempt, TState state);
    void OnCallbackExecuting(in TransitionAttemptContext<TState, TTrigger> attempt, TransitionStage stage, string callbackName);
    void OnCallbackFaulted(in TransitionAttemptContext<TState, TTrigger> attempt, TransitionStage stage, string callbackName, Exception exception);
    void OnMachineStarted(Guid instanceId, TState initialState);
}
```

The methods have default empty implementations. Implement only the hooks you need, and declare them on `Hooks`. **The mask is authoritative:** an override whose flag is not set is never called.

| Flag | Hooks |
|---|---|
| `Transitions` | attempt starting, transition matched, attempt completed |
| `Guards` | guard evaluating / evaluated |
| `States` | state exiting / entered |
| `Callbacks` | callback executing / faulted |
| `Lifecycle` | machine started |
| `Hierarchy` | reserved; HSM is observed through `States` plus the attempt/result fields below |

## Call order

```
OnAttemptStarting
OnTransitionMatched          // omitted when nothing matched
OnGuardEvaluating / Evaluated
OnStateExiting               // leaf → ancestor for HSM; none for internal
OnCallbackExecuting(OnExit)
OnStateEntered               // ancestor → leaf for HSM, after the state assignment
OnCallbackExecuting(OnEntry)
OnCallbackExecuting(Action)
OnAttemptCompleted
```

`GuardRejected` means the guard of the **single matched** transition returned false. FastFsm does not try a second candidate.

## Attempt outcomes

`OnAttemptCompleted` always receives a `TransitionResult<TState>`. The outcome describes how the attempt ended; `Stage` and `Exception` add detail for failures that occurred during transition work.

| Outcome | `TryFire` / `TryFireAsync` return | `MatchedTransition` | `ResolvedTarget` | `Stage` | `Exception` |
|---|---|---|---|---|---|
| `Succeeded` | `true` | matched transition (or `null` for internal-only paths that never materialized a candidate) | leaf entered for external transitions; `null` for internal | usually `null` | `null` |
| `GuardRejected` | `false` | the single candidate whose guard returned false | `null` (state never changed) | `null` | `null` |
| `UnhandledTrigger` | `false` | `null` (nothing matched) | `null` | `null` | `null` |
| `InvalidPayload` | `false` | `null` | `null` | `null` | `null` |
| `Canceled` | throws `OperationCanceledException` (async) or `false` when cancellation is treated as failure | candidate when one was matched before cancel | last resolved target if state changed before cancel; otherwise `null` | stage active when cancel occurred (`Guard`, `OnExit`, …) | the `OperationCanceledException` |
| `Faulted` | rethrows after `OnAttemptCompleted` unless the machine exception handler swallows it | candidate when one was matched | last resolved target if state changed before fault; otherwise `null` | stage active when the fault occurred | the faulting exception |

For async machines, a call whose `CancellationToken` is already canceled at entry **does not start an attempt**: no `OnAttemptStarting`, no increment of the attempt counter, and no other extension hooks for that call.

The extension set is captured once at the beginning of an attempt and stays fixed for that attempt. An extension removed mid-attempt still receives `OnAttemptCompleted`; one added mid-attempt receives nothing for that attempt.

## Hierarchical machines

These fields are the HSM contract. There are no extra HSM-specific callback methods.

| Field | Meaning |
|---|---|
| `attempt.SourceState` | active leaf when `Fire` / `TryFire` began |
| `MatchedTransition.HandledAtState` | state that owns the selected transition, including an ancestor |
| `MatchedTransition.Kind` | from `TransitionModel.IsInternal`; never from `From == To` |
| `DeclaredTarget` | configured target; `null` for internal |
| `ResolvedTarget` | leaf actually entered after initial/history resolution; `null` for internal and when resolution never ran |
| `FinalState` | `CurrentState` when the attempt completed; always present |

Internal transitions raise no state hooks. External ancestor self-transitions exit and re-enter the active subtree. A transition from an ancestor to a descendant uses the owning state as the lifecycle boundary, not the active leaf.

User `OnExit` / `OnEntry` / `Action` methods keep their existing execution order. Observability hooks wrap that sequence; they do not reorder it.

## Extension exceptions

`ExtensionRunner` catches exceptions thrown by extension hooks so they do not propagate through the transition. When `FastFsm.Sharp.Logging` is enabled, those exceptions are logged at Error level.

## Disabling extension support

Set `GenerateExtensibleVersion = false`, or omit the property, to generate the non-extensible variant.

## Tests

- `src/Fsm/Fsm.Tests/Tests.Fsm/Extensions/` — contract, outcomes, lifecycle, and HSM semantics
- `src/Fsm/Fsm.Tests/Tests.Async/Features/Extensions/` — async parity

For DI registration of extensions, see [dependency-injection.md](dependency-injection.md).
