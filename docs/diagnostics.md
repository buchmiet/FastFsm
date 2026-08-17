# Diagnostics

FastFsm reports compile-time diagnostics from the Roslyn source generator. Rule IDs are defined in `src/Generator/Generator.Rules/Definitions/RuleIdentifiers.cs` and described in `src/Generator/Generator.Rules/Definitions/RuleDefinition.cs` (`DefinedRules`).

**This document is the 0.9.x diagnostic catalog.** Legacy IDs such as FSM001 or FSM100 appear only in code comments mapping to the new scheme.

## Summary

| Range | Category |
|-------|----------|
| FSM0100–FSM0500 | Model & declarations |
| FSM1100–FSM1120 | Async consistency |
| FSM2000–FSM2040 | Hierarchical state machines |
| FSM3000–FSM3083 | Fluent DSL |

Severity: **Error** stops compilation; **Warning** and **Info** are non-fatal unless configured otherwise.

## Model & declarations (FSM0100–FSM0500)

| ID | Severity | Title |
|----|----------|-------|
| FSM0100 | Warning | Potentially missing `[StateMachine]` attribute or non-partial class |
| FSM0101 | Error | State/Trigger types must be enums |
| FSM0200 | Error | Invalid enum value in transition |
| FSM0300 | Error | Invalid method signature for FSM callback |
| FSM0301 | Error | Guard with payload in non-payload machine |
| FSM0302 | Error | Callback returns `async void` |
| FSM0400 | Warning | Duplicate transition (same from-state + trigger) |
| FSM0500 | Info | Possibly unreachable state |

## Async (FSM1100–FSM1120)

| ID | Severity | Title |
|----|----------|-------|
| FSM1100 | Warning | Mixed synchronous and asynchronous callbacks |
| FSM1110 | Error | Async guard must return `ValueTask<bool>` |
| FSM1120 | Error | Async callback in synchronous machine |

## HSM (FSM2000–FSM2040)

| ID | Severity | Title |
|----|----------|-------|
| FSM2000 | Error | Circular hierarchy |
| FSM2010 | Error | Orphan substate / invalid parent reference |
| FSM2020 | Warning | Composite without initial substate |
| FSM2030 | Error | Multiple initial substates |
| FSM2040 | Error | History on non-composite state |

## Fluent DSL (FSM3000–FSM3083)

| ID | Severity | Title |
|----|----------|-------|
| FSM3000 | Error | Open transition not finalized (missing `.GoTo()` / `.Internal()`) |
| FSM3010 | Info | Transition auto-finalized as internal |
| FSM3020 | Warning | Multiple `Payload()` calls on one transition |
| FSM3030 | Error | Invalid `Priority()` argument (must be int literal) |
| FSM3040 | Error | `Priority()` without active transition |
| FSM3050 | Error | Multiple global `OnException` handlers |
| FSM3060 | Error | Invalid `OnException` handler signature |
| FSM3070 | Error | Ambiguous method group |
| FSM3071 | Error | Impure expression in Fluent DSL |
| FSM3072 | Error | Property used where method expected |
| FSM3073 | Error | External method group not allowed |
| FSM3074 | Error | Method signature incompatible with DSL position |
| FSM3075 | Error | Lambda expression not allowed |
| FSM3076 | Error | Field/property access where literal required |
| FSM3077 | Error | Method invocation in Fluent DSL |
| FSM3080 | Error | Multiple `Configure` methods |
| FSM3081a | Error | `Configure` must be private |
| FSM3081b | Error | `Configure` must be parameterless |
| FSM3081c | Error | `Configure` cannot be virtual/override |
| FSM3081d | Warning | `Configure` should be instance method (static is legacy) |
| FSM3082 | Error | `Configure` must be declared on the state machine type |
| FSM3083 | Error | `Configure` cannot be partial |

## Removed / not implemented

The following are **not** part of the 0.9 catalog:

- FSM007, FSM008, FSM105 — removed unused rules
- FSM203–FSM206 — removed unused Fluent rules
- FSM9000–FSM9013 — generator infrastructure diagnostics removed
- **FSM3084** — not defined in `RuleIdentifiers`

## Source of truth in code

When this document and the generator disagree, trust:

1. `src/Generator/Generator.Rules/Definitions/RuleIdentifiers.cs`
2. `src/Generator/Generator.Rules/Definitions/RuleDefinition.cs` → `DefinedRules.All`
3. `src/Generator/Generator.Rules/Definitions/RuleCatalog.cs`

## Suppressing warnings

Use standard C# `#pragma warning disable FSM0400` or `.editorconfig` `dotnet_diagnostic.FSM0400.severity` entries. Prefer fixing the underlying model issue when possible.
