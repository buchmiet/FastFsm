# Logging

Install the logging package:

```bash
dotnet add package FastFsm.Net.Logging
```

The package sets `FsmGenerateLogging=true` and defines `FSM_LOGGING_ENABLED` so generated machines accept an optional `ILogger<TMachine>` constructor parameter and emit structured lifecycle events.

## Enable in your project

When consuming via project reference (this repository), set in your `.csproj`:

```xml
<PropertyGroup>
  <FsmGenerateLogging>true</FsmGenerateLogging>
</PropertyGroup>
```

The `FastFsm.Net.Logging` package props do this automatically for package consumers.

## Constructor injection

```csharp
var logger = loggerFactory.CreateLogger<DoorController>();
var door = new DoorController(DoorState.Closed, logger: logger);
door.Start();
door.Fire(DoorTrigger.Open);
```

## Event categories

Generated machines log transition lifecycle events when the logger is enabled, including:

- Machine started / stopped
- Transition started / succeeded / failed
- Guard evaluation
- OnEntry / OnExit / action execution
- Unhandled triggers
- HSM-specific events (composite entry, history restore, hierarchical transition summary) when hierarchy is enabled

Exact event IDs and templates are defined in the logging source generator (`Generator.Logger`).

## Extensions and logging

Extension exceptions are logged via `ExtensionRunner` when `FSM_LOGGING_ENABLED` is active. Extension hooks themselves remain in your `IStateMachineExtension` implementations.

## Tests

`FastFsm.Logging.Tests` contains integration tests and the Legacy ↔ Fluent parity matrix (`DualApiMatrixTests`).

## Related

- [extensions.md](extensions.md) — extension hooks (orthogonal to `ILogger`)
- [getting-started.md](getting-started.md) — package installation
