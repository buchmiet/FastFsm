# Changelog

All notable changes to FastFsm are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Repository package version is `FastFsmPackageVersion` / `Version` in `Directory.Build.props`.

## [Unreleased]

## [0.9.2] - 2026-08-21

### Breaking

- Replaced the untyped `IStateMachineExtension` contract with `IStateMachineExtension<TState,TTrigger>`. The previous untyped interface and context types are removed; there is no parallel compatibility surface.

### Added

- **`FastFsm.Sharp.Observability`** — `ActivitySource` tracing, `Meter` metrics, optional `ILogger` sink, and machine-agnostic `ObservabilityEvent` stream via `ObservabilityExtension<TState,TTrigger>`.
- HSM extension benchmarks (`HsmExtensionBenchmarks`: no extensions, transitions-only, states-only).
- Observability benchmarks (`ObservabilityBenchmarks`, `FlatObservabilitySampledTracingBenchmarks`: flat and HSM registration/disabled/metrics/tracing scenarios).
- Documentation for attempt outcomes, async pre-cancel semantics, and [observability.md](docs/observability.md).
- `scripts/run-benchmark-snapshot.ps1` for reproducible packaged-mode benchmark capture.

### Changed

- Extension transition data now distinguishes the active source state, transition-owning state, declared target, resolved target, transition kind, outcome, failure stage, and actual final state.
- HSM state lifecycle hooks exit leaf-to-ancestor and enter ancestor-to-leaf, including ancestor-owned external and self-transitions.
- Extension hook masks are enforced at generator emission sites: transition payloads, guard dispatch, HSM LCA traversal, callback hooks, and attempt completion run only when the corresponding `ExtensionHooks` flag is set.
- Comparison benchmark dependency **Stateless** updated to **5.20.1** (from 5.17.0).
- StateMachine/HSM benchmarks use InProcess jobs (consistent with extension/observability suites; avoids outer-process generator restore failures).
- `global.json` SDK pin relaxed to `10.0.100` with `rollForward: latestMajor` for heterogeneous CI/bench hosts.

### Fixed

- Observability hot path gates string formatting and event construction behind enabled surfaces (metrics-only and no-listener tracing no longer pay for disabled event-stream work).
- Observability DI captures an immutable options snapshot per `(TState,TTrigger)` registration instead of a shared mutable singleton.
- `ObservabilityEvent.Timestamp` is emission time; `AttemptStartTimestamp` correlates events within one attempt.
- Removed redundant `System.Diagnostics.DiagnosticSource` package reference on `net10.0` (NU1510 under `-warnaserror`).
- Benchmark sampled-tracing listener registers with unconditional `ShouldListenTo` and gates work in `Sample`; sampled tracing runs in an isolated benchmark class (`FlatObservabilitySampledTracingBenchmarks`).

## [0.9.1] - 2026-08-17

Published on [NuGet.org](https://www.nuget.org/packages/FastFsm.Sharp). Patch release: packaging, props, CI, and test hygiene after 0.9.0.

### Fixed

- `FastFsm.Sharp.props` adds `Abstractions.dll` for all .NET 10+ TFMs (including `net10.0-windows`), not only `net10.0`.
- `pack-and-smoke` covers `net10.0-windows` consumers for Core, Logging, and DI.
- CI `-warnaserror` no longer fails on intentional `FSM0400` duplicate-transition test fixtures.
- `FastFsm.Sharp` nuspec always includes `Abstractions.dll` and analyzer satellites on clean builds (pack inputs added after dependency build).
- Benchmark project no longer pulls vulnerable `Microsoft.NETCore.Jit` 1.0.2 (replaced `LiquidState` 8.2.0 with `LiquidState.Unofficial` 1.0.6 for comparison benchmarks).
- Async cancellation tests use `OnEntry:InDelay` / `Action:InDelay` synchronization markers instead of fixed delays (stable on macOS CI).

## [0.9.0] - 2026-08-17

Published on [NuGet.org](https://www.nuget.org/packages/FastFsm.Sharp). Targets **.NET 10**, introduces canonical **FastFsm.Sharp*** package IDs, and ships legacy **FastFsm.Net*** metapackages that forward to them.

### Breaking

- **Canonical** NuGet IDs are `FastFsm.Sharp`, `FastFsm.Sharp.Logging`, and `FastFsm.Sharp.DependencyInjection`. New projects should use these IDs.
- Packed MSBuild props filenames: `FastFsm.Net.props` → `FastFsm.Sharp.props`, `FastFsm.Net.Logging.props` → `FastFsm.Sharp.Logging.props`, `FastFsm.Net.DependencyInjection.props` → `FastFsm.Sharp.DependencyInjection.props`. Only affects projects that import props by path manually.

### Added

- Target **.NET 10** (`net10.0`, SDK pin in `global.json`).
- Legacy **metapackages** `FastFsm.Net`, `FastFsm.Net.Logging`, `FastFsm.Net.DependencyInjection` (`src/LegacyPackages/`): same version, dependency-only forward to the matching `FastFsm.Sharp*` package so existing `<PackageReference>` lines keep working without duplicating assemblies or analyzers.
- Canonical product docs under `docs/` (Fluent and Attribute APIs, HSM, async, payloads, extensions, logging, DI, diagnostics, architecture, benchmarks).
- `ROADMAP.md` and deferred-events design proposal.
- .NET 10 benchmark snapshots under `docs/benchmarks/results/` (release gate on `win-x64-amd-9600x` at `93ab811`; `linux-arm64` pre-release retained for comparison).
- GitHub-hosted CI for pull requests; self-hosted Windows / Linux / macOS runners only on `push` to `main` and `workflow_dispatch`.
- `scripts/pack-and-smoke` — pack the three NuGet packages, assert satellite nuspec dependencies and `FastFsm.dll` assembly version, and compile clean consumer consoles that install **only** the package under test.

### Changed

- NuGet package IDs rebranded from `FastFsm.Net*` to `FastFsm.Sharp*` (`FastFsm.Sharp`, `FastFsm.Sharp.Logging`, `FastFsm.Sharp.DependencyInjection`). Build props filenames follow the new package IDs.
- Source projects moved under `src/` with Leith-style solution folder nodes; runtime projects live under `src/Fsm/`, generator under `src/Generator/`.
- Core package assembly name restored to `FastFsm` (`lib/net10.0/FastFsm.dll`); generator entry project renamed to `Generator.Core.csproj` with `AssemblyName` `Generator`.
- Test and product `.csproj` filenames aligned with folder names; `FastFsm.slnx` uses grouped solution folders (`/Fsm/`, `/Fsm/Fsm.Tests/`, `/Generator/`). Test code namespaces unified to `Tests.*` (e.g. `Tests.Fsm`, `Tests.Machines`, `Tests.SourceGenerators`).
- Extension generation is **opt-in** (`GenerateExtensibleVersion = true`). Machines do not emit the extension surface by default.
- Generated user code no longer emits CS0162 / CS0164 / CS0168 / CS0219 / CS8618 on typical HSM and logging paths (`END_TRY_FIRE` and async action ids are conditional).
- NuGet packages aligned to .NET 10 (`Microsoft.Extensions.*` 10.0.11, Roslyn 5.6.0, test SDK 18.9.0).
- Solution file is `FastFsm.slnx` (renamed from `FastFsm.Net.slnx`).
- `FastFsm.Sharp.Logging` and `FastFsm.Sharp.DependencyInjection` declare NuGet dependencies on `FastFsm.Sharp` and `Microsoft.Extensions.*` (content-only packages; NU5128 suppressed). `dotnet add package FastFsm.Sharp.Logging` / `FastFsm.Sharp.DependencyInjection` is sufficient.
- Shared package metadata (license, repository, tags, authors) and `Version=0.9.0` live in `Directory.Build.props` so nupkg and assembly versions match.

### Preserved (consumer API)

- Public C# namespaces unchanged: `Abstractions.*`, `FastFsm.*` (runtime, contracts, DI, exceptions, builder).
- Core package still ships `lib/net10.0/FastFsm.dll` (`AssemblyName=FastFsm`).
- Analyzer DLL names unchanged (`Generator.dll`, `Generator.Rules.dll`, …).
- Attribute/Fluent configuration API, generated machine surface, and diagnostic IDs unchanged vs pre-rebrand development snapshots.

### Fixed

- Generator test harness finds the repo root via `*.slnx`, so `ExtensionRunner.cs` is compiled when testing extensible machines.
- Clean `dotnet pack` of `FastFsm.Sharp` builds `Generator.dll` before `GenerateNuspec` (NU5019).
- Stale analyzer `ProjectReference` edges removed from `FastFsm.csproj`.
- `FastFsm.Sharp` nupkg ships only `ExtensionRunner` and global usings as contentFiles (no duplicate DI types, no double-compile via `.props`).
- `scripts/pack-and-smoke.sh` packs the three product projects only (does not force `GeneratePackageOnBuild` on the generator, which cycles Pack on SDK 10).
- Generator type-format cache timing test no longer uses coarse millisecond ratios on sub-10ms samples.
- FSM2020 / FSM2030 messages point at `[State(..., IsInitial = true)]` / `.IsInitial()`, not a non-existent `[InitialSubstate]` attribute. FSM2010 description no longer truncates at "defined in the.".

### Removed

- Historical scratch trees from the git working set (old tests, multi-language bench experiments, local feed data, `Generator.Tests/old_tests`, tracked BenchmarkDotNet .NET 9 artifacts, local NuGet.config pointing at localhost). A local `archive.zip` may hold a copy; it is not part of the repository.
- Hardcoded parser debug hooks for `AsyncOceOnEntryMachine` / `ContinueOnActionMachine`.

[0.9.2]: https://github.com/buchmiet/FastFsm/releases/tag/v0.9.2
[0.9.1]: https://github.com/buchmiet/FastFsm/releases/tag/v0.9.1
[0.9.0]: https://github.com/buchmiet/FastFsm/releases/tag/v0.9.0
