# Roadmap

This file records intended areas of work. It is not a release schedule or a compatibility commitment.

## 0.9.1 (released)

Repository package version is `0.9.1`. Patch release after 0.9.0: `net10.0-windows` props, clean-build packaging, CI hygiene, and stable async cancellation tests. Published on NuGet.org; git tag `v0.9.1` marks the release.

## 0.9.0 (released)

Repository package version is `0.9.0`. First .NET 10 release with canonical `FastFsm.Sharp*` package IDs and legacy `FastFsm.Net*` metapackages. Published on NuGet.org; git tag `v0.9.0` marks the release.

## Next work: extension contract v2

The extension mechanism ships in 0.9.1, but its public data contract is not correct enough to build observability on. In particular, the data delivered to extensions for hierarchical machines does not describe what the machine actually did: a trigger handled on an ancestor is reported as a state change that never occurred, and a transition into a composite state reports the composite rather than the leaf entered.

Extension contract v2 replaces the extension surface with a contract generic in the state and trigger types, with stable machine identity, explicit transition semantics, structured outcomes, and a tested lifecycle event stream. It is a breaking change to the extension API, taken while the package has no dependent users, and is intended to release as `0.10.0`.

`FastFsm.Sharp.Observability` — tracing, metrics, and an inspection stream — is built on the v2 contract and is not started before it lands.

See [docs/proposals/extension-contract-v2.md](docs/proposals/extension-contract-v2.md).

## Later feature candidate: deferred events

Deferred events remain a substantial state-machine feature under consideration, after extension contract v2.

The intended capability is to allow a state to defer selected triggers instead of handling or rejecting them immediately, and to make those events eligible for processing after the machine leaves the deferring configuration.

Any implementation must cover both configuration APIs and the existing execution model:

- Attribute API and Fluent API parity;
- flat FSM and HSM semantics;
- synchronous and asynchronous machines;
- payload behavior;
- bounded storage and explicit overflow behavior;
- compile-time diagnostics;
- deterministic ordering and run-to-completion behavior;
- no generated-code or runtime overhead when the feature is not used.

The design is not yet frozen. In particular, event ordering, HSM precedence, payload storage, and the exact meaning of a successful `TryFire` for a deferred event must be specified before implementation.

See [docs/proposals/deferred-events.md](docs/proposals/deferred-events.md).

## Later investigation: non-enum state and trigger types

A previous design exploration considered `record struct`, smart-enum, and string-backed state or trigger types while retaining compact generated internal identifiers.

This remains an investigation rather than a planned feature. Any design would need to preserve compile-time validation, deterministic generated code, AOT/trimming compatibility, and predictable performance before it becomes part of the product roadmap.
