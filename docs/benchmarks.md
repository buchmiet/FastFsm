# Benchmarks

FastFsm includes a BenchmarkDotNet project at `src/Benchmark/`. Verified results for the **0.9.2 / .NET 10** line are recorded below.

## Scope and interpretation

These benchmarks measure framework overhead on small, synthetic state-machine workloads. They are microbenchmarks, not application-level throughput measurements.

FastFsm is source-generated, while the comparison libraries are configured and dispatched at runtime. The benchmark tables report the measured cost of those implementations on the same workloads; they do not normalize the architectural differences into a single "faster than" ratio.

Results reported with `OperationsPerInvoke` are per-operation values from batched BenchmarkDotNet invocations. Sub-nanosecond figures should therefore be interpreted as amortized microbenchmark cost, not as standalone wall-clock latency of an isolated transition.

Use the full snapshots when comparing implementations. They include allocation data, async scenarios, runtime and hardware details, package versions, and benchmark configuration.

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

### `win-x64-amd-9600x` (2026-08-21, `v0.9.2` release gate)

Measured on Windows 11 x64 (AMD Ryzen 5 9600X). Full snapshot: [win-x64-amd-9600x-2026-08-21.md](benchmarks/results/win-x64-amd-9600x-2026-08-21.md)

**Package:** FastFsm.Sharp 0.9.2  
**BenchmarkDotNet:** 0.15.8 — InProcess, `IterationCount=15`, `WarmupCount=3`, runtime .NET 10.0.11  
**Comparison libraries:** Stateless 5.20.1, LiquidState.Unofficial 1.0.6, Appccelerate.StateMachine 6.0.0

The tables below are a compact index into the snapshot. The linked snapshot contains the broader benchmark set.

**Flat synchronous dispatch:**

| Method | Mean | Alloc/op |
|--------|-----:|---------:|
| FastFsm_Basic | 0.84 ns | 0 |
| LiquidState_Basic | 26.0 ns | 72 B |
| Stateless_Basic | 327.8 ns | 1208 B |
| Appccelerate_Basic | 331.7 ns | 1568 B |

**HSM:**

| Method | Mean | Alloc/op |
|--------|-----:|---------:|
| FastFSM_Hsm_Basic_EnterLeave | 2.60 ns | 0 |
| Stateless_Hsm_Basic_EnterLeave | 671.1 ns | 3520 B |
| FastFSM_Hsm_Internal | 0.93 ns | 0 |
| Stateless_Hsm_Internal | 383.9 ns | 1424 B |

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

**Flat FSM:**

| Method | Win native | WSL2 | `win-x64-amd-9600x` | `linux-arm64` |
|--------|----------:|-----:|--------------------:|--------------:|
| FastFsm_Basic | 0.58 ns | 0.64 ns | 0.91 ns | 1.24 ns |
| Stateless_Basic | 293 ns | 445 ns | 234 ns | 437 ns |
| FastFsm_CanFire | 0.36 ns | 0.48 ns | 0.35 ns | 0.54 ns |
| LiquidState_Basic | 26.5 ns | 33.7 ns | 21.2 ns | 25.4 ns |

**HSM:**

| Method | Win native | WSL2 | `win-x64-amd-9600x` |
|--------|----------:|-----:|--------------------:|
| FastFSM_Hsm_Basic_EnterLeave | 2.68 ns | 2.73 ns | 3.19 ns |
| Stateless_Hsm_Basic_EnterLeave | 765 ns | 1,204 ns | 712 ns |
| FastFSM_Hsm_Internal | 0.97 ns | 0.99 ns | 1.17 ns |

Cross-run comparisons should be treated separately from same-run comparisons because the job mode, runtime, package versions, and host can differ. The dated snapshots preserve those details.

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

**Flat FSM:**

| Method | Mean |
|--------|-----:|
| FastFsm_Basic | 0.91 ns |
| Stateless_Basic | 234.0 ns |
| LiquidState_Basic | 21.2 ns |
| FastFsm_GuardsActions | 0.87 ns |
| FastFsm_CanFire | 0.35 ns |
| FastFsm_Payload | 1.04 ns |

**HSM:**

| Method | Mean |
|--------|-----:|
| FastFSM_Hsm_Basic_EnterLeave | 3.19 ns |
| Stateless_Hsm_Basic_EnterLeave | 711.6 ns |
| FastFSM_Hsm_Internal | 1.17 ns |
| Stateless_Hsm_Internal | 290.0 ns |
| FastFSM_Hsm_History_Shallow | 35.0 ns |

The full report contains the async, payload, helper, allocation, and code-size results for the same run.

### `linux-arm64` (pre-release)

Earlier ARM64 snapshot at `548ea01` (pre-release): [linux-arm64-2026-08-16.md](benchmarks/results/linux-arm64-2026-08-16.md).

### Pending 0.9.2 multi-platform snapshots

| Host label | Platform | Status |
|------------|----------|--------|
| `win-arm64` | Windows 11 ARM64 (Orange Pi) | observability snapshot available — [snapshot](benchmarks/results/win-arm64-2026-08-21.md); full comparison pending |
| `linux-arm64` | Ubuntu aarch64 | benchmark generator issue under investigation |
| `macos-arm64` | Apple Silicon | pending |

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

Avoid converting individual microbenchmark rows into general library-wide speedup claims. If a ratio is useful for analysis, derive it from values produced by the same run and keep the benchmark scope explicit.

## Implementation properties relevant to benchmarking

The following describe the implementation rather than measured performance:

- generated transition dispatch uses enum-based `switch` statements
- asynchronous callback paths use `ValueTask`
- optional extension and logging paths execute additional hooks when enabled

Application-level performance depends on the state-machine definition, callbacks, logging, extensions, runtime, and workload. Measure the configuration used by the application when performance is a requirement.
