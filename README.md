# FastFsm

Source-generated finite and hierarchical state machines for .NET 10.

FastFsm generates `switch`-based state-machine code at build time. Machines can be configured with either the **Fluent API** or the **Attribute API**. The generator validates transitions, guards, callbacks, and hierarchy during compilation.

**Repository package version:** `0.9.0` (defined in `Directory.Build.props`). See [CHANGELOG.md](CHANGELOG.md).

## Install

```bash
dotnet add package FastFsm.Net
# optional
dotnet add package FastFsm.Net.Logging
dotnet add package FastFsm.Net.DependencyInjection
```

Requires **.NET SDK 10.0** (see `global.json`).

## Configuration APIs

Both APIs use the same generator and runtime.

### Fluent API

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;

public enum DoorState { Closed, Open }
public enum DoorTrigger { Open, Close }

[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    private void Configure() => FSM
        .State(DoorState.Closed)
            .On(DoorTrigger.Open).GoTo(DoorState.Open)
        .State(DoorState.Open)
            .On(DoorTrigger.Close).GoTo(DoorState.Closed);
}
```

See [docs/fluent-api.md](docs/fluent-api.md).

### Attribute API

```csharp
using Abstractions.Attributes;

public enum DoorState { Closed, Open }
public enum DoorTrigger { Open, Close }

[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    [Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Open)]
    [Transition(DoorState.Open, DoorTrigger.Close, DoorState.Closed)]
    private void ConfigureTransitions() { }
}
```

See [docs/attribute-api.md](docs/attribute-api.md).

For either form, create the generated machine with an initial state, call `Start()`, then use `Fire()` or `TryFire()`:

```csharp
var door = new DoorController(DoorState.Closed);
door.Start();
door.Fire(DoorTrigger.Open);              // throws when no valid transition exists
bool ok = door.TryFire(DoorTrigger.Close); // returns false when no valid transition exists
```

## Features

- Compile-time validation of state-machine definitions
- Synchronous and asynchronous machines (`ValueTask` on asynchronous paths)
- Hierarchical states, shallow and deep history, internal transitions, and transition priority
- Typed payloads per trigger
- `IStateMachineExtension` transition hooks
- Optional logging through `FastFsm.Net.Logging`
- Optional dependency-injection integration through `FastFsm.Net.DependencyInjection`
- Generated code paths compatible with trimming and Native AOT

## Documentation

| Topic | Guide |
|-------|-------|
| Setup and first machine | [docs/getting-started.md](docs/getting-started.md) |
| Fluent API | [docs/fluent-api.md](docs/fluent-api.md) |
| Attribute API | [docs/attribute-api.md](docs/attribute-api.md) |
| Hierarchical machines | [docs/hsm.md](docs/hsm.md) |
| Async | [docs/async.md](docs/async.md) |
| Payloads | [docs/payloads.md](docs/payloads.md) |
| Extensions | [docs/extensions.md](docs/extensions.md) |
| Logging | [docs/logging.md](docs/logging.md) |
| Dependency injection | [docs/dependency-injection.md](docs/dependency-injection.md) |
| Diagnostics (FSM0100–FSM3083) | [docs/diagnostics.md](docs/diagnostics.md) |
| Architecture | [docs/architecture.md](docs/architecture.md) |
| Benchmarks | [docs/benchmarks.md](docs/benchmarks.md) |
| Changelog | [CHANGELOG.md](CHANGELOG.md) |
| Roadmap | [ROADMAP.md](ROADMAP.md) |

## Packages

| Package | Purpose |
|---------|---------|
| `FastFsm.Net` | Runtime and source generator |
| `FastFsm.Net.Logging` | `ILogger` integration for generated machines |
| `FastFsm.Net.DependencyInjection` | `Microsoft.Extensions.DependencyInjection` registration helpers |

## Repository layout

- `FastFsm/` — runtime packaged as `FastFsm.Net`
- `Abstractions/` — attributes and Fluent API definitions
- `Generator/` — Roslyn source generator
- `Generator.Rules/` — diagnostic rule definitions (`RuleIdentifiers`, `DefinedRules`)
- `Machines.Tests/` — shared machine definitions used across test projects

## Contributing

Build and test from a clean tree. With `UsePackages=false`, test projects use project references rather than a pre-built `0.9.0` package.

```bash
dotnet test FastFsm.slnx -c Release
```

That runs `FastFsm.Tests`, `FastFsm.Async.Tests`, `FastFsm.Logging.Tests`, `FastFsm.DependencyInjection.Tests`, `FastFsm.Instance.Tests`, and `Generator.Tests`. `Machines.Tests` is a shared machine library, not a test runner.

Pack the three NuGet packages and compile clean consumer consoles against `./nuget`:

```bash
# Windows
./scripts/pack-and-smoke.ps1
# Linux / macOS
bash ./scripts/pack-and-smoke.sh
```

CI for pull requests runs on GitHub-hosted runners (`.github/workflows/ci.yml`). Self-hosted Windows/Linux/macOS jobs run only on `push` to `main` and `workflow_dispatch` (`.github/workflows/ci-self-hosted.yml`).

See [docs/architecture.md](docs/architecture.md) for the generator layout.

## License

MIT — see [LICENSE](LICENSE).
