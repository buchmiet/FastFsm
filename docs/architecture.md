# Architecture (0.9)

Contributor-oriented overview of the FastFsm 0.9 codebase. API guides live under [docs/](.).

## High-level flow

```
Your partial class + [StateMachine] / Fluent Configure()
        │
        ▼
  Roslyn analyzer pass (Generator.dll + Generator.Rules.dll)
        │
        ├── Validation rules → diagnostics (FSM0100–FSM3083)
        └── Code emission → *.g.cs partial implementations
        │
        ▼
  FastFsm runtime (StateMachineBase / AsyncStateMachineBase)
        │
        ├── Optional: ExtensionRunner + IStateMachineExtension
        ├── Optional: ILogger (FastFsm.Logging generator)
        └── Optional: DI factory (FastFsm.DependencyInjection)
```

## Projects

| Project | Role |
|---------|------|
| `Abstractions` | `[StateMachine]`, transition/state attributes, Fluent `FSM` API definitions |
| `Generator` | `IIncrementalGenerator` — parses models and emits C# |
| `Generator.Model` | Shared model types |
| `Generator.Rules` | Validation rules and `RuleIdentifiers` / `DefinedRules` |
| `Generator.Logger` | Logging code generation |
| `Generator.DependencyInjection` | DI-related code-generation hooks |
| `FastFsm` | Runtime bases and contracts; packaged as `FastFsm.Net` |
| `FastFsm.Logging` | Logging package (`FastFsm.Net.Logging`) |
| `FastFsm.DependencyInjection` | DI package (`FastFsm.Net.DependencyInjection`) |
| `Machines.Tests` | Shared machine definitions for tests |

## Generator entry points

- **Attribute parser** — `Generator/Parsers/StateMachineParser.cs`
- **Fluent parser** — `Generator/Parsers/FluentParser.cs`
- **Unified emitter** — `Generator/SourceGenerators/UnifiedStateMachineGenerator.cs`

Both configuration APIs converge on the same internal model before emission.

## Runtime

- `StateMachineBase<TState,TTrigger>` — synchronous transitions and `TryFire` / `Fire`
- `AsyncStateMachineBase<…>` — asynchronous transitions, `ValueTask` API, serialized transition attempts
- `ExtensionRunner` — invokes extension hooks; included as compile-time content in the core package
- `IStateMachineContext` / `IStateSnapshot` — context exposed to extensions

## Packages and build

- Target framework: `net10.0` (`Directory.Build.props`)
- Repository package version: `FastFsmPackageVersion` (`0.9.0`)
- `UsePackages=false` — repository projects use project references and analyzer wiring from `eng/FastFsmReferences.props` / `Directory.Build.targets`
- `UsePackages=true` — package references resolve the configured FastFsm package version from the configured NuGet sources

The `0.9.0` repository version is not a public release until the corresponding packages are published.

Analyzers are packed under `analyzers/dotnet/cs` in the NuGet package.

## Test layout

| Project | Focus |
|---------|-------|
| `FastFsm.Tests` | Core synchronous behavior, extensions, HSM compile tests |
| `FastFsm.Async.Tests` | Asynchronous API and HSM |
| `FastFsm.Logging.Tests` | Logging and Attribute/Fluent parity matrix |
| `FastFsm.DependencyInjection.Tests` | DI registration |
| `Generator.Tests` | Generator rules and emission tests |
| `Machines.Tests` | Machine definitions referenced by other test projects |

## Maintenance docs

Repository recovery and branch notes: [maintenance/repository-archeology-2026-08.md](maintenance/repository-archeology-2026-08.md).

## 0.9 codebase

- Fluent and attribute configuration use a single generator pipeline
- DI and logging are separate NuGet packages
- Diagnostic IDs use FSM01xx / FSM11xx / FSM20xx / FSM30xx ranges
- `OnTransitioned` is part of `IStateMachineExtension`
