# Architecture (0.9)

Contributor-oriented overview of the FastFsm 0.9 codebase. User-facing API guides live under [docs/](.).

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
| `Abstractions` | `[StateMachine]`, transition/state attributes, Fluent `FSM` DSL stubs |
| `Generator` | `IIncrementalGenerator` — parses models, emits C# |
| `Generator.Model` | Shared AST / model types |
| `Generator.Rules` | Validation rules + `RuleIdentifiers` / `DefinedRules` |
| `Generator.Logger` | Logging codegen |
| `Generator.DependencyInjection` | DI-aware codegen hooks |
| `FastFsm` | Runtime bases, contracts, packaged as **FastFsm.Net** |
| `FastFsm.Logging` | Logging package (**FastFsm.Net.Logging**) |
| `FastFsm.DependencyInjection` | DI package (**FastFsm.Net.DependencyInjection**) |
| `Machines.Tests` | Shared machine definitions for cross-project tests |

## Generator entry points

- **Attribute parser** — `Generator/Parsers/StateMachineParser.cs`
- **Fluent parser** — `Generator/Parsers/FluentParser.cs`
- **Unified emitter** — `Generator/SourceGenerators/UnifiedStateMachineGenerator.cs` (successor to separate legacy/fluent generators)

Both API styles converge on the same internal model before emission.

## Runtime

- `StateMachineBase<TState,TTrigger>` — sync transitions, `TryFire` / `Fire`
- `AsyncStateMachineBase<…>` — async transitions, `ValueTask` API, serialized execution
- `ExtensionRunner` — invokes extension hooks; shipped as compile-time content in the core package
- `IStateMachineContext` / `IStateSnapshot` — context passed to extensions

## Packages and build

- Target framework: **net10.0** (`Directory.Build.props`)
- Version: `FastFsmPackageVersion` (0.9.0)
- `UsePackages=false` in repo — test projects use `ProjectReference` + analyzer wiring (`eng/FastFsmReferences.props`, `Directory.Build.targets`)
- `UsePackages=true` — consumers reference published `FastFsm.Net` 0.9.0 packages

Analyzers are packed under `analyzers/dotnet/cs` in the NuGet package.

## Test layout

| Project | Focus |
|---------|-------|
| `FastFsm.Tests` | Core sync, extensions, HSM compile tests |
| `FastFsm.Async.Tests` | Async API + HSM |
| `FastFsm.Logging.Tests` | Logging + Legacy/Fluent parity matrix |
| `FastFsm.DependencyInjection.Tests` | DI registration |
| `Generator.Tests` | Generator rule and emission tests |
| `Machines.Tests` | Machine catalog only (referenced by other test projects) |

## Maintenance docs

Repository recovery and branch notes: [maintenance/repository-archeology-2026-08.md](maintenance/repository-archeology-2026-08.md).

Historical architecture drafts: [archive/architecture/](archive/architecture/).

## What changed since 0.7 / 0.8

- Single unified generator path (see [archive/specs/0.8-generator-refactor.md](archive/specs/0.8-generator-refactor.md))
- Fluent DSL is a first-class configuration style alongside attributes
- DI and logging are **separate packages**, not monolithic core
- Diagnostic IDs reorganized into FSM01xx / FSM11xx / FSM20xx / FSM30xx ranges
- `OnTransitioned` extension hook restored in 0.9 recovery line

Features **not** in 0.9: deferred events ([archive/proposals/deferred-events.md](archive/proposals/deferred-events.md)), StringEnum payloads ([archive/proposals/string-enum.md](archive/proposals/string-enum.md)).
