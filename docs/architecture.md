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
| `FastFsm` | Runtime bases and contracts; packaged as `FastFsm.Sharp` |
| `Fsm.Logging` | Logging package (`FastFsm.Sharp.Logging`) |
| `Fsm.DependencyInjection` | DI package (`FastFsm.Sharp.DependencyInjection`) |
| `Tests.Machines` | Shared machine definitions at `src/Fsm/Fsm.Tests/Tests.Machines/`; used by `Tests.Fsm` and `Tests.Logging` |

## Generator entry points

- **Attribute parser** — `src/Generator/Generator.Core/Parsers/StateMachineParser.cs`
- **Fluent parser** — `src/Generator/Generator.Core/Parsers/FluentParser.cs`
- **Unified emitter** — `src/Generator/Generator.Core/SourceGenerators/UnifiedStateMachineGenerator.cs`

Both configuration APIs converge on the same internal model before emission.

## Runtime

- `StateMachineBase<TState,TTrigger>` — synchronous transitions and `TryFire` / `Fire`
- `AsyncStateMachineBase<…>` — asynchronous transitions, `ValueTask` API, serialized transition attempts
- `ExtensionRunner` — invokes extension hooks; included as compile-time content in the core package
- `IStateMachineContext` / `IStateSnapshot` — context exposed to extensions

## Packages and build

- Target framework: `net10.0` (`Directory.Build.props`)
- Repository package version: `FastFsmPackageVersion` (`0.9.1`)
- `UsePackages=false` — repository projects use project references and analyzer wiring from `eng/FastFsmReferences.props` / `Directory.Build.targets`
- `UsePackages=true` — package references resolve the configured FastFsm package version from the configured NuGet sources

NuGet still lists `0.9.0` under `FastFsm.Net*`. Repository `0.9.1` ships as `FastFsm.Sharp*` when published.

Analyzers are packed under `analyzers/dotnet/cs` in the NuGet package.

## Public API compatibility (0.9.1)

0.9.1 rebrands **NuGet marketing IDs** and **repository layout**, but keeps the **consumer C# and binary surface** aligned with 0.9.0.

### Naming model (idiomatic split)

| Layer | Example | Can differ from folder name? | Role |
|-------|---------|------------------------------|------|
| **Repo / slnx** | `src/Fsm/Fsm.Core/Fsm.Core.csproj` | yes | Contributor ergonomics only |
| **NuGet PackageId** | `FastFsm.Sharp` (canonical), `FastFsm.Net` (legacy metapackage) | yes | How consumers reference packages |
| **AssemblyName** | `FastFsm`, `Generator` | yes | Binary identity in `lib/` and `analyzers/` |
| **C# namespace** | `FastFsm.Runtime`, `Abstractions.Attributes` | yes | Public API in source code |

Do not force these four to share one string. Match industry practice: **Serilog** (package) vs **Serilog** (assembly) vs nested namespaces; **Microsoft.Extensions.\*** packages with stable `Microsoft.Extensions.*` namespaces.

`.Sharp` on **PackageId only** signals the .NET product line without renaming every `using FastFsm`.

### Preserved (not a source-code break)

| Layer | What stays the same | Why |
|-------|---------------------|-----|
| **C# namespaces** | `Abstractions.*`, `FastFsm.Runtime`, `FastFsm.Contracts`, `FastFsm.Exceptions`, `FastFsm.Builder`, `FastFsm.DependencyInjection` | Public API consumers `using` these today |
| **Core assembly** | `lib/net10.0/FastFsm.dll` (`AssemblyName=FastFsm` on `Fsm.Core`) | Stable reference identity; smoke tests assert `AssemblyVersion` |
| **Abstractions** | Shipped inside `FastFsm.Sharp` (`Abstractions.dll` in `lib/` and `analyzers/`) | Attribute/Fluent API unchanged |
| **Analyzers** | `Generator.dll`, `Generator.Rules.dll`, `Generator.Model.dll`, … in `analyzers/dotnet/cs` | Roslyn loads analyzers by file name next to the entry assembly |
| **Generated code** | Same bases, method names, extension hooks, diagnostics IDs | Machines compile and behave the same |
| **MSBuild in consumer projects** | Same properties (`FsmGenerateLogging`, `FsmGenerateDI`, …) via renamed but equivalent `.props` | Only the **props filename** inside the nupkg changed |

Repository-only renames (`Fsm.Core.csproj`, folder `src/Fsm/…`, test namespaces `Tests.*`) do **not** ship in NuGet and do not affect package consumers.

### Breaking (package / project reference only)

| Change | Migration |
|--------|-----------|
| Canonical package IDs are now `FastFsm.Sharp*` | New projects: `dotnet add package FastFsm.Sharp` (etc.) |
| Old IDs `FastFsm.Net*` | **Optional:** keep `<PackageReference Include="FastFsm.Net" />` — 0.9.1+ ships **legacy metapackages** under `src/LegacyPackages/` that depend on the `FastFsm.Sharp*` packages with no duplicate binaries |
| Props paths in `.csproj` if you referenced them manually | `FastFsm.Net.props` → `FastFsm.Sharp.props`, etc. |

On NuGet.org, deprecate `FastFsm.Net*` 0.9.0 listings with alternative package `FastFsm.Sharp*` when you publish 0.9.1 (manual step in package management UI).

Typical consumer code (`[StateMachine]`, `FSM.State(…)`, `Fire`/`TryFire`, `AddStateMachine<…>`) needs **no edits** whether you migrate references or keep legacy metapackage IDs. `scripts/pack-and-smoke.ps1` verifies both paths.

### What we intentionally did *not* rename in 0.9.1

- Product **namespaces** (`FastFsm.*`, `Abstractions.*`, `Generator.*`) — a mass rename would break every consumer and every generated partial.
- **Analyzer assembly names** — generator pipeline and packed nupkg layout depend on stable DLL names.
- **Test-only namespaces until the test sweep** — then unified to `Tests.*` *inside the repo*; those assemblies are not published.

Future releases should treat the preserved table as the compatibility contract unless a major version explicitly documents a break.

## Repository layout

| Path | Contents |
|------|----------|
| `src/Abstractions/` | Attributes and Fluent API |
| `src/Fsm/Fsm.Core/` | Runtime; packaged as `FastFsm.Sharp` |
| `src/Fsm/Fsm.Logging/`, `src/Fsm/Fsm.DependencyInjection/` | Satellite packages |
| `src/Fsm/Fsm.Tests/` | FSM test projects (`Tests.*`) and shared `Tests.Machines` fixtures |
| `src/Generator/` | Generator projects (`Generator.*`) and `Tests.SourceGenerators` |
| `src/Benchmark/`, `src/IndentedStringBuilder/` | Benchmark harness and generator helper |

Solution folders in `FastFsm.slnx`: `/Fsm/`, `/Fsm/Fsm.Tests/`, `/Generator/` (including generator tests). Standalone projects (`Abstractions`, `Benchmark`, `IndentedStringBuilder`) sit at the solution root without wrapper folders.

## Test layout

| Project | Path | Focus |
|---------|------|-------|
| `Tests.Fsm` | `src/Fsm/Fsm.Tests/Tests.Fsm/` | Core synchronous behavior, extensions, HSM compile tests |
| `Tests.Async` | `src/Fsm/Fsm.Tests/Tests.Async/` | Asynchronous API and HSM |
| `Tests.Logging` | `src/Fsm/Fsm.Tests/Tests.Logging/` | Logging and Attribute/Fluent parity matrix |
| `Tests.DependencyInjection` | `src/Fsm/Fsm.Tests/Tests.DependencyInjection/` | DI registration |
| `Tests.Instance` | `src/Fsm/Fsm.Tests/Tests.Instance/` | Instance-configure / startup |
| `Tests.Machines` | `src/Fsm/Fsm.Tests/Tests.Machines/` | Shared machine definitions referenced by `Tests.Fsm` and `Tests.Logging` |
| `Tests.SourceGenerators` | `src/Generator/Generator.Tests/Tests.SourceGenerators/` | Generator rules and emission tests |

Test code namespaces mirror project names (`Tests.Fsm`, `Tests.Machines`, `Tests.SourceGenerators`, …).

Pack + consumer smoke: `scripts/pack-and-smoke.ps1` / `scripts/pack-and-smoke.sh` (installs only the package under test; satellites must pull Core and Microsoft.Extensions.* transitively). PR CI: GitHub-hosted `.github/workflows/ci.yml`. Trusted self-hosted: `.github/workflows/ci-self-hosted.yml`.

## Maintenance docs

Repository recovery and branch notes: [maintenance/repository-archeology-2026-08.md](maintenance/repository-archeology-2026-08.md).

## 0.9 codebase

- Fluent and attribute configuration use a single generator pipeline
- DI and logging are separate NuGet packages
- Diagnostic IDs use FSM01xx / FSM11xx / FSM20xx / FSM30xx ranges
- `OnTransitioned` is part of `IStateMachineExtension`
