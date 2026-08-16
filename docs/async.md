# Async state machines

When any callback (guard, action, `OnEntry`, `OnExit`) returns `ValueTask` or `ValueTask<T>`, the generator produces an **async** state machine.

## Sync vs async rules

- **All callbacks must be consistently sync or async** in a single machine (diagnostic FSM1100).
- Async guards must return `ValueTask<bool>`, not `Task<bool>` (FSM1110).
- `async void` callbacks are rejected (FSM0302).

## API surface

| Sync | Async |
|------|-------|
| `Start()` | `StartAsync(CancellationToken ct = default)` |
| `TryFire(trigger[, payload])` | `TryFireAsync(trigger[, payload], ct)` |
| `Fire(trigger[, payload])` | `FireAsync(trigger[, payload], ct)` |
| `CanFire(trigger)` | `CanFireAsync(trigger, ct)` |

Calling sync `Fire` / `TryFire` on an async machine throws `SyncCallOnAsyncMachineException`. Always use the `*Async` methods.

## Semantics

- `TryFireAsync` returns `ValueTask<bool>` — `false` when no transition applies or a guard fails.
- `FireAsync` throws `InvalidOperationException` when `TryFireAsync` would return `false` (same contract as sync `Fire`).
- Async machines serialize transitions (one in flight at a time) using internal synchronization.

## Example

```csharp
[StateMachine(typeof(S), typeof(T))]
public partial class AsyncWorkflow
{
    private void Configure() => FSM
        .State(S.Idle)
            .On(T.Start)
                .Guard(CanStartAsync)
                .Action(StartAsync)
                .GoTo(S.Running);

    private ValueTask<bool> CanStartAsync(CancellationToken ct) => ValueTask.FromResult(true);

    private async ValueTask StartAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
    }
}

var machine = new AsyncWorkflow(S.Idle);
await machine.StartAsync();
await machine.FireAsync(T.Start);
```

## `ContinueOnCapturedContext`

`[StateMachine(..., ContinueOnCapturedContext = true)]` controls whether async continuations post back to the captured synchronization context. Default is `false` for lower overhead.

## Testing

See `FastFsm.Async.Tests` for parity coverage between Legacy and Fluent APIs, extension hook order, and HSM async scenarios.
