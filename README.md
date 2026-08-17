# FastFsm

Source-generated finite and hierarchical state machines for .NET 10.

FastFsm generates `switch`-based state-machine code at build time. Machines can be configured with either the **Fluent API** or the **Attribute API**. The generator validates transitions, guards, callbacks, and hierarchy during compilation.

**Repository package version:** `0.9.0` (defined in `Directory.Build.props`). See [CHANGELOG.md](CHANGELOG.md). **0.9.0** is on [NuGet.org](https://www.nuget.org/packages/FastFsm.Sharp) as `FastFsm.Sharp*`, with legacy `FastFsm.Net*` metapackages forwarding to the same bits.

## Install

```bash
dotnet add package FastFsm.Sharp
# optional
dotnet add package FastFsm.Sharp.Logging
dotnet add package FastFsm.Sharp.DependencyInjection
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
- Optional logging through `FastFsm.Sharp.Logging`
- Optional dependency-injection integration through `FastFsm.Sharp.DependencyInjection`
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
| `FastFsm.Sharp` | Runtime and source generator |
| `FastFsm.Sharp.Logging` | `ILogger` integration for generated machines |
| `FastFsm.Sharp.DependencyInjection` | `Microsoft.Extensions.DependencyInjection` registration helpers |

**Migrating from FastFsm.Net 0.6.9.x:** new projects use `FastFsm.Sharp*`. Existing `FastFsm.Net*` references can stay — 0.9.0 ships legacy metapackages that forward to `FastFsm.Sharp*` (see [CHANGELOG.md](CHANGELOG.md)). Your machine code (`Abstractions.*`, `FastFsm.*`) stays the same. Details: [docs/architecture.md](docs/architecture.md#public-api-compatibility-090).

## Repository layout

- `src/Fsm/Fsm.Core/` — runtime packaged as `FastFsm.Sharp`
- `src/Abstractions/` — attributes and Fluent API definitions
- `src/Generator/` — Roslyn source generator (`Generator.Core`, `Generator.Model`, `Generator.Rules`, …)
- `src/Generator/Generator.Rules/` — diagnostic rule definitions (`RuleIdentifiers`, `DefinedRules`)
- `src/Fsm/Fsm.Tests/Tests.Machines/` — shared machine definitions used by `Tests.Fsm` and `Tests.Logging`
- `src/Fsm/Fsm.Tests/Tests.*/` — FSM test runners (`Tests.Fsm`, `Tests.Async`, `Tests.Logging`, …)
- `src/Generator/Generator.Tests/Tests.SourceGenerators/` — generator rule and emission tests

## Contributing

Build and test from a clean tree. With `UsePackages=false`, test projects use project references rather than a pre-built package from `./nuget`.

```bash
dotnet test FastFsm.slnx -c Release
```

That runs `Tests.Fsm`, `Tests.Async`, `Tests.Logging`, `Tests.DependencyInjection`, `Tests.Instance`, and `Tests.SourceGenerators`. `Tests.Machines` is a shared machine library, not a test runner.

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
