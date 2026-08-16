# Benchmarks

FastFsm includes a BenchmarkDotNet project at `Benchmark/`. **The repository does not ship verified benchmark numbers for 0.9 / .NET 10.** Do not cite latency figures in user-facing docs until you have run benchmarks locally and recorded the environment.

## Running benchmarks locally

Prerequisites: .NET 10 SDK, Release configuration recommended.

```bash
dotnet run -c Release --project Benchmark/Benchmark.csproj
```

Results are written under `Benchmark/BenchmarkDotNet.Artifacts/`. Review `*.md` reports in that folder after a run.

### What the benchmark project covers

See `Benchmark/` sources:

- Flat sync transition hot paths (`StateMachineBenchmarks`)
- HSM scenarios (`HsmBenchmarks`)
- Comparisons against Stateless, LiquidState, Appccelerate (package references in `Benchmark.csproj`)

## Publishing numbers in documentation

Before quoting latency in README or release notes:

1. Run BenchmarkDotNet on the **target** framework (.NET 10).
2. Record hardware, SDK version, commit SHA, and configuration (Release, server GC, etc.).
3. Summarize methodology and headline results in this document.
4. Attach raw BenchmarkDotNet output to a **GitHub Release** or CI artifact — not as a committed log in the repo.

Avoid absolute claims (“sub-nanosecond guarantees”, “near hardware-level”, “100% AOT”) unless each claim maps to a reproducible measurement or a specific technical guarantee (e.g. “generated code uses enum switches without reflection”).

## Performance characteristics (qualitative)

These are architectural properties, not benchmark results:

- Transitions compile to direct `switch` on enum discriminators
- Sync hot paths avoid heap allocations for transition dispatch
- Async paths use `ValueTask` to reduce task allocations
- Extension and logging hooks add cost proportional to registered extensions / log level

Measure your own workload — domain actions (I/O, logging, extension logic) usually dominate once transition dispatch is already in the low tens of nanoseconds.
