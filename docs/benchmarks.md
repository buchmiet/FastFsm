# Benchmarks

FastFsm includes a BenchmarkDotNet project at `Benchmark/`. **The repository does not currently ship a verified .NET 10 benchmark report suitable for marketing claims.**

## Committed historical run

Raw output from August 2025 on **.NET 9.0.5** is archived at:

[archive/benchmarks/net9-2025/benchresult.txt](archive/benchmarks/net9-2025/benchresult.txt)

That file was produced on a Windows host (see header in the file). Do not cite its numbers as .NET 10 results.

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
3. Store the raw `.txt` / `.md` artifact under `docs/archive/benchmarks/<label>/` or attach to the release.
4. Describe methodology (warmup, iteration count, what “hot path” means) in the same document.

Avoid absolute claims (“sub-nanosecond guarantees”, “near hardware-level”, “100% AOT”) unless each claim maps to a reproducible measurement or a specific technical guarantee (e.g. “generated code uses enum switches without reflection”).

## Performance characteristics (qualitative)

These are architectural properties, not benchmark results:

- Transitions compile to direct `switch` on enum discriminators
- Sync hot paths avoid heap allocations for transition dispatch
- Async paths use `ValueTask` to reduce task allocations
- Extension and logging hooks add cost proportional to registered extensions / log level

Measure your own workload — domain actions (I/O, logging, extension logic) usually dominate once transition dispatch is already in the low tens of nanoseconds.
