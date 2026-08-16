# FastFsm

Source-generated finite and hierarchical state machines for .NET 10.

FastFsm compiles your state logic at build time into tight `switch`-based code. You define machines with a **Fluent DSL** or **attributes**; the generator validates transitions, guards, and hierarchy before runtime.

**Current release line:** `0.9.0` (see `Directory.Build.props` / package version in CI).

## Install

```bash
dotnet add package FastFsm.Net
# optional
dotnet add package FastFsm.Net.Logging
dotnet add package FastFsm.Net.DependencyInjection
```

Requires **.NET SDK 10.0** (see `global.json`).

## Quick example (Fluent API)

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;

public enum DoorState { Closed, Open, Locked }
public enum DoorTrigger { Open, Close, Lock, Unlock }

[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    private void Configure() => FSM
        .State(DoorState.Closed)
            .On(DoorTrigger.Open).GoTo(DoorState.Open)
            .On(DoorTrigger.Lock).GoTo(DoorState.Locked)
        .State(DoorState.Open)
            .OnEntry(OnDoorOpened)
            .On(DoorTrigger.Close).GoTo(DoorState.Closed)
        .State(DoorState.Locked)
            .On(DoorTrigger.Unlock).GoTo(DoorState.Closed);

    private void OnDoorOpened() => Console.WriteLine("Door opened");
}

var door = new DoorController(DoorState.Closed);
door.Start();                         // required before firing triggers
door.Fire(DoorTrigger.Open);          // throws if transition is invalid
bool ok = door.TryFire(DoorTrigger.Close); // returns false instead of throwing
```

Prefer attributes? See [docs/attribute-api.md](docs/attribute-api.md).

## Features

- Compile-time validation (states, triggers, guards, HSM hierarchy, Fluent DSL purity)
- Sync and async machines (`ValueTask` for async paths)
- Hierarchical states, shallow/deep history, internal transitions, transition priority
- Typed payloads per trigger
- Extensions (`IStateMachineExtension`) for cross-cutting hooks
- Optional logging (`FastFsm.Net.Logging`) and DI (`FastFsm.Net.DependencyInjection`)
- Trimming / Native AOT compatible generated code paths

## Documentation

| Topic | Guide |
|-------|-------|
| Setup & first machine | [docs/getting-started.md](docs/getting-started.md) |
| Fluent DSL | [docs/fluent-api.md](docs/fluent-api.md) |
| Attribute API | [docs/attribute-api.md](docs/attribute-api.md) |
| Hierarchical machines | [docs/hsm.md](docs/hsm.md) |
| Async | [docs/async.md](docs/async.md) |
| Payloads | [docs/payloads.md](docs/payloads.md) |
| Extensions | [docs/extensions.md](docs/extensions.md) |
| Logging | [docs/logging.md](docs/logging.md) |
| Dependency injection | [docs/dependency-injection.md](docs/dependency-injection.md) |
| Diagnostics (FSM0100–FSM3083) | [docs/diagnostics.md](docs/diagnostics.md) |
| Architecture (contributors) | [docs/architecture.md](docs/architecture.md) |
| Benchmarks | [docs/benchmarks.md](docs/benchmarks.md) |

## Packages

| Package | Purpose |
|---------|---------|
| `FastFsm.Net` | Core runtime + source generator |
| `FastFsm.Net.Logging` | `ILogger` integration for generated machines |
| `FastFsm.Net.DependencyInjection` | `Microsoft.Extensions.DependencyInjection` registration helpers |

## Repository layout

- `FastFsm/` — core library packaged as `FastFsm.Net`
- `Abstractions/` — attributes and Fluent DSL surface consumed by your code
- `Generator/` — Roslyn source generator
- `Generator.Rules/` — diagnostic rule definitions (`RuleIdentifiers`, `DefinedRules`)
- `Machines.Tests/` — shared machine definitions used across test projects

## Contributing

Build and test from a clean tree (no pre-built `0.9.0` nupkg required when `UsePackages=false`):

```bash
dotnet test FastFsm.Tests/FastFsm.Tests.csproj
dotnet test FastFsm.Async.Tests/FastFsm.Async.Tests.csproj
dotnet test Generator.Tests/Generator.Tests.csproj
```

See [docs/architecture.md](docs/architecture.md) for generator layout.

## License

MIT — see [LICENSE](LICENSE).
