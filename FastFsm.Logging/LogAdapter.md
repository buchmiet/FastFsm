# FastFsm.Runtime.Logging.LogAdapter

## What it is
`FastFsm.Runtime.Logging.LogAdapter` is a tiny, shared helper that centralizes the logging surface used by code generated when logging is enabled. It lives in the `FastFsm.Runtime.Logging` namespace and is shipped as a `contentFiles` source file in the `FastFsm.Net.Logging` package, so it gets compiled into the consuming project only when that package is referenced.

- Namespace: `FastFsm.Runtime.Logging`
- Type: `internal static class LogAdapter`
- Members: a small set of methods that mirror the events produced by the generator (e.g. `TransitionSucceeded`, `GuardFailed`, `TransitionFailed`, `OnEntryExecuted`, `OnExitExecuted`, `ActionExecuted`, `PayloadValidationFailed`, …)
- Signatures: each method is declared as an extension method on `ILogger` (first parameter `this ILogger logger`), but the generator calls them as static methods: `LogAdapter.TransitionSucceeded(_logger, ...)` — both forms are valid.

## Why it exists
Previously the generator emitted a per‑machine helper class like `{ClassName}Log` into the compilation and then invoked `ClassNameLog.TransitionSucceeded(...)`. After a larger refactor that changed naming in the generator, those helper files stopped being added in some scenarios, causing errors like:

```
error CS0103: The name 'PureStateMachineLog' does not exist in the current context
```

Moving to a single, shared `LogAdapter` solves that class‑emission coupling and makes logging robust again while preserving the library’s pay‑for‑play model:

- No binary dependency leaked into the core: the adapter is shipped as source (`contentFiles`) by `FastFsm.Net.Logging`.
- It only compiles into projects that explicitly reference `FastFsm.Net.Logging` (or DI, which also enables logging).
- The generator stays simple: it just calls one known target (`LogAdapter`) instead of generating per‑machine logger types.

## How the generator uses it
When logging is enabled (`FsmGenerateLogging=true`), the generator injects an `ILogger<TMachine>? _logger` field and emits calls to `LogAdapter` for relevant events. Example (simplified):

```csharp
// in generated TryFire
_currentState = State.Processing;
if (_logger?.IsEnabled(LogLevel.Information) == true)
{
    global::FastFsm.Runtime.Logging.LogAdapter
        .TransitionSucceeded(_logger, _instanceId, "Initial", "Processing", "Start");
}
```

The methods are plain `ILogger` calls with structured message templates and stable event IDs:

- TransitionSucceeded: EventId(1)
- GuardFailed: EventId(2)
- TransitionFailed: EventId(3)
- OnEntryExecuted: EventId(4)
- OnExitExecuted: EventId(5)
- ActionExecuted: EventId(6)
- PayloadValidationFailed: EventId(7)
- InternalTransitionOnAncestor: EventId(10)
- HierarchicalTransition: EventId(11)
- CompositeStateEntry: EventId(12)
- HistoryRestored: EventId(13)
- ActivePath: EventId(14)

## Where it comes from
- `FastFsm.Net.Logging` packs `shared/LogAdapter.cs` as `contentFiles`. Adding `PackageReference Include="FastFsm.Net.Logging"` brings it into your project automatically.
- The DI overlay (`FastFsm.Net.DependencyInjection`) also packs the same adapter to ensure it is present when DI implicitly enables logging.

## Compatibility and performance
- The adapter keeps the original structured messages the generator emitted; only the call target changed.
- It uses the standard `Microsoft.Extensions.Logging.Abstractions` API and performs level checks (`IsEnabled`) before logging.
- Because it’s source‑included, it remains trimming/AOT‑friendly.

## Extending it
If in the future we add a new generator log point (e.g. a new event), we only need to:
- add a corresponding method to `LogAdapter`, and
- emit a call from the generator.

No per‑machine helper class is required anymore.
