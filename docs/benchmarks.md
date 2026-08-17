# Benchmarks

FastFsm includes a BenchmarkDotNet project at `src/Benchmark/`. Verified results for the **0.9 / .NET 10** line are recorded below.

## Running benchmarks locally

Prerequisites:

1. .NET 10 SDK (`global.json` pins 10.0.400).
2. A built `FastFsm.Sharp` package in `./nuget` (the benchmark project uses `UsePackages=true`).

```bash
dotnet build src/Fsm/Fsm.Core/Fsm.Core.csproj -c Release
dotnet run -c Release --project src/Benchmark/Benchmark.csproj
```

On Linux/macOS, `BenchmarkDotNet.Diagnostics.Windows` is excluded automatically.

BenchmarkDotNet writes results under `BenchmarkDotNet.Artifacts/results/` (under `src/Benchmark/` when run from that project directory).

## Benchmark coverage

The benchmark sources include:

- flat synchronous transition paths (`StateMachineBenchmarks`)
- hierarchical-state-machine scenarios (`HsmBenchmarks`)
- comparisons with Stateless, LiquidState, and Appccelerate as referenced by `Benchmark.csproj`

## Verified results (2026-08-17, `v0.9.0` release gate)

Measured at commit `93ab811` on the Windows x64 workstation (AMD Ryzen 5 9600X) (Windows 11, AMD Ryzen 5 9600X x64).

**Package:** FastFsm.Sharp 0.9.0  
**BenchmarkDotNet:** 0.15.8 — `IterationCount=15`, `WarmupCount=3`, `Runtime=.NET 10.0`  
**Comparison libraries:** Stateless 5.17.0, LiquidState 8.2.0, Appccelerate.StateMachine 6.0.0

Full tables: [docs/benchmarks/results/win-x64-amd-9600x-2026-08-17.md](benchmarks/results/win-x64-amd-9600x-2026-08-17.md)

### win-x64-amd-9600x

| Field | Value |
|-------|-------|
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

### linux-arm64 (, pre-release)

Earlier ARM64 snapshot at `548ea01` (pre-release): [linux-arm64-2026-08-16.md](benchmarks/results/linux-arm64-2026-08-16.md).

### macOS (Apple Silicon Mac)

**Status:** not measured yet. `dotnet restore` for the benchmark graph is killed by the OS (SIGKILL / memory pressure) over SSH even with ~17 GiB disk free. SDK 10.0.400 is installed at `~/.dotnet`; a pre-seeded NuGet cache was copied from another ARM64 host. Re-run locally after closing memory-heavy apps:

```bash
export PATH="$HOME/.dotnet:$PATH"
export NUGET_PACKAGES="$HOME/source/repos/FastFsm.Sharp/.packages"
cd ~/source/repos/FastFsm.Sharp
dotnet build src/Benchmark/Benchmark.csproj -c Release
dotnet run -c Release --project src/Benchmark/Benchmark.csproj --no-build
```

## Publishing benchmark results

When adding new numbers:

1. Run BenchmarkDotNet on the target framework and the commit being released.
2. Record hardware, operating system, .NET SDK/runtime version, commit SHA, and benchmark configuration.
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
