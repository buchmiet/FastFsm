# Extensions

Implement `IStateMachineExtension` to observe transitions without modifying generated transition tables.

Enable extension support with `GenerateExtensibleVersion = true` on `[StateMachine]` (this is the **default**). Pass extensions to the machine constructor:

```csharp
var machine = new MyMachine(
    initialState,
    extensions: new IStateMachineExtension[] { new AuditExtension() },
    logger: null);
```

## Hook interface

```csharp
public interface IStateMachineExtension
{
    void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext;
    void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext;
    void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext;
    void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext;
    void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext;
    void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext;
    void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext;
}
```

## Call order (successful external transition)

For a successful transition with state change:

1. `OnBeforeTransition`
2. `OnGuardEvaluation` / `OnGuardEvaluated` (per guard)
3. State exit / action / entry (generated code)
4. **`OnTransitioned`** — after effects, before after-transition notification
5. `OnAfterTransition(context, success: true)`

For internal transitions (no state change), `OnInternalTransition` is also invoked from the after-transition path when `FromState` equals `ToState`.

Failed transitions call `OnAfterTransition(context, success: false)` and may call `OnUnhandledTrigger` when no handler exists.

## Error isolation

`ExtensionRunner` catches exceptions from extension hooks so a faulty extension does not break the state machine. When `FastFsm.Net.Logging` is enabled, extension errors are logged at Error level.

## Disable extensions

Set `GenerateExtensibleVersion = false` if you do not need hooks and want the simpler generated constructor.

## Examples

- `Machines.Tests/Extensions/` — test extensions (`TestExtension`, `AuditExtension`, …)
- `FastFsm.Tests/Features/Extensions/OnTransitionedTests.*` — `OnTransitioned` ordering tests

Register extensions globally via DI: [dependency-injection.md](dependency-injection.md).
