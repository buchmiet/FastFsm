# Extensions

Implement `IStateMachineExtension` to observe transition processing without modifying generated transition tables.

Extension support is selected with `GenerateExtensibleVersion` on `[StateMachine]`. In the current 0.9 codebase, set this property explicitly because the attribute property's initializer and the generator's handling of an omitted named argument are inconsistent.

```csharp
[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
```

Pass extension instances to the generated machine constructor:

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

## Call order for a successful external transition

For a successful transition that changes state:

1. `OnBeforeTransition`
2. `OnGuardEvaluation` / `OnGuardEvaluated` for each evaluated guard
3. Generated exit, action, and entry processing
4. `OnTransitioned`
5. `OnAfterTransition(context, success: true)`

Internal-transition processing also invokes `OnInternalTransition` through the generated extension path.

A failed transition invokes `OnAfterTransition(context, success: false)`. `OnUnhandledTrigger` is invoked when no applicable handler is found.

## Extension exceptions

`ExtensionRunner` catches exceptions thrown by extension hooks so they do not propagate through the state-machine transition call. When `FastFsm.Net.Logging` is enabled, extension exceptions are logged at Error level.

## Disabling extension support

Set `GenerateExtensibleVersion = false` explicitly to generate the non-extensible variant.

## Tests

- `Machines.Tests/Extensions/` — extension implementations used by tests
- `FastFsm.Tests/Features/Extensions/OnTransitionedTests.*` — `OnTransitioned` ordering tests

For DI registration of extensions, see [dependency-injection.md](dependency-injection.md).
