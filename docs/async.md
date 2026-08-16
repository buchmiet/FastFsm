# Async state machines

When any callback (guard, action, `OnEntry`, or `OnExit`) returns `ValueTask` or `ValueTask<T>`, the generator produces an asynchronous state machine.

## Sync vs async rules

- Callbacks must satisfy the machine's synchronous/asynchronous mode rules (diagnostic FSM1100).
- Async guards return `ValueTask<bool>`, not `Task<bool>` (FSM1110).
- `async void` callbacks are rejected (FSM0302).

## API surface

| Sync | Async |
|------|-------|
| `Start()` | `StartAsync(CancellationToken ct = default)` |
| `TryFire(trigger[, payload])` | `TryFireAsync(trigger[, payload], ct)` |
| `Fire(trigger[, payload])` | `FireAsync(trigger[, payload], ct)` |
| `CanFire(trigger)` | `CanFireAsync(trigger, ct)` |

Calling synchronous `Fire` or `TryFire` on an asynchronous machine throws `SyncCallOnAsyncMachineException`. Use the asynchronous API for asynchronous machines.

## Semantics

- `TryFireAsync` returns `ValueTask<bool>` and returns `false` when no transition applies or a guard rejects the transition.
- `FireAsync` throws `InvalidOperationException` when the transition cannot be taken.
- Asynchronous machines serialize transition attempts using internal synchronization.

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

`[StateMachine(..., ContinueOnCapturedContext = true)]` controls whether asynchronous continuations capture the current synchronization context. The default is `false`.

## Tests

`FastFsm.Async.Tests` contains asynchronous API, extension-ordering, and HSM scenarios for Attribute and Fluent configurations.
