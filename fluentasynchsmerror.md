# Fluent API HSM Error Report - SourceOrderTieMachine Duplicate Case Problem

## Executive Summary

The Fluent API parser generates incorrect models when multiple transitions are defined from the same state on the same trigger with the same priority. This causes the code generator to produce invalid C# code with duplicate case labels in switch statements, resulting in compilation error CS0152.

## Problem Description

### Error Message
```
error CS0152: The switch statement contains multiple cases with the label value '0'
```

### Root Cause
The Fluent parser passes both transitions to the generator instead of resolving the conflict (choosing the first one based on source order). The Legacy parser correctly handles this scenario.

## Machine Definitions

### Legacy API Definition (Working)
**File:** `/mnt/c/Users/newon/source/repos/FastFsm/FastFsm.Async.Tests/Features/Hsm/Runtime/HierarchicalAsyncRuntimeTests.cs`
**Lines:** 276-293

```csharp
[StateMachine(typeof(S), typeof(T))]
public partial class SourceOrderTieMachine
{
    public List<string> Log { get; } = new();

    [State(S.A, OnEntry = nameof(OnAEntryAsync))]
    [State(S.B)]
    [State(S.C)]
    private void ConfigureStates() { }

    [Transition(S.A, T.Go, S.B, Action = nameof(First), Priority = 0)]
    [Transition(S.A, T.Go, S.C, Action = nameof(Second), Priority = 0)]
    private void ConfigureTransitions() { }

    private async Task First() { await Task.Yield(); Log.Add("First"); }
    private async Task Second() { await Task.Yield(); Log.Add("Second"); }
    private async Task OnAEntryAsync() => await Task.CompletedTask;
}
```

### Fluent API Definition (Broken)
**File:** `/mnt/c/Users/newon/source/repos/FastFsm/FastFsm.Async.Tests/Features/Hsm/Runtime/HierarchicalAsyncRuntimeTests.cs`
**Lines:** 593-622

```csharp
[StateMachine(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T))]
public partial class SourceOrderTieMachineFluentFsm
{
    public List<string> Log { get; } = new();
    
    private static void Configure()
    {
        FSM.State(AsyncResolutionOrderTests.S.A)
            .OnEntryAsync(nameof(OnAEntryAsync));
        FSM.State(AsyncResolutionOrderTests.S.B);
        FSM.State(AsyncResolutionOrderTests.S.C);
        
        // Two transitions with same priority - first wins
        FSM.At(AsyncResolutionOrderTests.S.A)
            .On(AsyncResolutionOrderTests.T.Go)
            .ActionAsync(nameof(First))
            .Priority(0)
            .GoTo(AsyncResolutionOrderTests.S.B);
            
        FSM.At(AsyncResolutionOrderTests.S.A)
            .On(AsyncResolutionOrderTests.T.Go)
            .ActionAsync(nameof(Second))
            .Priority(0)
            .GoTo(AsyncResolutionOrderTests.S.C);
    }

    private async Task OnAEntryAsync() => await Task.CompletedTask;
    private async Task First() { Log.Add("First"); await Task.Yield(); }
    private async Task Second() { Log.Add("Second"); await Task.Yield(); }
}
```

## Generated Code Comparison

### Legacy Generated Code (Correct)
**File:** `/mnt/c/Users/newon/source/repos/FastFsm/ParserComparison.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__ParserComparison.Tests.SourceOrderTieMachineComparison.SourceOrderTieMachine_Legacy.Generated.cs`
**Lines:** 98-113

```csharp
switch (_currentState)
{
    case ParserComparison.Tests.SourceOrderTieMachineComparison.S.A:
    {
        switch (trigger)
        {
            case ParserComparison.Tests.SourceOrderTieMachineComparison.T.Go:
            {
                return true;
            }
            default: return false;
        }
    }
    default: return false;
}
```

### Fluent Generated Code (Broken)
**File:** `/mnt/c/Users/newon/source/repos/FastFsm/ParserComparison.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__ParserComparison.Tests.SourceOrderTieMachineComparison.SourceOrderTieMachine_Fluent.Generated.cs`
**Lines:** 98-117

```csharp
switch (_currentState)
{
    case ParserComparison.Tests.SourceOrderTieMachineComparison.S.A:
    {
        switch (trigger)
        {
            case ParserComparison.Tests.SourceOrderTieMachineComparison.T.Go:  // Line 104
            {
                return true;
            }
            case ParserComparison.Tests.SourceOrderTieMachineComparison.T.Go:  // Line 108 - DUPLICATE!
            {
                return true;
            }
            default: return false;
        }
    }
    default: return false;
}
```

## Model Differences

### Expected Behavior (Legacy Parser)
The Legacy parser, when encountering multiple transitions with the same `(FromState, Trigger, Priority)` tuple, resolves the conflict by:
1. Taking the first transition defined in source order
2. Ignoring subsequent transitions with the same tuple
3. Passing only one transition to the generator

Result: **1 transition** for `(S.A, T.Go, Priority=0)` → goes to `S.B` with action `First`

### Actual Behavior (Fluent Parser)
The Fluent parser incorrectly:
1. Accepts both transitions with the same `(FromState, Trigger, Priority)` tuple
2. Passes both transitions to the generator
3. The generator faithfully generates code for both transitions

Result: **2 transitions** for `(S.A, T.Go, Priority=0)` → attempts to generate cases for both `S.B` and `S.C` destinations

## Impact

1. **Compilation Failure:** Generated code does not compile due to duplicate case labels
2. **Test Failure:** Unit tests for resolution order cannot run
3. **Feature Incomplete:** Priority-based transition resolution is broken in Fluent API

## Solution Required

The Fluent parser (`/mnt/c/Users/newon/source/repos/FastFsm/Generator/Parsers/FluentParser.cs`) needs to be modified to:

1. **Track unique transitions** by `(FromState, Trigger, Priority)` tuple
2. **Resolve conflicts** by keeping only the first transition when duplicates are found
3. **Match Legacy behavior** for source-order resolution

### Suggested Implementation Location
In `FluentParser.cs`, after building the transitions list but before returning the model, add conflict resolution logic:

```csharp
// Group transitions by (FromState, Trigger, Priority)
var resolvedTransitions = model.Transitions
    .GroupBy(t => (t.FromState, t.Trigger, t.Priority))
    .Select(g => g.First())  // Take first in source order
    .ToList();

model.Transitions = resolvedTransitions;
```

## Test Expectations

When fixed, the test should:
1. Fire trigger `T.Go` from state `S.A`
2. Execute the `First` action (not `Second`)
3. Transition to state `S.B` (not `S.C`)
4. Log should contain `["First"]` only

## Files Affected

- **Parser:** `/mnt/c/Users/newon/source/repos/FastFsm/Generator/Parsers/FluentParser.cs`
- **Test:** `/mnt/c/Users/newon/source/repos/FastFsm/FastFsm.Async.Tests/Features/Hsm/Runtime/HierarchicalAsyncRuntimeTests.cs`
- **Generated (Legacy):** Works correctly
- **Generated (Fluent):** Compilation error CS0152

## Versions
- **Package Version:** 0.0.0.10-feature-fluent-hsm-parser-v0.7.5
- **Branch:** workFluentApi
- **Date:** 2025-09-04