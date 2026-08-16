# Changelog

All notable changes to FastFsm are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Repository package version is `FastFsmPackageVersion` in `Directory.Build.props`.

## [0.9.0] - 2026-08-17

First public-facing 0.9 line on .NET 10. Packages: `FastFsm.Net`, `FastFsm.Net.Logging`, `FastFsm.Net.DependencyInjection`.

### Added

- Target **.NET 10** (`net10.0`, SDK pin in `global.json`).
- Canonical product docs under `docs/` (Fluent and Attribute APIs, HSM, async, payloads, extensions, logging, DI, diagnostics, architecture, benchmarks).
- `ROADMAP.md` and deferred-events design proposal.
- Homelab .NET 10 benchmark results (`docs/benchmarks/results/`).
- CI on self-hosted Windows (Cray), Linux (homelab), and macOS (homelab).
- `scripts/pack-and-smoke` — pack the three NuGet packages and compile clean consumer consoles against the local feed.

### Changed

- Extension generation is **opt-in** (`GenerateExtensibleVersion = true`). Machines do not emit the extension surface by default.
- Generated user code no longer emits CS0162 / CS0164 / CS0168 / CS0219 / CS8618 on typical HSM and logging paths (`END_TRY_FIRE` and async action ids are conditional).
- NuGet packages aligned to .NET 10 (`Microsoft.Extensions.*` 10.0.11, Roslyn 5.6.0, test SDK 18.9.0).
- Solution file is `FastFsm.slnx` (renamed from `FastFsm.Net.slnx`).
- Logging and DI packages set `SuppressDependenciesWhenPacking` so nupkgs do not warn NU5128.

### Fixed

- Generator test harness finds the repo root via `*.slnx`, so `ExtensionRunner.cs` is compiled when testing extensible machines.
- Clean `dotnet pack` of `FastFsm.Net` builds `Generator.dll` before `GenerateNuspec` (NU5019).
- Stale analyzer `ProjectReference` edges removed from `FastFsm.csproj`.
- `FastFsm.Net` nupkg ships only `ExtensionRunner` and global usings as contentFiles (no duplicate DI types, no double-compile via `.props`).

### Removed

- Historical scratch trees from the git working set (old tests, multi-language bench experiments, local feed data). A local `archive.zip` may hold a copy; it is not part of the repository.

[0.9.0]: https://github.com/buchmiet/FastFsm/releases/tag/v0.9.0
