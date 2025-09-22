# Progress Log

## Milestone 1
- Reviewed repository documentation (`readme.md`, `FluentAPI.md`, `finstancefluent.md`) to understand the shift to instance-based Configure and method-group delegates.
- Inspected current Fluent DSL implementation and parser (`Generator/Parsers/FluentParser.cs`, `Abstractions/Fluent/FSM.cs`) noting static Configure requirement and string-based callbacks.
- Surveyed ParserComparison.Tests tooling (README, development guide, representative test machines) to plan validation strategy.

## Milestone 2
- Introduced method-group delegate overloads in the Fluent DSL (`Abstractions/Fluent/FSM.cs`) and aligned the ParserComparison stub (`ParserComparison.Tests/Dsl.cs`) so `.OnEntry/.OnExit/.Action` accept the new `Act`/`Entry`/`Exit` shapes while keeping legacy string overloads for back-compat.

## Milestone 3
- Refactored `FluentParser` to prefer instance `Configure()` methods, add validation diagnostics (`FSM3080`–`FSM3083`), and unify callback parsing via method-group aware helpers emitting the new purity diagnostics (`FSM3071`–`FSM3077`).
- Added method-group support for `.OnException(...)` paths and provided overloads in the DSL so callers can bind exception handlers without `nameof`.
- Triggered solution build to capture new compiler feedback; failures highlight remaining static `Configure()` usage that will be migrated next.
