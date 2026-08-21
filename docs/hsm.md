# Hierarchical state machines (HSM)

FastFsm supports composite states, initial substates, shallow/deep history, internal transitions on ancestors, and transition priority.

Enable hierarchy explicitly or by using HSM attributes / Fluent HSM calls:

```csharp
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
```

## Composite states and initial children

A **composite** state contains substates. When you enter the composite, the machine descends to the **initial substate**.

**Attributes:**

```csharp
[State(HState.Composite, History = HistoryMode.Shallow)]
[State(HState.ChildA, Parent = HState.Composite, IsInitial = true)]
[State(HState.ChildB, Parent = HState.Composite)]
```

**Fluent:**

```csharp
.State(HState.Composite).WithHistory(HistoryMode.Shallow)
.State(HState.ChildA).Parent(HState.Composite).IsInitial()
.State(HState.ChildB).Parent(HState.Composite)
```

Use `Parent()`, not the legacy name `ChildOf()`.

## History

| Mode | Behavior |
|------|----------|
| `HistoryMode.Shallow` | Re-entering the composite restores the last active direct child |
| `HistoryMode.Deep` | Restores the full descendant path |

Set on composite states via `[State(..., History = …)]` or `.WithHistory(HistoryMode.Shallow)`.

## Internal transitions

Internal transitions fire actions **without** leaving the current state configuration (no exit/entry for a state change). Define on an ancestor to handle events in any descendant:

```csharp
[InternalTransition(HState.Composite, HTrigger.Refresh, nameof(OnRefresh))]
```

Fluent:

```csharp
.OnInternal(HTrigger.Refresh).Action(nameof(OnRefresh)).Internal()
```

## External transitions and bubbling

External transitions move between states (including across composite boundaries). The generator emits hierarchical exit/entry sequences. Extension Contract v2 observes that path as per-state `OnStateExiting` / `OnStateEntered` hooks plus `HandledAtState`, `DeclaredTarget`, and `ResolvedTarget` on the attempt result. There are no separate HSM-specific extension callbacks.

## Transition priority

When multiple transitions match, use `.Priority(literal)` in Fluent API or resolve ordering explicitly. Misplaced `Priority()` calls are diagnosed (FSM3040).

## Active path and introspection

Generated HSM machines track the active leaf state in `CurrentState` while traversing composite entry. Logging integration emits hierarchical transition details when `FastFsm.Sharp.Logging` is enabled.

## Validation

HSM-specific diagnostics (FSM2000–FSM2040) cover circular hierarchies, orphan substates, missing initial children, and invalid history configuration. See [diagnostics.md](diagnostics.md).

## Examples in the repository

- `src/Fsm/Fsm.Tests/Tests.Machines/Machines/Legacy/HsmMachine.cs` — attribute HSM
- `src/Fsm/Fsm.Tests/Tests.Fsm/Hsm/` — compile-time and runtime HSM tests
- `src/Benchmark/HsmBenchmarks.cs` — performance harness (see [benchmarks.md](benchmarks.md))
