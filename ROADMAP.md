# Roadmap

This file records intended areas of work. It is not a release schedule or a compatibility commitment.

## 0.9.0

Repository package version is `0.9.0`. Product docs, opt-in extensions, warning-free generated code, and pack+smoke scripts are in tree. Pull-request CI is GitHub-hosted; self-hosted runners run only on `push` to `main` / `workflow_dispatch`. Publish by pushing packages to NuGet and tagging `v0.9.0`.

## Next feature candidate: deferred events

Deferred events are the next substantial state-machine feature under consideration.

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
