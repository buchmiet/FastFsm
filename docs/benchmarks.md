# Benchmarks

FastFsm includes a BenchmarkDotNet project at `Benchmark/`. The repository does not currently publish verified benchmark results for the 0.9 / .NET 10 line.

## Running benchmarks locally

Prerequisite: .NET 10 SDK.

```bash
dotnet run -c Release --project Benchmark/Benchmark.csproj
```

BenchmarkDotNet writes results under `Benchmark/BenchmarkDotNet.Artifacts/`.

## Benchmark coverage

The benchmark sources include:

- flat synchronous transition paths (`StateMachineBenchmarks`)
- hierarchical-state-machine scenarios (`HsmBenchmarks`)
- comparisons with Stateless, LiquidState, and Appccelerate as referenced by `Benchmark.csproj`

## Publishing benchmark results

Before adding performance numbers to README or release notes:

1. Run BenchmarkDotNet on the target framework and the commit being released.
2. Record hardware, operating system, .NET SDK/runtime version, commit SHA, and benchmark configuration.
3. Record the package versions used for comparison libraries.
4. Summarize the methodology and results in this document.
5. Attach raw BenchmarkDotNet output to the corresponding GitHub Release or CI artifact rather than committing generated logs to the repository.

Performance statements should distinguish measured results from implementation properties. A measured latency or allocation result should identify the benchmark and environment that produced it.

## Implementation properties relevant to benchmarking

The following describe the implementation rather than measured performance:

- generated transition dispatch uses enum-based `switch` statements
- asynchronous callback paths use `ValueTask`
- optional extension and logging paths execute additional hooks when enabled

Application-level performance depends on the state-machine definition, callbacks, logging, extensions, runtime, and workload. Measure the configuration used by the application when performance is a requirement.
