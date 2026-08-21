# Benchmarks

FastFsm includes a BenchmarkDotNet project at `src/Benchmark/`. Verified results for the **0.9.2 / .NET 10** line are recorded below.

## Running benchmarks locally

Prerequisites:

1. .NET 10 SDK (`global.json` pins `10.0.100` with `rollForward: latestMajor`).
2. For **packaged-mode** runs (consumer-like): a built `FastFsm.Sharp` package in `./nuget` (see `nuget.config`).

The benchmark project inherits `UsePackages=false` from `Directory.Build.props`, so `dotnet build` / CI use project references like the test projects. Pass `-p:UsePackages=true` only when measuring against a local nupkg.

```bash
dotnet build src/Benchmark/Benchmark.csproj -c Release
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-build -- --filter "*"
```

Or on Windows: `src/Benchmark/run.ps1` (packs core, then runs with `UsePackages=true`).

For reproducible snapshot export: `scripts/run-benchmark-snapshot.ps1 -HostLabel win-x64-amd-9600x -CopyToDocs`.

On Linux/macOS, `BenchmarkDotNet.Diagnostics.Windows` is excluded automatically.

BenchmarkDotNet writes results under `BenchmarkDotNet.Artifacts/results/` (relative to the working directory when the benchmark exe runs — typically repo root or `src/Benchmark/`).

## Benchmark coverage

The benchmark sources include:

- flat synchronous transition paths (`StateMachineBenchmarks`)
- hierarchical-state-machine scenarios (`HsmBenchmarks`)
- extension hook overhead (`ExtensionBenchmarks`, `HsmExtensionBenchmarks`)
- observability registration paths (`ObservabilityBenchmarks`, `FlatObservabilitySampledTracingBenchmarks`)
- comparisons with Stateless, LiquidState, and Appccelerate as referenced by `Benchmark.csproj`

All suites use **InProcess** jobs (`IterationCount=15`, `WarmupCount=3`) for reliable builds with source-generator project references.

## Verified results

Host labels (`win-x64-amd-9600x`, `linux-arm64`, …) describe **CPU architecture and OS only** — never machine names or hostnames. See [benchmarks/results/README.md](benchmarks/results/README.md).

### `win-x64-amd-9600x` (2026-08-21, **`v0.9.2` release gate**)

Measured on Windows 11 x64 (AMD Ryzen 5 9600X). Full snapshot: [win-x64-amd-9600x-2026-08-21.md](benchmarks/results/win-x64-amd-9600x-2026-08-21.md)

**Package:** FastFsm.Sharp 0.9.2  
**BenchmarkDotNet:** 0.15.8 — InProcess, `IterationCount=15`, `WarmupCount=3`, runtime .NET 10.0.11  
**Comparison libraries:** Stateless **5.20.1**, LiquidState.Unofficial 1.0.6, Appccelerate.StateMachine 6.0.0

**Flat FSM (selected, Mean):**

| Method | Mean | Alloc/op |
|--------|-----:|---------:|
| FastFsm_Basic | 0.84 ns | 0 |
| LiquidState_Basic | 26.0 ns | 72 B |
| Stateless_Basic | 327.8 ns | 1208 B |
| FastFsm_CanFire | 0.28 ns | 0 |

**HSM (selected, Mean):**

| Method | Mean |
|--------|-----:|
| FastFSM_Hsm_Basic_EnterLeave | 2.60 ns |
| Stateless_Hsm_Basic_EnterLeave | 671.1 ns |
| FastFSM_Hsm_Internal | 0.93 ns |
| Stateless_Hsm_Internal | 383.9 ns |

**Observability flat (512 ops/invoke):**

| Scenario | Mean/op | Alloc/op |
|----------|--------:|---------:|
| Metrics only | 73.8 ns | 0 |
| Tracing, no listener | 79.3 ns | 0 |
| Sampled tracing + listener | 579.6 ns | ~1.26 KB |

### Historical snapshots (0.9.0 / 0.9.1)

Intel Core i5-14600K, measured at commit `b6ed370` on **native Windows** and **WSL2** for a dual-OS view.

| Snapshot | OS | Package | Wall time |
|----------|----|---------|-----------|
| [win-x64-intel-14600k-2026-08-17.md](benchmarks/results/win-x64-intel-14600k-2026-08-17.md) | Windows 11 | 0.9.0 | ~8 min |
| [wsl-x64-intel-14600k-2026-08-17.md](benchmarks/results/wsl-x64-intel-14600k-2026-08-17.md) | WSL2 Ubuntu 24.04 | 0.9.0 | ~7.5 min |

**Flat FSM (selected, Mean):**

| Method | Win native | WSL2 | `win-x64-amd-9600x` | `linux-arm64` |
|--------|----------:|-----:|--------------------:|--------------:|
| FastFsm_Basic | 0.58 ns | 0.64 ns | 0.91 ns | 1.24 ns |
| Stateless_Basic | 293 ns | 445 ns | 234 ns | 437 ns |
| FastFsm_CanFire | 0.36 ns | 0.48 ns | 0.35 ns | 0.54 ns |
| LiquidState_Basic | 26.5 ns | 33.7 ns | 21.2 ns | 25.4 ns |

**HSM (selected, Mean):**

| Method | Win native | WSL2 | `win-x64-amd-9600x` |
|--------|----------:|-----:|--------------------:|
| FastFSM_Hsm_Basic_EnterLeave | 2.68 ns | 2.73 ns | 3.19 ns |
| Stateless_Hsm_Basic_EnterLeave | 765 ns | 1,204 ns | 712 ns |
| FastFSM_Hsm_Internal | 0.97 ns | 0.99 ns | 1.17 ns |

**Takeaways:**

- Generated FastFsm sync paths stay **sub-2 ns** on every x64 host; differences between Windows and WSL on the same CPU are noise-level for `FastFsm_Basic` / `CanFire`.
- Third-party libraries (Stateless, Appccelerate) show **much larger WSL vs Windows gaps** on the i5-14600K pair (e.g. `Stateless_Basic` 293 ns → 445 ns), consistent with WSL2 + DrvFS overhead and different reported core topology.
- `win-x64-amd-9600x` remains the reference **0.9.0 release-gate** snapshot at commit `93ab811`; the Intel/WSL pair at `b6ed370` uses the same benchmark harness and comparison package versions — runtime performance is expected to match.

### `win-x64-amd-9600x` (2026-08-17, `v0.9.0` release gate)

Measured at commit `93ab811` on Windows 11 x64 (AMD Ryzen 5 9600X).

**Package:** FastFsm.Sharp 0.9.0  
**BenchmarkDotNet:** 0.15.8 — `IterationCount=15`, `WarmupCount=3`, `Runtime=.NET 10.0`  
**Comparison libraries:** Stateless 5.17.0, LiquidState.Unofficial 1.0.6 (API-compatible fork without legacy `Microsoft.NETCore.Jit` transitives), Appccelerate.StateMachine 6.0.0

Full tables: [docs/benchmarks/results/win-x64-amd-9600x-2026-08-17.md](benchmarks/results/win-x64-amd-9600x-2026-08-17.md)

| Field | Value |
|-------|-------|
| Host label | `win-x64-amd-9600x` |
| OS | Windows 11 (10.0.26200) |
| CPU | AMD Ryzen 5 9600X, 6 physical cores |
| .NET | SDK 10.0.400, runtime 10.0.11 |
| Wall time | ~9.5 min total (23 + 7 benchmarks) |

**Flat FSM (selected, vs Stateless baseline = 1.00 where shown):**

| Method | Mean | Ratio vs Stateless |
|--------|-----:|-------------------:|
| FastFsm_Basic | 0.91 ns | 0.004 |
| Stateless_Basic | 234.0 ns | 1.000 |
| LiquidState_Basic | 21.2 ns | 0.091 |
| FastFsm_GuardsActions | 0.87 ns | 0.004 |
| FastFsm_CanFire | 0.35 ns | 0.001 |
| FastFsm_Payload | 1.04 ns | 0.004 |

**HSM (selected):**

| Method | Mean |
|--------|-----:|
| FastFSM_Hsm_Basic_EnterLeave | 3.19 ns |
| Stateless_Hsm_Basic_EnterLeave | 711.6 ns |
| FastFSM_Hsm_Internal | 1.17 ns |
| Stateless_Hsm_Internal | 290.0 ns |
| FastFSM_Hsm_History_Shallow | 35.0 ns |

On this x64 Windows host, generated `switch`-based dispatch is fastest on simple transition paths; LiquidState is competitive on several async scenarios. Interpret ratios only together with allocation columns in the full report.

### `linux-arm64` (pre-release)

Earlier ARM64 snapshot at `548ea01` (pre-release): [linux-arm64-2026-08-16.md](benchmarks/results/linux-arm64-2026-08-16.md).

### Pending 0.9.2 multi-platform snapshots

| Host label | Platform | Status |
|------------|----------|--------|
| `win-arm64` | Windows 11 ARM64 (Orange Pi) | observability complete — [snapshot](benchmarks/results/win-arm64-2026-08-21.md); full comparison pending disassembly fix |
| `linux-arm64` | homelab Ubuntu aarch64 | blocked: Roslyn source generator does not emit benchmark machines on Linux (investigate separately) |
| `macos-arm64` | Apple Silicon | pending (long restore over SSH; rerun locally on Mac) |

Run on each host:

```bash
dotnet build src/Benchmark/Benchmark.csproj -c Release
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-build -- --filter "*"
```

On macOS, ensure `~/.dotnet` is on `PATH`.

## Publishing benchmark results

When adding new numbers:

1. Run BenchmarkDotNet on the target framework and the commit being released.
2. Record hardware (CPU/OS only — use a neutral host label, not machine names), .NET SDK/runtime version, commit SHA, and benchmark configuration.
3. Record the package versions used for comparison libraries.
4. Add a dated snapshot under `docs/benchmarks/results/` and summarize here.
5. Attach raw BenchmarkDotNet HTML/CSV to the corresponding GitHub Release or CI artifact rather than committing generated logs to the repository.

Performance statements should distinguish measured results from implementation properties. A measured latency or allocation result should identify the benchmark and environment that produced it.

## Implementation properties relevant to benchmarking

The following describe the implementation rather than measured performance:

- generated transition dispatch uses enum-based `switch` statements
- asynchronous callback paths use `ValueTask`
- optional extension and logging paths execute additional hooks when enabled

Application-level performance depends on the state-machine definition, callbacks, logging, extensions, runtime, and workload. Measure the configuration used by the application when performance is a requirement.
