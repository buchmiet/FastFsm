# Enum Mapping Report - FastFSM Test Suite

## Summary
This report documents all enum mappings between Fluent and Legacy APIs in the FastFSM test suite, identifying discrepancies and providing solutions for achieving full parity.

## Current Status
- **Total Machines**: 24
- **With Complete Enum Definitions**: Analysis in progress
- **Missing Mappings**: To be determined by EnumParityTests

## Enum Definitions by Machine

### 1. CoreBenchmark
- **State Enums**: 
  - Fluent: `BenchmarkTests.BenchmarkState` (A, B, C, D, E, F, G, H, I, J)
  - Legacy: `BenchmarkTestsLegacy.BenchmarkState` (A, B, C, D, E, F, G, H, I, J)
- **Trigger Enums**:
  - Fluent: `BenchmarkTests.BenchmarkTrigger` (Next, Previous)
  - Legacy: `BenchmarkTestsLegacy.BenchmarkTrigger` (Next, Previous)
- **Status**: ✅ Full parity

### 2. GuardPermitted
- **State/Trigger**: Uses `Features.Core.State` and `Features.Core.Trigger` for both APIs
- **Status**: ✅ Full parity (same enums)

### 3. BasicBenchmark
- **Uses same enums as CoreBenchmark**
- **Status**: ✅ Full parity

### 4. CallbackOrder
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `CallbackState` (A, B, C)
- **Trigger**: `CallbackTrigger` (Next)
- **Status**: 🔍 Needs verification

### 5. CaseSensitive
- **Location**: `Features/EdgeCases/NameCollisionTests.cs`
- **State**: `CaseSensitiveState` (state, State, STATE)
- **Trigger**: `CaseSensitiveTrigger` (go, Go, GO)
- **Status**: ⚠️ Case sensitivity may cause issues

### 6. ComplexCallback
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `ComplexCallbackState` (Idle, Ready, Processing, Done)
- **Trigger**: `ComplexCallbackTrigger` (Start, Process, Complete)
- **Registry Issue**: Referenced as `ComplexState/ComplexTrigger` - needs alias

### 7. ConflictingNames
- **Location**: `Features/EdgeCases/NameCollisionTests.cs`
- **State**: `ConflictState` (A, B)
- **Trigger**: `ConflictTrigger` (Go)
- **Registry Issue**: Referenced as `ConflictingState/ConflictingTrigger` - needs alias

### 8. ExceptionCallback
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `ExceptionState` (A, B)
- **Trigger**: `ExceptionTrigger` (Go)
- **Status**: 🔍 Needs verification

### 9. FullMultiPayload
- **Location**: `Features/Payload/PayloadTestData.cs`
- **State**: `MultiState` (Initial, Configured, Processing, Failed)
- **Trigger**: `MultiTrigger` (Configure, Process, Error)
- **Registry Issue**: Referenced as `MultiPayloadState/MultiPayloadTrigger` - needs alias

### 10. FullOrder
- **Location**: `Machines/FullOrderMachine.Legacy.cs`
- **State**: `OrderState` (New, Processing, Paid, Shipped, Delivered, Cancelled)
- **Trigger**: `OrderTrigger` (Process, Pay, Ship, Deliver, Cancel, Refund)
- **Status**: 🔍 Needs verification

### 11. GuardedCallback
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `GuardedState` (A, B)
- **Trigger**: `GuardedTrigger` (Go)
- **Status**: 🔍 Needs verification

### 12. InitialState
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `InitialState` (Start, Next)
- **Trigger**: `InitialTrigger` (Go)
- **Status**: 🔍 Needs verification

### 13. InternalOnly
- **Location**: `Features/EdgeCases/EmptyMachineTests.cs`
- **State**: `InternalOnlyState` (Static)
- **Trigger**: `InternalOnlyTrigger` (Action)
- **Status**: 🔍 Needs verification

### 14. InternalTransition
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `InternalState` (Active, Inactive)
- **Trigger**: `InternalTrigger` (Update, Deactivate)
- **Registry Issue**: Referenced as `InternalTransitionState/InternalTransitionTrigger` - needs alias

### 15. KeywordState
- **Location**: `Features/EdgeCases/NameCollisionTests.cs`
- **State**: `KeywordState` (@class, @return, @void, @int, @interface, @namespace)
- **Trigger**: `KeywordTrigger` (@goto, @continue, @break, @new, @throw)
- **Registry Issue**: Referenced as `KeywordStateEnum/KeywordTriggerEnum` - needs alias

### 16. LongName
- **Location**: `Features/EdgeCases/NameCollisionTests.cs`
- **State**: `LongNameState` (very long names)
- **Trigger**: `LongNameTrigger` (very long names)
- **Registry Issue**: Referenced as `VeryLongAndDescriptive...` - needs alias

### 17. MultipleCallbacks
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `MultiState` (A, B)
- **Trigger**: `MultiTrigger` (Go)
- **Registry Issue**: Referenced as `MultipleCallbackState/MultipleCallbackTrigger` - needs alias

### 18. NoGuardBenchmark
- **Uses same enums as CoreBenchmark**
- **Status**: ✅ Full parity

### 19. Numeric
- **Location**: `Features/EdgeCases/NameCollisionTests.cs`
- **State**: `NumericState` (_1Start, _3Middle, _5End)
- **Trigger**: `NumericTrigger` (_2Next, _4Continue)
- **Status**: 🔍 Needs verification

### 20. PayloadState
- **Location**: `FluentAPI_SpecificTests.cs`
- **State**: `PayloadState` (Ready, Processing, Complete)
- **Trigger**: `PayloadTrigger` (Submit, Process, Finish)
- **Status**: 🔍 Needs verification

### 21. SelfTransition
- **Location**: `Features/Core/StateCallbackTests.cs`
- **State**: `SelfState` (Active)
- **Trigger**: `SelfTrigger` (Refresh)
- **Registry Issue**: Referenced as `SelfTransitionState/SelfTransitionTrigger` - needs alias

### 22. SingleState
- **Location**: `Features/EdgeCases/EmptyMachineTests.cs`
- **State**: `SingleState` (Only)
- **Trigger**: `SingleTrigger` (Loop)
- **Registry Issue**: Referenced as `SingleStateEnum/SingleTriggerEnum` - needs alias

### 23. Unicode
- **Location**: `Features/EdgeCases/NameCollisionTests.cs`
- **State**: `UnicodeState` (αlpha, βeta, Ωmega)
- **Trigger**: `UnicodeTrigger` (αlpha, βeta, γamma)
- **Status**: 🔍 Needs verification

### 24. Unreachable
- **Location**: `Features/EdgeCases/EmptyMachineTests.cs`
- **State**: `UnreachableState` (Start, Connected, Isolated)
- **Trigger**: `UnreachableTrigger` (Connect, Disconnect, Isolate)
- **Status**: 🔍 Needs verification

### 25. WithGuardBenchmark
- **Uses same enums as CoreBenchmark**
- **Status**: ✅ Full parity

## Required Enum Aliases

The following aliases need to be added to `EnumConverterV2.Maps` for proper mapping:

```csharp
// ComplexCallback machine
Maps["ComplexCallback"]["ToFluent.ComplexState"] = "ComplexCallbackState";
Maps["ComplexCallback"]["ToLegacy.ComplexCallbackState"] = "ComplexState";

// ConflictingNames machine
Maps["ConflictingNames"]["ToFluent.ConflictingState"] = "ConflictState";
Maps["ConflictingNames"]["ToLegacy.ConflictState"] = "ConflictingState";

// FullMultiPayload machine
Maps["FullMultiPayload"]["ToFluent.MultiPayloadState"] = "MultiState";
Maps["FullMultiPayload"]["ToLegacy.MultiState"] = "MultiPayloadState";

// InternalTransition machine
Maps["InternalTransition"]["ToFluent.InternalTransitionState"] = "InternalState";
Maps["InternalTransition"]["ToLegacy.InternalState"] = "InternalTransitionState";

// KeywordState machine
Maps["KeywordState"]["ToFluent.KeywordStateEnum"] = "KeywordState";
Maps["KeywordState"]["ToLegacy.KeywordState"] = "KeywordStateEnum";

// LongName machine
Maps["LongName"]["ToFluent.VeryLongAndDescriptiveStateNameForTestingPurposes"] = "LongNameState";
Maps["LongName"]["ToLegacy.LongNameState"] = "VeryLongAndDescriptiveStateNameForTestingPurposes";

// MultipleCallbacks machine
Maps["MultipleCallbacks"]["ToFluent.MultipleCallbackState"] = "MultiState";
Maps["MultipleCallbacks"]["ToLegacy.MultiState"] = "MultipleCallbackState";

// SelfTransition machine
Maps["SelfTransition"]["ToFluent.SelfTransitionState"] = "SelfState";
Maps["SelfTransition"]["ToLegacy.SelfState"] = "SelfTransitionState";

// SingleState machine
Maps["SingleState"]["ToFluent.SingleStateEnum"] = "SingleState";
Maps["SingleState"]["ToLegacy.SingleState"] = "SingleStateEnum";
```

## Action Items

1. ✅ **Completed**: Created EnumConverterV2 with bidirectional mapping support
2. ✅ **Completed**: Created MachineRegistry to catalog all machines
3. ✅ **Completed**: Added EnumAlias attribute support
4. 🔄 **In Progress**: Fix enum type references in MachineRegistry
5. ⏳ **Pending**: Run EnumParityTests to validate all mappings
6. ⏳ **Pending**: Create wrappers for remaining 22 machines
7. ⏳ **Pending**: Add all required enum aliases to EnumConverterV2.Maps

## Next Steps

1. Update MachineRegistry with correct enum type references
2. Build and run EnumParityTests to identify exact discrepancies
3. Add all required aliases to EnumConverterV2.Maps
4. Create wrapper implementations for all machines
5. Achieve 100% test coverage for both APIs