# Logging

Install the logging package:

```bash
dotnet add package FastFsm.Logging.Sharp
```

The package sets `FsmGenerateLogging=true` and defines `FSM_LOGGING_ENABLED`. Generated machines then include the logging-specific constructor parameter and lifecycle logging code.

## Project-reference configuration

When using project references inside this repository, enable logging generation with:

```xml
<PropertyGroup>
  <FsmGenerateLogging>true</FsmGenerateLogging>
</PropertyGroup>
```

The `FastFsm.Logging.Sharp` package props set this property for package consumers.

## Constructor injection

```csharp
var logger = loggerFactory.CreateLogger<DoorController>();
var door = new DoorController(DoorState.Closed, logger: logger);
door.Start();
door.Fire(DoorTrigger.Open);
```

## Event categories

Generated logging includes events for:

- machine start and stop
- transition start, success, and failure
- guard evaluation
- `OnEntry`, `OnExit`, and action execution
- unhandled triggers
- hierarchy-specific processing when HSM support is enabled

Event IDs and templates are defined by the logging source generator in `Generator.Logger`.

## Extensions and logging

When `FSM_LOGGING_ENABLED` is active, exceptions caught while invoking extension hooks can be reported through the generated logging path. Extension behavior is defined by `IStateMachineExtension` implementations.

## Tests

`Tests.Logging` (`src/Fsm/Fsm.Tests/Tests.Logging/`) contains logging integration tests and the Attribute/Fluent parity matrix (`DualApiMatrixTests`).

## Related

- [extensions.md](extensions.md) — extension hooks
- [getting-started.md](getting-started.md) — package installation
