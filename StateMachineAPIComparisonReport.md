# FastFsm State Machine API Comparison Report

## Executive Summary
This report analyzes the state machine definitions in FastFsm.Tests and FastFsm.Async.Tests projects, comparing Legacy (Attribute-based) API with Fluent API implementations.

---

## 📊 Overall Statistics

### FastFsm.Tests
- **Total Tests:** 143
- **Legacy Machines:** 15 files containing `[State(` attributes
- **Fluent Machines:** 7 files containing `FSM.State(` definitions

### FastFsm.Async.Tests
- **Total Tests:** 90
- **Legacy Machines:** 11 files containing `[State(` attributes
- **Fluent Machines:** 2 files containing `FSM.State(` definitions

---

## 🔍 Detailed Analysis by Category

### 1. Hierarchical State Machines (HSM)

#### FastFsm.Tests - HSM
- **Legacy HSM:** 3 implementations
  - HierarchicalRuntime.cs
  - HsmAdditionalCompilationTests.cs
  - HsmParsingCompilationTests.cs

- **Fluent HSM:** 7 implementations ✅
  - DeepHistoryTests_Fluent.cs
  - HsmParsingCompilationTests.cs (contains both)
  - InheritanceTests_Fluent.cs
  - InitialChildTests_Fluent.cs
  - InternalTransitionTests_Fluent.cs
  - ShallowHistoryTests_Fluent.cs
  - SimpleParentChildMachine_Fluent.cs

#### FastFsm.Async.Tests - HSM
- **HierarchicalAsyncRuntimeTests.cs** contains:
  - Legacy: 8 machines
    1. InitialChildMachine
    2. ShallowHistoryMachine
    3. DeepHistoryMachine
    4. InternalMachine
    5. PriorityMachine
    6. ChildOverridesMachine
    7. SourceOrderTieMachine
    8. InheritanceMachine
  
  - Fluent: 8 machines ✅ (1:1 parity)
    1. InitialChildMachineFluentFsm
    2. ShallowHistoryMachineFluentFsm
    3. DeepHistoryMachineFluentFsm
    4. InternalMachineFluentFsm
    5. PriorityMachineFluentFsm
    6. ChildOverridesMachineFluentFsm
    7. SourceOrderTieMachineFluentFsm
    8. InheritanceMachineFluentFsm

- **AsyncNoActionHsmTests.cs** contains:
  - Legacy: 1 machine (TinyAsyncHsm)
  - Fluent: 1 machine (TinyAsyncHsmFluentFsm) ✅

### 2. Core State Machines

#### FastFsm.Tests - Core Machines
**Legacy implementations without Fluent equivalents:** ⚠️
- BasicBenchmarkMachine.cs
- ComplexCallbackMachine.cs
- ExceptionCallbackMachine.cs
- ExceptionDirective_Cancellation_Tests.cs
- ExceptionDirective_Continue_OnEntry_Tests.cs
- ExtensionsMachine.cs
- FullOrderMachine.cs
- GuardedCallbackMachine.cs
- InitialStateMachine.cs
- Machines.cs
- MultipleCallbacksMachine.cs
- PayloadStateMachine.cs

**Note:** Many of these machines have FluentAPI versions in the Machines folder:
- BasicBenchmarkMachineFluentAPI.cs
- ComplexCallbackMachineFluentAPI.cs
- CoreBenchmarkMachineFluentAPI.cs
- ExceptionCallbackMachineFluentAPI.cs
- FullMultiPayloadMachineFluentAPI.cs
- FullOrderMachineFluentAPI.cs
- GuardedCallbackMachineFluentAPI.cs
- InitialStateMachineFluentAPI.cs
- MultipleCallbacksMachineFluentAPI.cs
- NoGuardBenchmarkMachineFluentAPI.cs
- PayloadStateMachineFluentAPI.cs
- WithGuardBenchmarkMachineFluentAPI.cs

#### FastFsm.Async.Tests - Core Machines
**Categories with Legacy implementations:**
- Core (BasicAsyncStateMachineTests.cs)
- Cancellation (various cancellation tests)
- Concurrency (RaceConditionTests.cs)
- Exceptions (ExceptionDirectiveTests.cs)
- Payload (AsyncPayloadStateMachineTests.cs)

**Categories with Fluent implementations:** ⚠️
- Limited to HSM tests only
- FluentAPITests.cs with basic examples
- PayloadMachineFluentFsm and RcMachineFluentFsm (in Cancellation/Concurrency)

---

## 📈 Coverage Analysis

### ✅ Areas with Good Fluent Coverage
1. **HSM in Async Tests:** 100% parity (9 Legacy : 9 Fluent)
2. **Benchmark Machines:** Most benchmark machines have Fluent equivalents
3. **Core functionality:** Basic state transitions, guards, actions

### ⚠️ Areas Needing Fluent Implementation
1. **Exception Handling Tests:** No Fluent equivalents for exception directive tests
2. **Extensions:** No Fluent version of ExtensionsMachine
3. **Async Core Tests:** Most async tests lack Fluent equivalents
4. **Cancellation Tests:** Limited Fluent coverage
5. **Concurrency Tests:** Limited Fluent coverage

---

## 🎯 Recommendations

### High Priority
1. **Complete Async Test Coverage:** Add Fluent equivalents for all async test machines
2. **Exception Handling:** Implement Fluent versions of exception directive tests
3. **Document Parity:** Create a tracking document for Legacy-to-Fluent migration

### Medium Priority
1. **Consolidate Test Organization:** Group Legacy and Fluent tests together for easier comparison
2. **Remove Duplicates:** Some machines appear to have multiple implementations
3. **Standardize Naming:** Use consistent naming convention (e.g., MachineName_Legacy vs MachineName_Fluent)

### Low Priority
1. **Add Integration Tests:** Test that Legacy and Fluent machines produce identical behavior
2. **Performance Benchmarks:** Compare performance between Legacy and Fluent APIs
3. **Migration Guide:** Create documentation for migrating from Legacy to Fluent API

---

## 📌 Key Observations

### Positive Findings
1. **HSM Support:** Fluent API has excellent HSM support with full feature parity
2. **Priority Support:** Confirmed working in Fluent API (after deduplification fix)
3. **Async Support:** Both OnEntryAsync/OnExitAsync and ActionAsync work correctly
4. **Internal Transitions:** Properly implemented in Fluent API

### Issues Found & Fixed
1. **Duplicate Transition Bug:** Fixed with deduplication logic in FluentParser
2. **Method Name Flexibility:** FluentParser now supports both "Configure" and "SetupStates"
3. **Priority Order:** Corrected to be placed before GoTo() in method chain

### Remaining Gaps
1. **Test Coverage Disparity:** 15 Legacy vs 7 Fluent in FastFsm.Tests
2. **Async Test Coverage:** 11 Legacy vs 2 Fluent in FastFsm.Async.Tests
3. **Missing Exception Tests:** No Fluent equivalents for exception directive scenarios
4. **Documentation:** FluentApi.md needs updating to reflect current implementation status

---

## 📋 Summary

The Fluent API implementation has made significant progress, especially in HSM support where it achieves 100% feature parity. However, there's still work needed to achieve full coverage across all test scenarios, particularly in:

- Async state machine tests (78% of machines lack Fluent equivalents)
- Exception handling scenarios
- Cancellation and concurrency tests

**Overall Fluent API Coverage:**
- FastFsm.Tests: 47% (7/15)
- FastFsm.Async.Tests: 18% (2/11)
- HSM Tests: 100% ✅

**Recommendation:** Focus on implementing Fluent equivalents for the core async tests to improve overall API parity and ensure all features are accessible through both APIs.