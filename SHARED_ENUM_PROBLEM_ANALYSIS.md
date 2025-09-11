# Shared Enum Problem - Full Code Analysis

## Executive Summary

**Problem:** Multiple state machines share the same enum definitions, causing EnumConverterV2 to fail when trying to "convert" between identical types. Both Fluent and Legacy APIs use exactly the same enum types in many cases.

## 1. The Core Problem: StateCallbackTests Shared Enums

### Source Definition
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/Features/Core/StateCallbackTests.cs` (lines 206-244)

```csharp
// Test state machines
public enum CallbackState { A, B, C }
public enum CallbackTrigger { Next }

public enum InitialState { Start, Next }
public enum InitialTrigger { Go }

public enum InternalState { Active, Inactive }
public enum InternalTrigger { Update, Deactivate }

public enum GuardedState { A, B }
public enum GuardedTrigger { Go }

public enum SelfState { Active }
public enum SelfTrigger { Refresh }

public enum ExceptionState { A, B }
public enum ExceptionTrigger { Go }

public enum ComplexCallbackState { Idle, Ready, Processing, Done }
public enum ComplexCallbackTrigger { Start, Process, Complete }

public enum MultiState { A, B }
public enum MultiTrigger { Go }
```

### How They're Used: InternalTransition Example

#### Fluent Version
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/Machines/InternalTransitionMachine.Fluent.cs`

```csharp
using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.InternalState), typeof(StateCallbackTests.InternalTrigger))]
    public partial class InternalTransitionMachineFluent
    {
        public List<string> EventLog { get; } = [];

        private static void Configure() => FSM
            .State(StateCallbackTests.InternalState.Active)
                .OnEntry(nameof(OnEntryActive)).OnExit(nameof(OnExitActive))
                .On(StateCallbackTests.InternalTrigger.Deactivate).GoTo(StateCallbackTests.InternalState.Inactive)
            .State(StateCallbackTests.InternalState.Inactive)
                .OnEntry(nameof(OnEntryInactive))
            .State(StateCallbackTests.InternalState.Active)
                .OnInternal(StateCallbackTests.InternalTrigger.Update).Action(nameof(HandleUpdate));

        private void OnEntryActive() => EventLog.Add("OnEntry-Active");
        private void OnExitActive() => EventLog.Add("OnExit-Active");
        private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
        private void HandleUpdate() => EventLog.Add("InternalAction");
    }
}
```

#### Legacy Version
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/Machines/InternalTransitionMachine.Legacy.cs`

```csharp
using System.Collections.Generic;
using Abstractions.Attributes;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.InternalState), typeof(StateCallbackTests.InternalTrigger))]
    public partial class InternalTransitionMachineLegacy
    {
        public List<string> EventLog { get; } = [];

        [State(StateCallbackTests.InternalState.Active, OnEntry = nameof(OnEntryActive), OnExit = nameof(OnExitActive))]
        [State(StateCallbackTests.InternalState.Inactive, OnEntry = nameof(OnEntryInactive))]
        private void ConfigureStates() { }

        [Transition(StateCallbackTests.InternalState.Active, StateCallbackTests.InternalTrigger.Deactivate, StateCallbackTests.InternalState.Inactive)]
        private void TransitionDeactivate() { }

        [InternalTransition(StateCallbackTests.InternalState.Active, StateCallbackTests.InternalTrigger.Update, Action = nameof(HandleUpdate))]
        private void InternalUpdate() { }

        private void OnEntryActive() => EventLog.Add("OnEntry-Active");
        private void OnExitActive() => EventLog.Add("OnExit-Active");
        private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
        private void HandleUpdate() => EventLog.Add("InternalAction");
    }
}
```

**KEY OBSERVATION:** Both Fluent and Legacy use `StateCallbackTests.InternalState` and `StateCallbackTests.InternalTrigger` - THE EXACT SAME TYPES!

## 2. GuardPermitted Problem - Wrong Namespace

### Actual Definition
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/Features/Core/GuardPermittedTriggersTests.cs`

```csharp
namespace FastFsm.Tests.Features.Core
{
    // ── Legacy API mini-FSM ───────────────────────────────────────────────────────────────
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class GuardPermittedMachineLegacy
    {
        public bool Allow { get; set; }

        private bool CanRun() => Allow;

        [Transition(State.Idle, Trigger.Run, State.Done,
            Guard = nameof(CanRun))]
        private void Configure() { }
    }

    // ── Fluent API mini-FSM ───────────────────────────────────────────────────────────────
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class GuardPermittedMachineFluent
    {
        public bool Allow { get; set; }

        private bool CanRun() => Allow;

        private static void Configure() => FSM
            .State(State.Idle)
                .On(Trigger.Run)
                    .Guard(nameof(CanRun))
                    .GoTo(State.Done);
    }

    public enum State { Idle, Done }
    public enum Trigger { Run }
}
```

### But MachineRegistry References Wrong Types!
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/MachineRegistry.cs`

```csharp
// GuardPermitted from Features/Core
Register("GuardPermitted",
    typeof(Features.Core.State), typeof(Features.Core.State),  // WRONG! Should be GuardPermittedTriggersTests.State
    typeof(Features.Core.Trigger), typeof(Features.Core.Trigger),  // WRONG! Should be GuardPermittedTriggersTests.Trigger
    (api, state) => api == StateMachineWrapperFactory.ApiType.Fluent
        ? new GuardPermittedFluentWrapper(state)
        : new GuardPermittedLegacyWrapper(state));
```

**PROBLEM:** There is no `Features.Core.State` or `Features.Core.Trigger` - they're actually in the GuardPermittedTriggersTests class!

## 3. EnumConverterV2 Failing on Identical Types

### The Conversion Method
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/EnumConverterV2.cs`

```csharp
public static (bool isValid, List<string> errors) ValidateEnumParity<TFluent, TLegacy>(string machineName)
    where TFluent : struct, Enum
    where TLegacy : struct, Enum
{
    var errors = new List<string>();
    var fluentNames = Enum.GetNames(typeof(TFluent));
    var legacyNames = Enum.GetNames(typeof(TLegacy));

    // Check if all Fluent values can map to Legacy
    foreach (var fluentName in fluentNames)
    {
        try
        {
            var fluentValue = Enum.Parse<TFluent>(fluentName);
            var _ = ToLegacy<TLegacy>(fluentValue, machineName);  // THIS FAILS WHEN TFluent == TLegacy!
        }
        catch (Exception ex)
        {
            errors.Add($"Fluent -> Legacy: {fluentName} failed: {ex.Message}");
        }
    }

    // Check if all Legacy values can map to Fluent
    foreach (var legacyName in legacyNames)
    {
        try
        {
            var legacyValue = Enum.Parse<TLegacy>(legacyName);
            var _ = ToFluent<TFluent>(legacyValue, machineName);  // THIS FAILS WHEN TFluent == TLegacy!
        }
        catch (Exception ex)
        {
            errors.Add($"Legacy -> Fluent: {legacyName} failed: {ex.Message}");
        }
    }

    return (errors.Count == 0, errors);
}
```

### ToLegacy/ToFluent Methods Expect Different Types
```csharp
public static TLegacy ToLegacy<TLegacy>(object fluentValue, string machineName)
    where TLegacy : struct, Enum
{
    // When fluentValue is already TLegacy, this conversion logic breaks!
    // ...
}
```

## 4. All Machines Using Shared Enums

### Complete List from MachineRegistry

```csharp
// All these use StateCallbackTests enums - SAME TYPE FOR FLUENT AND LEGACY!
Register("CallbackOrder",
    typeof(Features.Core.StateCallbackTests.CallbackState), 
    typeof(Features.Core.StateCallbackTests.CallbackState),  // Same!
    typeof(Features.Core.StateCallbackTests.CallbackTrigger), 
    typeof(Features.Core.StateCallbackTests.CallbackTrigger),  // Same!
    null);

Register("ComplexCallback",
    typeof(Features.Core.StateCallbackTests.ComplexCallbackState),
    typeof(Features.Core.StateCallbackTests.ComplexCallbackState),  // Same!
    typeof(Features.Core.StateCallbackTests.ComplexCallbackTrigger),
    typeof(Features.Core.StateCallbackTests.ComplexCallbackTrigger),  // Same!
    null);

Register("ExceptionCallback",
    typeof(Features.Core.StateCallbackTests.ExceptionState),
    typeof(Features.Core.StateCallbackTests.ExceptionState),  // Same!
    typeof(Features.Core.StateCallbackTests.ExceptionTrigger),
    typeof(Features.Core.StateCallbackTests.ExceptionTrigger),  // Same!
    null);

Register("GuardedCallback",
    typeof(Features.Core.StateCallbackTests.GuardedState),
    typeof(Features.Core.StateCallbackTests.GuardedState),  // Same!
    typeof(Features.Core.StateCallbackTests.GuardedTrigger),
    typeof(Features.Core.StateCallbackTests.GuardedTrigger),  // Same!
    null);

Register("InitialState",
    typeof(Features.Core.StateCallbackTests.InitialState),
    typeof(Features.Core.StateCallbackTests.InitialState),  // Same!
    typeof(Features.Core.StateCallbackTests.InitialTrigger),
    typeof(Features.Core.StateCallbackTests.InitialTrigger),  // Same!
    null);

Register("InternalTransition",
    typeof(Features.Core.StateCallbackTests.InternalState),
    typeof(Features.Core.StateCallbackTests.InternalState),  // Same!
    typeof(Features.Core.StateCallbackTests.InternalTrigger),
    typeof(Features.Core.StateCallbackTests.InternalTrigger),  // Same!
    null);

Register("MultipleCallbacks",
    typeof(Features.Core.StateCallbackTests.MultiState),
    typeof(Features.Core.StateCallbackTests.MultiState),  // Same!
    typeof(Features.Core.StateCallbackTests.MultiTrigger),
    typeof(Features.Core.StateCallbackTests.MultiTrigger),  // Same!
    null);

Register("SelfTransition",
    typeof(Features.Core.StateCallbackTests.SelfState),
    typeof(Features.Core.StateCallbackTests.SelfState),  // Same!
    typeof(Features.Core.StateCallbackTests.SelfTrigger),
    typeof(Features.Core.StateCallbackTests.SelfTrigger),  // Same!
    null);
```

## 5. Generated Code Shows the Problem

### Example: ComplexCallbackMachine Generated Code
**File:** `/home/lukasz/FastFsm/FastFsm.Tests/obj/GeneratedFiles/Generator/Generator.StateMachineGenerator/global__FastFsm.Tests.Machines.ComplexCallbackMachine.Generated.cs`

```csharp
public partial class ComplexCallbackMachineFluent : 
    StateMachineBase<FastFsm.Tests.Features.Core.StateCallbackTests.ComplexCallbackState, 
                     FastFsm.Tests.Features.Core.StateCallbackTests.ComplexCallbackTrigger>
{
    // Uses fully qualified names because the enums are shared
}
```

## 6. The Real Problems

### Problem 1: Same Type "Conversion"
When Fluent and Legacy use the same enum type (e.g., `StateCallbackTests.InternalState`), EnumConverterV2 tries to "convert" from type X to type X, which makes no sense.

### Problem 2: Wrong Namespace References
MachineRegistry references `Features.Core.State` and `Features.Core.Trigger` which don't exist. The actual enums are in `GuardPermittedTriggersTests`.

### Problem 3: Test Configuration Mismatch
Tests expect different trigger values than what the enums actually contain:
- GuardPermitted test uses "X", "Y" but enum only has "Run"
- ExceptionCallback test uses "Trigger", "Reset" but enum only has "Go"

## 7. Why Tests Fail

### EnumParityTests.Triggers_HaveFullParity Failures

The test tries to validate enum parity between Fluent and Legacy:
1. Gets types from MachineRegistry
2. For machines like "InternalTransition", gets:
   - FluentTriggerType = `StateCallbackTests.InternalTrigger`
   - LegacyTriggerType = `StateCallbackTests.InternalTrigger` (SAME!)
3. Calls `ValidateEnumParity<TFluent, TLegacy>` where TFluent == TLegacy
4. ToLegacy/ToFluent methods fail because they expect different types

### DualApiMatrixTests Failures

The test tries to use triggers that don't exist:
1. MatrixConfig says GuardPermitted should use trigger "X"
2. But GuardPermittedTriggersTests.Trigger only has "Run"
3. EnumConverter.ConvertEnum fails with "Cannot convert enum value 'X' to type Trigger"

## 8. Solution Options

### Option 1: Fix EnumConverterV2 to Handle Same Types
```csharp
public static TLegacy ToLegacy<TLegacy>(object fluentValue, string machineName)
    where TLegacy : struct, Enum
{
    // Check if already the target type
    if (fluentValue is TLegacy legacyTyped)
        return legacyTyped;
    
    // Check if same type different instance
    if (fluentValue.GetType() == typeof(TLegacy))
        return (TLegacy)fluentValue;
    
    // Continue with conversion logic...
}
```

### Option 2: Create Separate Enums for Legacy
Instead of sharing enums, create separate Legacy versions:
```csharp
namespace FastFsm.Tests.Features.Core.Legacy
{
    public enum InternalState { Active, Inactive }
    public enum InternalTrigger { Update, Deactivate }
}
```

### Option 3: Fix MachineRegistry References
```csharp
Register("GuardPermitted",
    typeof(GuardPermittedTriggersTests.State),  // Correct namespace
    typeof(GuardPermittedTriggersTests.State),
    typeof(GuardPermittedTriggersTests.Trigger),  // Correct namespace
    typeof(GuardPermittedTriggersTests.Trigger),
    ...);
```

## Conclusion

The problem is NOT complex - it's a systematic issue where:
1. **Shared enums are used by both APIs** (no conversion needed)
2. **Wrong namespace references** in MachineRegistry
3. **Test configurations use wrong trigger values**
4. **EnumConverterV2 doesn't handle same-type "conversion"**

This is indeed "mozolna praca" - tedious work to fix each machine's configuration, but not conceptually difficult.