# FastFsm audits

Architecture review reports live next to the code they describe. **Reports are written in English.**

The 2026-08-16 transitive-references finding (stale analyzer `ProjectReference` on `FastFsm.csproj`) was fixed before 0.9.0; that snapshot report is not kept in tree.

## Convention

```text
audits/<YYYY-MM-DD>-<target-slug>-<audit-id>/report.md
```

| Audit ID | Knowledge package |
|----------|-------------------|
| `transitive-references` | `../audits/transitive-references` |

Citing the compendium (sibling repos):

```markdown
[Lifetime policy](../../../../audits/dependency-injection/compendium/references/lifetimes.md)
```
