# Archive

Historical material kept for reference. **Not** part of the active build or test solution (`FastFsm.Net.slnx`).

## Layout

| Path | Contents |
|------|----------|
| `oldtests/` | Pre-recovery test suite and legacy machine definitions |
| `benchmarks-legacy/` | Multi-language benchmark experiments (C++, Go, Java, JS, Rust, TypeScript) |
| `examples/` | Sample apps and PoCs (`Example.*`, `FastFsmDemo`, `Syntax.Test`, `StringEnumPoC`) |
| `gentest/` | Roslyn generator CLI test harness |
| `parser-comparison/` | Parser development infrastructure (`ParserComparison.Tests`) |
| `dev-scripts/` | Ad-hoc Python scripts and one-off test utilities |
| `dev-infra/` | Local ProGet/Nexus `docker-compose.yml` (data dirs are not in git) |
| `tui/` | Terminal UI experiments (Python) |
| `workingen/` | Generator exploration notes and scratch code |
| `misc/` | ASCII art, NDJSON exports, scratch `.cs` files |

## Active equivalents on `main`

- .NET benchmarks: `Benchmark/` (BenchmarkDotNet, documented in `docs/benchmarks.md`)
- Tests: `FastFsm.Tests`, `FastFsm.Async.Tests`, `Machines.Tests`, `Generator.Tests`, …
- Docs: `docs/` (product guides only)
