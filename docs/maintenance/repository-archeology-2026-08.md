# FastFsm repository archaeology — August 2026

## Purpose

This note records a **history/repository research pass only**. It does not change product code, choose a release version, or declare any historical branch safe to delete. Its purpose is to give the follow-up maintenance agent a defensible starting point before it normalizes `main`, packaging, CI, targets, documentation, or benchmarks.

The core question was: **which ref best represents the latest FastFsm work, and which historical branches still contain work that may not be represented there?**

## Executive conclusion

`main` is not the latest product line and should not be treated as the sole source of truth for recovery work.

The strongest lineage found is:

```text
main (bd34a64, 2025-08-23)
  |
  +-- 22 commits --> v0.8.0.29 (cdf8709, 2025-08-28)
                       |
                       +-- 197 commits --> nuget_pack (f26e458, 2025-10-22)
```

`v0.8.0.29` is the last clearly tagged/release-like checkpoint in this lineage. `nuget_pack` is a direct descendant of it and contains the bulk of the later engineering work. It is therefore the **best recovery/reference line found in this pass**.

However, `nuget_pack` should **not** simply replace `main` without a cleanup/reconciliation pass. It mixes substantial product work with packaging experiments, temporary reports, generated/scratch material, and a non-product version stamp. In addition, several side branches have small unique tails that are not ancestors of `nuget_pack`.

Recommended interpretation:

- `main`: stale public/default snapshot; useful as an old baseline, not as the latest implementation.
- `v0.8.0.29`: last coherent tagged baseline before the later development burst.
- `nuget_pack`: primary archaeology/recovery candidate and most advanced integrated line found, but not publication-ready by inspection alone.
- divergent Fluent/HSM/develop branches: preservation/reconciliation inputs; do not delete until their unique commits are checked semantically.

## Evidence: primary lineage

### `main` -> `v0.8.0.29`

GitHub comparison reports `v0.8.0.29` **22 commits ahead and 0 behind** `main`. This means the tag is a direct continuation of the default branch rather than an unrelated release branch.

The tag commit is `cdf8709db0cf881a2414f74a93491149bcb81c12`, with release message `chore(release): v0.8.0.29`.

The core package at this tag stamps `0.8.0.29`, whereas current `main` stamps `0.8.0.1`. This is another strong signal that default-branch state was left behind rather than intentionally becoming a newer release state.

### `v0.8.0.29` -> `nuget_pack`

GitHub comparison reports `nuget_pack` **197 commits ahead and 0 behind** `v0.8.0.29` (and 219 commits ahead / 0 behind `main`). Thus `nuget_pack` is a direct descendant of the last 0.8.0.29 tagged state.

Its head is `f26e458ada96c531cbbbcc21a3c14948dcfa752f`, dated 2025-10-22.

The change set after `v0.8.0.29` is not merely packaging churn. It contains, among other things:

- a Fluent API surface (`Abstractions/Fluent/FSM.cs` and later parser/generator work),
- substantially expanded sync/async test coverage,
- cancellation and payload test expansion,
- legacy-vs-Fluent parity infrastructure,
- Fluent/HSM/model/async diagnostics tests,
- instance-oriented tests,
- logging and DI integration work,
- test-machine reorganization,
- `buildTransitive` packaging work,
- additional benchmark implementations (including non-.NET comparison harnesses).

This makes `nuget_pack` materially more advanced than `main` and the 0.8.0.29 tag.

## Why `nuget_pack` is not yet a clean canonical branch

The same line also contains clear development/recovery debris, including examples such as:

- `GenTest.exe` and a sizeable `GenTest/` scratch area,
- generated output snapshots under `GenTest/output_*`,
- transient reports/notes such as `FLUENT-API-FINAL.md`, `parity-prep.md`, and historical work reports,
- local/package-feed support files,
- a package version stamp of `0.0.4.11-nuget-pack` at the branch head rather than a normal FastFsm release version.

These do not make the code invalid. They do mean the branch is better described as an **integration/work line** than as a ready-to-publish replacement for `main`.

A follow-up agent should recover from this line selectively/structurally rather than interpreting the branch name or head package version as product truth.

## Side-branch classification

The comparisons below use `nuget_pack` as the reference head. “Ancestor” means GitHub reports the branch head as the merge base with zero commits unique to that historical branch. “Diverged” means the historical branch still has commits not reachable from `nuget_pack`.

| Branch | Relationship to `nuget_pack` | Unique commits on branch | Interpretation for recovery |
|---|---:|---:|---|
| `fluentrefinement` | ancestor | 0 | Work is represented in `nuget_pack`; no separate recovery expected from branch head. |
| `homefluent` | ancestor | 0 | Work is represented in `nuget_pack`. |
| `workFluentAPi` | ancestor | 0 | Work is represented in `nuget_pack`. |
| `gen-upd` | ancestor | 0 | Work is represented in `nuget_pack`. |
| `fluentapi` | diverged | 6 | Do not delete/archive blindly. Compare the 6 unique commits for semantic equivalence with later Fluent work. |
| `fluentapiv2` | diverged | 9 | Same: small unique tail requires inspection before retirement. |
| `feature/fluent-hsm-parser-v0.7.5` | diverged | 1 | High-value targeted check: inspect the single unique HSM-parser commit and determine whether its behavior was reimplemented later. |
| `develop` | diverged | 3 | Old development tail remains unique; inspect before branch cleanup, but do not use `develop` as the recovery base. |

Branches `diag/with-diagnostics` and `error_order` were identified in the repository inventory but are **not classified by semantic equivalence in this first pass**. They should be included in the agent's final branch-retirement checklist rather than assumed dead.

### Important nuance about divergent branches

A commit being “unique” in Git history does **not** mean its functionality is missing from `nuget_pack`. The later line may contain an independently rewritten or squashed equivalent. The correct follow-up is therefore patch/behavior comparison, not automatic cherry-picking.

This matters especially for `fluentapi` and `fluentapiv2`: the later `nuget_pack` tree clearly contains extensive Fluent API/parser/generator/test work, despite those branches retaining 6 and 9 unique historical commits respectively.

## Release/repository signals discovered during the pass

These are secondary to branch recovery, but they explain how the repository reached the current ambiguous state:

1. GitHub Releases are empty even though release-style tags exist.
2. Version truth is split across refs: current `main` stamps `0.8.0.1`, `v0.8.0.29` stamps `0.8.0.29`, and `nuget_pack` currently stamps an experimental `0.0.4.11-nuget-pack` value.
3. README state is not synchronized with the later tags: the README at `v0.8.0.29` still prominently announces “Version 0.7 Complete”.
4. The default branch remained at an earlier point while release and post-release work continued elsewhere.

The recovery task should therefore establish a single canonical branch/version/release flow after code lineage has been reconciled.

## Recommended handoff to the local maintenance agent

### Phase 1 — preserve and reproduce

1. Fetch all remote branches and tags; do not prune anything yet.
2. Preserve `v0.8.0.29` as the last tagged baseline.
3. Create a temporary recovery branch from `nuget_pack` (not from `main`).
4. Record the exact current heads of all historical branches before any cleanup.
5. Build and run the complete test suite from the recovery branch before changing structure.

### Phase 2 — reconcile the unique tails

For each divergent branch, use patch-id/diff plus tests rather than commit ancestry alone:

- `fluentapi`: inspect 6 unique commits,
- `fluentapiv2`: inspect 9 unique commits,
- `feature/fluent-hsm-parser-v0.7.5`: inspect 1 unique commit,
- `develop`: inspect 3 unique commits,
- additionally classify `diag/with-diagnostics` and `error_order`.

For each unique commit, record one of:

- already semantically present in recovery head,
- obsolete/superseded by design,
- worth porting,
- uncertain — requires targeted regression test.

Do **not** cherry-pick release-number-only commits merely to preserve ancestry.

### Phase 3 — clean recovery head

Before proposing a new canonical `main`:

- separate real test fixtures from scratch/generated output,
- remove or relocate transient work reports and generated binaries,
- normalize package/version ownership,
- verify packaged source generator behavior from a fresh consumer project,
- verify logging/DI packages and build/buildTransitive behavior,
- then run the entire suite again.

Only after that should the project decide whether to merge, fast-forward, or otherwise replace the stale default-branch lineage.

## Suggested source-of-truth decision rule

For the maintenance work, use this order of authority until reconciliation is complete:

1. **Behavior + passing tests on the recovered line**
2. **`nuget_pack` implementation**, because it is the latest integrated descendant found
3. **`v0.8.0.29`**, as the last tagged baseline for regression comparison
4. **divergent branch unique commits**, as candidate missing work
5. **`main`**, as historical/default-branch baseline only

This ordering is intentionally temporary. The goal of the maintenance pass is to finish with one obvious canonical branch and one reproducible release pipeline.

## Non-findings / limits of this research pass

- No local checkout was available in this environment, so this pass did not run builds, tests, `git patch-id`, bisect, or binary/package inspection.
- Commit ancestry proves lineage, not behavioral correctness.
- The divergent tails listed above have been identified but not yet proven semantically redundant or necessary.
- No branch should be deleted based only on this document.

## Bottom line

The repository is recoverable without guessing: there is a strong continuous line from `main` through `v0.8.0.29` to `nuget_pack`, and that line contains the latest integrated body of work found. The main risk is not losing the 197 post-release commits; it is prematurely declaring all side branches obsolete or publishing the integration branch without removing its experimental packaging/scratch state.

The next agent should therefore **start from `nuget_pack`, reconcile the small divergent tails, clean the integration state, prove it with tests/package smoke tests, and only then establish the new canonical `main`**.
