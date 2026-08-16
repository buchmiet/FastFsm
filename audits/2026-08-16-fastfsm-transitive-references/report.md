# Transitive Project-Reference Review — FastFsm

Date: 2026-08-16 | Baseline: `main` (post package-update commit)  
Status: review — advisory unless marked binding  
Parameters:
- Exclusions: `archive/`, `bin/`, `obj/`
- Production: packable/runtime + generator stack (not `*Tests*`, not `Benchmark/`)
- Key projects: `FastFsm`, `FastFsm.Logging`, `FastFsm.DependencyInjection`, `Generator`, `Abstractions`

---

## 1. Metrics

Corpus: **18** projects on disk excluding `archive/` (**10** production, **7** tests, **1** sample/benchmark host).

| Project | Direct refs | Transitive closure | Max depth | Δ vs prior run |
|---------|-------------|-------------------|-----------|----------------|
| `Abstractions/Abstractions.csproj` | 0 | 1 | 0 | — (first run) |
| `IndentedStringBuilder/IndentedStringBuilder.csproj` | 0 | 1 | 0 | — |
| `Generator.Model/Generator.Model.csproj` | 0 | 1 | 0 | — |
| `Generator.Rules/Generator.Rules.csproj` | 0 | 1 | 0 | — |
| `Generator.Logger/Generator.Logger.csproj` | 1 | 2 | 1 | — |
| `Generator.DependencyInjection/Generator.DependencyInjection.csproj` | 2 | 3 | 1 | — |
| `FastFsm/FastFsm.csproj` | 4 | 6 | 1 | — |
| `Generator/Generator.csproj` | 6 | 7 | 2 | — |
| `FastFsm.Logging/FastFsm.Logging.csproj` | 2 | 7 | 2 | — |
| `FastFsm.DependencyInjection/FastFsm.DependencyInjection.csproj` | 2 | 7 | 2 | — |

**Binding checks:** cycles = **none**; production → test/spike edges = **none**.

**Hubs (incoming production edges):** `IndentedStringBuilder` (4), `Abstractions` (4), `FastFsm` (2), `Generator.Model` (2), `Generator.DependencyInjection` (2), `Generator.Logger` (2).

**Implicit edges:** `eng/FastFsmReferences.props` injects `ProjectReference` items when `FastFsmReferenceCore`, `FastFsmReferenceLogging`, or `FastFsmReferenceDependencyInjection` are set. These are included in the metrics above.

---

## 2. Findings (ordered by consequence)

### F-1 — Stale analyzer `ProjectReference` edges on `FastFsm.csproj` (P2, verified)

**What:** `FastFsm/FastFsm.csproj` declares three analyzer-only `ProjectReference` edges that are not required for compilation:

| Removed edge | Verification |
|--------------|--------------|
| `FastFsm` → `Generator.Logger` | `dotnet build FastFsm/FastFsm.csproj -c Release` — **0 errors** |
| `FastFsm` → `Generator.DependencyInjection` | same — **0 errors** |
| `FastFsm` → `IndentedStringBuilder` | same — **0 errors** |

**How:** Remove the three `<ProjectReference … OutputItemType="Analyzer" …>` lines from `FastFsm.csproj`. Keep `Abstractions` (runtime). Ensure `dotnet pack` still finds analyzer DLLs via existing `None Include="..\Generator\bin\…"` conditions (build `Generator` first in CI/pack script if needed).

**Why:** The compiler is the arbiter ([review-procedure.md](../../../../audits/transitive-references/compendium/references/review-procedure.md) § Stale-edge sweep). These edges add graph noise and can force redundant rebuild ordering for `FastFsm` itself. Runtime compilation of `FastFsm` does not execute source generators; analyzers are delivered to **consumers** via the packed nupkg and via `Directory.Build.targets` (`AddFastFsmDevAnalyzers`) in dev mode.

### F-2 — Solution / disk set drift: `Benchmark/` (P2)

**What:** `Benchmark/Benchmark.csproj` exists on disk (uses `FastFsmReferenceCore` + `UsePackages=true`) but is **not** listed in `FastFsm.slnx` (16 projects).

**How:** Either add `Benchmark/Benchmark.csproj` to `FastFsm.slnx` or document in `README.md` / `docs/benchmarks.md` that benchmarks are built standalone (`dotnet run -c Release --project Benchmark`).

**Why:** Solution membership and on-disk projects drift ([optimal-layout.md](../../../../audits/transitive-references/compendium/references/optimal-layout.md) § Set coherence). CI and contributors may assume `dotnet build FastFsm.slnx` covers all active hosts.

### F-3 — Deliberate dual path to `Abstractions` for satellite packages (not a defect — documented)

**What:** `FastFsm.Logging` and `FastFsm.DependencyInjection` receive **both** `FastFsm` and `Abstractions` via `eng/FastFsmReferences.props`. Removing the direct `Abstractions` edge breaks the build (`CS0246` on `GlobalUsings.FastFsm.cs` / `ExtensionRunner.cs` when compiled as content into satellite packages).

**Why:** `FastFsm` references `Abstractions` with `PrivateAssets="all"`, so `Abstractions` does not flow transitively to consumers ([graph-and-metrics.md](../../../../audits/transitive-references/compendium/references/graph-and-metrics.md) § Redundant ≠ stale). The direct `Abstractions` edge is **redundant-but-used** — keep it.

### F-4 — Test fixture hub: `Machines.Tests` (P3, advisory)

**What:** `FastFsm.Tests` and `FastFsm.Logging.Tests` reference `Machines.Tests/Machines.Tests.csproj` for shared machine definitions. `Machines.Tests` itself uses `FastFsmReferenceCore` (closure: `FastFsm` + `Abstractions`).

**How:** No change required. Optional drop-down: reference only `Machines.Tests` from tests that need generated machines; already the case.

**Why:** Test corpus correctly hangs off production; no production → test edge. Hub pattern is acceptable for fixture libraries ([decision-principles.md](../../../../audits/transitive-references/compendium/references/decision-principles.md) § Hub vs leaf).

### F-5 — Namespace / project name misalignment in tests (P3)

**What:** DI tests live under namespaces such as `StateMachine.Tests.DI` and `StateMachine.DependencyInjection` while projects are named `FastFsm.*`.

**How:** Align namespaces when touching those files, or document the historical `StateMachine` prefix in test README. No graph change.

**Why:** Project name ≠ root namespace is a known false-positive trap ([graph-and-metrics.md](../../../../audits/transitive-references/compendium/references/graph-and-metrics.md) § False-positive traps). Pure naming; does not affect the production DAG.

---

## 3. Stale-edge sweep

| Referencing project | Removed reference | Verification | Note |
|---------------------|-------------------|--------------|------|
| `FastFsm/FastFsm.csproj` | → `Generator.Logger` (analyzer) | Builds clean | Stale for compile |
| `FastFsm/FastFsm.csproj` | → `Generator.DependencyInjection` (analyzer) | Builds clean | Stale for compile |
| `FastFsm/FastFsm.csproj` | → `IndentedStringBuilder` (analyzer) | Builds clean | Stale for compile |
| `eng/FastFsmReferences.props` (via `FastFsm.Logging`) | → `Abstractions` | **Build fails** | Required — `PrivateAssets` on core ref |

**Unverified scan candidates:** none with high confidence beyond the table above. Generator sub-project edges (`Generator` → `Generator.Rules`, `Generator.Logger`, etc.) all have namespace usage in `Generator/` sources.

---

## 4. To-Be reference layout (proposal)

### Observed As-Is (production)

```text
CONTRACTS (leaves)
  Abstractions, IndentedStringBuilder, Generator.Model, Generator.Rules

GENERATOR ENGINE
  Generator.DependencyInjection, Generator.Logger → …
  Generator → Abstractions + generator modules

RUNTIME
  FastFsm → Abstractions (+ stale analyzer edges to generator modules)

COMPOSITION PACKAGES
  FastFsm.Logging → FastFsm + Abstractions
  FastFsm.DependencyInjection → FastFsm + Abstractions
```

Direction is monotone: no upward edges. No production project references test projects.

### Proposed To-Be

Same layering; trim stale edges on `FastFsm.csproj` only.

| Action | Edge | Wave | Status |
|--------|------|------|--------|
| drop | `FastFsm` → `Generator.Logger` (analyzer PR) | 1 | verified stale |
| drop | `FastFsm` → `Generator.DependencyInjection` (analyzer PR) | 1 | verified stale |
| drop | `FastFsm` → `IndentedStringBuilder` (analyzer PR) | 1 | verified stale |
| keep (deliberate) | `FastFsm.Logging` → `Abstractions` | — | required (PrivateAssets) |
| keep (deliberate) | `FastFsm.DependencyInjection` → `Abstractions` | — | required (PrivateAssets) |
| add to sln or document | `Benchmark/` | 2 | set coherence |

**Named exceptions:** Analyzer DLLs ship via packed `None` items + `Directory.Build.targets` dev wiring — not via compile-time `ProjectReference` on `FastFsm` itself.

---

## 5. Suggested guards (conservative)

**Binding (red-tested first):**
- Production `ProjectReference` graph must remain acyclic (MSBuild already enforces; optional unit test from [ProjectReferenceAdvisoryTests.cs](../../../../audits/transitive-references/skill/transitive-references-review/assets/ProjectReferenceAdvisoryTests.cs)).
- No production `.csproj` may reference a `*Tests*` project.

**Advisory (opt-in):**
- Closure-metrics report for key projects (`FastFsm`, `Generator`, packable satellites) — `Category=architecture-advisory`.
- Offer copying `assets/ProjectReferenceGraph.cs` into a future `Tests.Architecture` project.

---

## 6. Explicitly not flagged

- **Dual `Abstractions` ref** on `FastFsm.Logging` / `FastFsm.DependencyInjection` via `eng/FastFsmReferences.props` — required because of `PrivateAssets="all"` on `FastFsm` → `Abstractions`.
- **`Generator` fan-in (6 direct refs)** — appropriate for the Roslyn analyzer host; all edges have source usage.
- **`Directory.Build.targets` analyzer injection** — intentional dev-mode bridge; not part of the static `.csproj` edge list but affects build closure for test projects.
- **`archive/` projects** — excluded; historical graphs not assessed.
- **Package version drift** — recently aligned across projects (single bump); no in-graph conflict at audit time.

---

## 7. Correctly simple

- **`Abstractions`** is a true leaf: zero `ProjectReference` edges, `netstandard2.0`, shared contracts for attributes and generator inputs.
- **Production DAG is acyclic** — no cycle work needed.
- **No production → test leakage** — test fixture projects are referenced only from test corpus.

---

## 8. Rationale summary (What / How / Why)

### Stale analyzer edges on `FastFsm`

The core runtime project compiles hand-written code; source generators run in **consumer** projects. Three analyzer `ProjectReference` entries on `FastFsm.csproj` do not participate in compilation (verified by remove-then-build). Removing them simplifies the graph without changing the shipped nupkg layout, provided pack/CI still builds `Generator` before `dotnet pack`.

### `Abstractions` dual reference

This is the opposite case: a scan might suggest `FastFsm.Logging` → `Abstractions` is redundant given `FastFsm.Logging` → `FastFsm`, but removal fails the build. The pattern matches **redundant-but-used** edges kept for correct asset flow ([graph-and-metrics.md](../../../../audits/transitive-references/compendium/references/graph-and-metrics.md)).

### `Benchmark/` outside solution

Not a graph defect, but a **set coherence** issue (P2 cap per [severity-model.md](../../../../audits/transitive-references/compendium/references/severity-model.md)). Low effort to fix by sln membership or documentation.

---

## Appendix A — Full production edge list

```text
Abstractions                          (leaf)
IndentedStringBuilder                   (leaf)
Generator.Model                         (leaf)
Generator.Rules                       (leaf)
Generator.Logger                      → IndentedStringBuilder
Generator.DependencyInjection         → Generator.Model, IndentedStringBuilder
Generator                             → Abstractions, Generator.DependencyInjection,
                                        Generator.Logger, Generator.Model,
                                        Generator.Rules, IndentedStringBuilder
FastFsm                               → Abstractions, IndentedStringBuilder*,
                                        Generator.DependencyInjection*, Generator.Logger*
                                        (* verified stale for compile — see F-1)
FastFsm.Logging                       → FastFsm, Abstractions
FastFsm.DependencyInjection           → FastFsm, Abstractions
```

## Appendix B — `eng/FastFsmReferences.props` injected edges (dev mode, `UsePackages≠true`)

| Flag | Adds |
|------|------|
| `FastFsmReferenceCore` | → `FastFsm`, `Abstractions` |
| `FastFsmReferenceLogging` | → `FastFsm.Logging` |
| `FastFsmReferenceDependencyInjection` | → `FastFsm.DependencyInjection` |

## Appendix C — Solution membership (`FastFsm.slnx`)

16 projects — all production/test projects listed; **`Benchmark/` missing** on disk.
