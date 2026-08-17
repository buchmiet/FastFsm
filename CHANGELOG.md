# Changelog

All notable changes to FastFsm are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Repository package version is `FastFsmPackageVersion` / `Version` in `Directory.Build.props`.

## [Unreleased]

## [0.9.1] - 2026-08-17

Not yet published to NuGet. NuGet `0.9.0` remains the latest release under the `FastFsm.Net*` package IDs.

### Breaking

- **Canonical** NuGet IDs are `FastFsm.Sharp`, `FastFsm.Sharp.Logging`, and `FastFsm.Sharp.DependencyInjection`. New projects should use these IDs.
- Packed MSBuild props filenames: `FastFsm.Net.props` → `FastFsm.Sharp.props`, `FastFsm.Net.Logging.props` → `FastFsm.Sharp.Logging.props`, `FastFsm.Net.DependencyInjection.props` → `FastFsm.Sharp.DependencyInjection.props`. Only affects projects that import props by path manually.

### Added

- Legacy **metapackages** `FastFsm.Net`, `FastFsm.Net.Logging`, `FastFsm.Net.DependencyInjection` (`src/LegacyPackages/`): same version, dependency-only forward to the matching `FastFsm.Sharp*` package so existing `<PackageReference>` lines keep working without duplicating assemblies or analyzers.

### Changed

- NuGet package IDs rebranded from `FastFsm.Net*` to `FastFsm.Sharp*` (`FastFsm.Sharp`, `FastFsm.Sharp.Logging`, `FastFsm.Sharp.DependencyInjection`). Build props filenames follow the new package IDs.
- Source projects moved under `src/` with Leith-style solution folder nodes; runtime projects live under `src/Fsm/`, generator under `src/Generator/`.
- Core package assembly name restored to `FastFsm` (`lib/net10.0/FastFsm.dll`); generator entry project renamed to `Generator.Core.csproj` with `AssemblyName` `Generator`.
- Test and product `.csproj` filenames aligned with folder names; `FastFsm.slnx` uses grouped solution folders (`/Fsm/`, `/Fsm/Fsm.Tests/`, `/Generator/`). Test code namespaces unified to `Tests.*` (e.g. `Tests.Fsm`, `Tests.Machines`, `Tests.SourceGenerators`).

### Preserved (consumer API)

- Public C# namespaces unchanged: `Abstractions.*`, `FastFsm.*` (runtime, contracts, DI, exceptions, builder).
- Core package still ships `lib/net10.0/FastFsm.dll` (`AssemblyName=FastFsm`).
- Analyzer DLL names unchanged (`Generator.dll`, `Generator.Rules.dll`, …).
- Attribute/Fluent configuration API, generated machine surface, and diagnostic IDs unchanged vs 0.9.0.

## [0.9.0] - 2026-08-17

First public-facing 0.9 line on .NET 10. Packages: `FastFsm.Net`, `FastFsm.Net.Logging`, `FastFsm.Net.DependencyInjection`.

### Added

- Target **.NET 10** (`net10.0`, SDK pin in `global.json`).
- Canonical product docs under `docs/` (Fluent and Attribute APIs, HSM, async, payloads, extensions, logging, DI, diagnostics, architecture, benchmarks).
- `ROADMAP.md` and deferred-events design proposal.
- .NET 10 benchmark snapshots under `docs/benchmarks/results/` (release gate on `win-x64-amd-9600x` at `93ab811`; `linux-arm64` pre-release retained for comparison).
- GitHub-hosted CI for pull requests; self-hosted Windows / Linux / macOS runners only on `push` to `main` and `workflow_dispatch`.
- `scripts/pack-and-smoke` — pack the three NuGet packages, assert satellite nuspec dependencies and `FastFsm.dll` assembly version, and compile clean consumer consoles that install **only** the package under test.

### Changed

- Extension generation is **opt-in** (`GenerateExtensibleVersion = true`). Machines do not emit the extension surface by default.
- Generated user code no longer emits CS0162 / CS0164 / CS0168 / CS0219 / CS8618 on typical HSM and logging paths (`END_TRY_FIRE` and async action ids are conditional).
- NuGet packages aligned to .NET 10 (`Microsoft.Extensions.*` 10.0.11, Roslyn 5.6.0, test SDK 18.9.0).
- Solution file is `FastFsm.slnx` (renamed from `FastFsm.Net.slnx`).
- `FastFsm.Net.Logging` and `FastFsm.Net.DependencyInjection` declare NuGet dependencies on `FastFsm.Net` and `Microsoft.Extensions.*` (content-only packages; NU5128 suppressed). `dotnet add package FastFsm.Net.Logging` / `FastFsm.Net.DependencyInjection` is sufficient.
- Shared package metadata (license, repository, tags, authors) and `Version=0.9.0` live in `Directory.Build.props` so nupkg and assembly versions match.

### Fixed

- Generator test harness finds the repo root via `*.slnx`, so `ExtensionRunner.cs` is compiled when testing extensible machines.
- Clean `dotnet pack` of `FastFsm.Net` builds `Generator.dll` before `GenerateNuspec` (NU5019).
- Stale analyzer `ProjectReference` edges removed from `FastFsm.csproj`.
- `FastFsm.Net` nupkg ships only `ExtensionRunner` and global usings as contentFiles (no duplicate DI types, no double-compile via `.props`).
- `scripts/pack-and-smoke.sh` packs the three product projects only (does not force `GeneratePackageOnBuild` on the generator, which cycles Pack on SDK 10).
- Generator type-format cache timing test no longer uses coarse millisecond ratios on sub-10ms samples.
- FSM2020 / FSM2030 messages point at `[State(..., IsInitial = true)]` / `.IsInitial()`, not a non-existent `[InitialSubstate]` attribute. FSM2010 description no longer truncates at "defined in the.".

### Removed

- Historical scratch trees from the git working set (old tests, multi-language bench experiments, local feed data, `Generator.Tests/old_tests`, tracked BenchmarkDotNet .NET 9 artifacts, local NuGet.config pointing at localhost). A local `archive.zip` may hold a copy; it is not part of the repository.
- Hardcoded parser debug hooks for `AsyncOceOnEntryMachine` / `ContinueOnActionMachine`.

[0.9.1]: https://github.com/buchmiet/FastFsm/compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/buchmiet/FastFsm/releases/tag/v0.9.0
