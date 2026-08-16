# FastFsm repository archaeology — branch heads (August 2026)

Recorded at recovery start. Do not delete branches based on this snapshot alone.

| Branch | Head | Unique vs `nuget_pack` | Recovery note |
|---|---|---|---|
| `main` | `bd34a64` | baseline (stale) | Historical default only |
| `v0.8.0.29` | `cdf8709` | +22 from `main` | Last tagged baseline |
| `nuget_pack` | `f26e458` | recovery base | Primary integrated line |
| `error_order` | `02f8940` + `95e2117` | +2 from `nuget_pack` | **Merged into recovery** — test infra refactor + v0.0.4.12 |
| `develop` | `460f65b` | 3 release-only commits | No product delta expected |
| `fluentapi` | `de0f3cb` | 6 (mostly releases + early fluent) | Superseded by later Fluent work on `nuget_pack` |
| `fluentapiv2` | `766106e` | 9 (mostly releases + early async fluent) | Superseded by later Fluent work on `nuget_pack` |
| `feature/fluent-hsm-parser-v0.7.5` | `f05f949` | 1 (`tests fix`) | Extension hook tests; behavior present on recovery line |
| `diag/with-diagnostics` | `0f7b4ef` | 0 (ancestor) | No separate recovery needed |
| `fluentrefinement`, `homefluent`, `workFluentAPi`, `gen-upd` | various | 0 (ancestors) | Already in `nuget_pack` |

## Reconciliation decisions

- **`error_order`**: ported (cherry-picked). Contains Machines.Tests extraction and logging test simplification.
- **`fluentapi` / `fluentapiv2`**: release-number commits only plus early exploratory work reimplemented later.
- **`develop`**: release-only version stamps (`v0.6.2.27-develop` … `v0.6.2.29-develop`).
- **`feature/fluent-hsm-parser-v0.7.5`**: `OnTransitioned` extension tests; equivalent coverage exists on recovery head.
- **`diag/with-diagnostics`**: ancestor of `nuget_pack`; no unique tail.
