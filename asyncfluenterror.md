# Async Fluent API – Parser Mismatch Report (SimpleAsync pair)

## Overview
- Goal: Validate Fluent API parser by comparing generated models against legacy attribute parser.
- Scope analyzed: Simple async machine pair (legacy vs fluent) from FastFsm.Async.Tests.
- Result: Single, material difference detected – Fluent uses an internal transition where legacy uses an external self‑loop.

## Steps Performed
- Ran async tests to trigger code generation (your build already present). Inspected generated sources with embedded comparison JSON under:
  - `FastFsm.Async.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/`
- Focus pair:
  - Legacy: `SimpleAsyncMachine` → `global__FastFsm.Async.Tests.Features.Core.SimpleAsyncMachine.Generated.cs`
  - Fluent: `SimpleAsyncMachineFluentFsm` → `global__FastFsm.Async.Tests.Features.Core.SimpleAsyncMachineFluentFsm.Generated.cs`
- Added workshop pair to ParserComparison for future iteration:
  - `ParserComparison.Tests/AsyncSimpleLegacyAndFluent.cs` (contains both legacy and fluent equivalents)

## Machines Compared
- Legacy (attributes): external self‑loop for `Process` on state `Processing`.
- Fluent (DSL): internal transition for `Process` on state `Processing`.

## Generated Model Comparison (Summary)
- Transition: `Processing --(Process)--> Processing`
  - Legacy "Original StateMachineParser Model": `IsInternal = false`
  - Fluent "FluentParser Model": `IsInternal = true`
- All other aspects match (Initial→Processing guard, Processing→Completed action, state entry/exit signatures).

Excerpt from Fluent generated file (showing the difference):
- File: `global__FastFsm.Async.Tests.Features.Core.SimpleAsyncMachineFluentFsm.Generated.cs`
- JSON tail shows the `Process` transition with `"IsInternal": true` in the FluentParser Model.

## Root Cause
- Legacy code defines `Process` as an external self‑loop (exit/entry will run):

```csharp
// Attribute-based (legacy)
[Transition(AsyncStates.Processing, AsyncTriggers.Process, AsyncStates.Processing,
    Action = nameof(ProcessAsync))]
```

- Fluent code defines `Process` as an internal transition (no exit/entry):

```csharp
// Fluent (current)
.State(AsyncStates.Processing)
    .OnInternal(AsyncTriggers.Process)
        .ActionAsync(nameof(ProcessAsync))
        .Internal();
```

## Runtime Semantics Difference
- External self-loop (legacy): executes `OnExit(Processing)` → set state → `OnEntry(Processing)` → `ProcessAsync()`.
- Internal (fluent): executes only `ProcessAsync()`; no `OnExit`/`OnEntry`.

This is visible in the generated methods: legacy path wraps exit/entry around action, fluent path only runs the action.

## Recommendations
- Option A – Align Fluent with Legacy (recommended for strict JSON parity):
  - Change fluent `Process` to external self‑loop:

```csharp
.State(AsyncStates.Processing)
    .On(AsyncTriggers.Process)
        .ActionAsync(nameof(ProcessAsync))
        .GoTo(AsyncStates.Processing);
```

- Option B – Migrate Legacy to Internal Semantics:
  - Change legacy to `[InternalTransition(AsyncStates.Processing, AsyncTriggers.Process, Action = nameof(ProcessAsync))]`.
  - Not suggested now, as we’re keeping legacy intact and only disconnecting it from tests.

## Additional Observations
- TokenMachine files confirm generator behavior:
  - Legacy TokenMachine: Original model populated; FluentParser Model is `{}` (expected for attribute code).
  - Fluent TokenMachineFluentFsm: both Original (fallback) and Fluent models populated and matching.
- ParserComparison pair added for SimpleAsync:
  - File: `ParserComparison.Tests/AsyncSimpleLegacyAndFluent.cs`
  - You can build `ParserComparison.Tests` and compare JSON at the end of the generated files as described in `ParserComparison.Tests/README.md`.

## Files of Interest
- Legacy generated: `FastFsm.Async.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__FastFsm.Async.Tests.Features.Core.SimpleAsyncMachine.Generated.cs`
- Fluent generated: `FastFsm.Async.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__FastFsm.Async.Tests.Features.Core.SimpleAsyncMachineFluentFsm.Generated.cs`
- Token (legacy): `.../TokenMachine.Generated.cs`
- Token (fluent): `.../TokenMachineFluentFsm.Generated.cs`

## Appendix – Full Code Samples

1) Legacy (attributes) – SimpleAsync (relevant parts)

```csharp
[StateMachine(typeof(AsyncStates), typeof(AsyncTriggers))]
public partial class SimpleAsyncMachine
{
    [Transition(AsyncStates.Initial, AsyncTriggers.Start, AsyncStates.Processing,
        Guard = nameof(CanStartAsync))]
    private async ValueTask<bool> CanStartAsync() { await Task.Delay(10); return true; }

    [Transition(AsyncStates.Processing, AsyncTriggers.Process, AsyncStates.Processing,
        Action = nameof(ProcessAsync))]
    private async Task ProcessAsync() { await Task.Delay(10); }

    [Transition(AsyncStates.Processing, AsyncTriggers.Complete, AsyncStates.Completed,
        Action = nameof(Complete))]
    private void Complete() { }

    [State(AsyncStates.Processing, OnEntry = nameof(OnProcessingEntryAsync))]
    private async Task OnProcessingEntryAsync() { await Task.Delay(5); }

    [State(AsyncStates.Processing, OnExit = nameof(OnProcessingExitAsync))]
    private async ValueTask OnProcessingExitAsync() { await Task.Delay(5); }
}
```

2) Fluent (current) – SimpleAsyncMachineFluentFsm (internal Process)

```csharp
[StateMachine(typeof(AsyncStates), typeof(AsyncTriggers))]
public partial class SimpleAsyncMachineFluentFsm
{
    private static void Configure() => FSM
        .State(AsyncStates.Initial)
            .On(AsyncTriggers.Start)
                .GuardAsync(nameof(CanStartAsync))
                .GoTo(AsyncStates.Processing)
        .State(AsyncStates.Processing)
            .OnEntryAsync(nameof(OnProcessingEntryAsync))
            .OnExitAsync(nameof(OnProcessingExitAsync))
            .OnInternal(AsyncTriggers.Process)
                .ActionAsync(nameof(ProcessAsync))
                .Internal()
            .On(AsyncTriggers.Complete)
                .Action(nameof(Complete))
                .GoTo(AsyncStates.Completed)
        .State(AsyncStates.Completed);
}
```

3) Fluent (proposed) – SimpleAsyncMachineFluentFsm (external Process, parity with legacy)

```csharp
.State(AsyncStates.Processing)
    .On(AsyncTriggers.Process)
        .ActionAsync(nameof(ProcessAsync))
        .GoTo(AsyncStates.Processing);
```

4) ParserComparison workshop pair

```csharp
// ParserComparison.Tests/AsyncSimpleLegacyAndFluent.cs
[StateMachine(typeof(AaslState), typeof(AaslTrigger))]
public partial class AsyncSimpleLegacyStateMachine { /* attribute config mirroring legacy */ }

[StateMachine(typeof(AaslState), typeof(AaslTrigger))]
public partial class AsyncSimpleFluentMachine
{
    private static void Configure() => FSM
        .State(AaslState.Initial)
            .On(AaslTrigger.Start).GuardAsync(nameof(CanStartAsync)).GoTo(AaslState.Processing)
        .State(AaslState.Processing)
            .OnEntryAsync(nameof(OnProcessingEntryAsync))
            .OnExitAsync(nameof(OnProcessingExitAsync))
            .OnInternal(AaslTrigger.Process).ActionAsync(nameof(ProcessAsync)).Internal()
            .On(AaslTrigger.Complete).Action(nameof(Complete)).GoTo(AaslState.Completed);
}
```

---

If you want me to apply Option A (external self-loop in Fluent) and verify JSON parity now, I can patch the fluent machine and re-run the comparison.

## Fix Applied and Verified

- Applied Option A: updated fluent Process to external self-loop in both the async test machine and the ParserComparison pair.
  - Files changed:
    - `FastFsm.Async.Tests/Features/Core/BasicAsyncStateMachineTests.cs`
    - `ParserComparison.Tests/AsyncSimpleLegacyAndFluent.cs`
- Rebuilt ParserComparison and checked generated JSON in:
  - `ParserComparison.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__ParserComparison.Tests.AsyncSimpleFluentMachine.Generated.cs`
  - `ParserComparison.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__ParserComparison.Tests.AsyncSimpleLegacyStateMachine.Generated.cs`
- Result: For the transition `Processing --(Process)--> Processing` both models now show `"IsInternal": false` in the comparison sections. Other properties already matched.

Conclusion: The fluent and legacy models are now 1:1 equivalent for the SimpleAsync pair.
