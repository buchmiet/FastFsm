# Test API Coverage Report - Fluent vs Legacy

## Summary

- **Total Fluent machines**: 45
- **Total Legacy machines**: 43
- **Machines with both versions**: 38
- **Fluent-only machines**: 7
- **Legacy-only machines**: 6
- **Coverage**: 84.4% (38/45 Fluent), 88.4% (38/43 Legacy)

## Machines with Both Fluent and Legacy Versions ✅

| Category | Fluent API | Legacy API | Status |
|----------|------------|------------|--------|
| **Features/EdgeCases** |
| NoTransitions | NoTransitionsMachine.Fluent.cs | NoTransitionsMachine.Legacy.cs | ✅ Parity |
| **Features/Exceptions** |
| ActionException | ActionExceptionTests.Fluent.cs | ActionExceptionTests.Legacy.cs | ✅ Parity |
| Cancellation | ExceptionDirective_Cancellation_Tests.Fluent.cs | ExceptionDirective_Cancellation_Tests.Legacy.cs | ✅ Parity |
| ContinueAction | ExceptionDirective_Continue_Action_Tests.Fluent.cs | ExceptionDirective_Continue_Action_Tests.Legacy.cs | ✅ Parity |
| ContinueOnEntry | ExceptionDirective_Continue_OnEntry_Tests.Fluent.cs | ExceptionDirective_Continue_OnEntry_Tests.Legacy.cs | ✅ Parity |
| Positions | ExceptionDirective_Positions_Tests.Fluent.cs | ExceptionDirective_Positions_Tests.Legacy.cs | ✅ Parity |
| PropagateAction | ExceptionDirective_Propagate_Action_Tests.Fluent.cs | ExceptionDirective_Propagate_Action_Tests.Legacy.cs | ✅ Parity |
| ExceptionHandling | ExceptionHandlingTests.Fluent.cs | ExceptionHandlingTests.Legacy.cs | ✅ Parity |
| TestMachine | TestMachine.Fluent.cs | TestMachine.Legacy.cs | ✅ Parity |
| **Features/Extensions** |
| Extensions | ExtensionsMachine.Fluent.cs | ExtensionsMachine.Legacy.cs | ✅ Parity |
| **Features/Hsm/CompileTime** |
| HsmParsing | HsmParsingCompilationTests.Fluent.cs | HsmParsingCompilationTests.Legacy.cs | ✅ Parity |
| **Features/Payload** |
| Machines | Machines.Fluent.cs | Machines.Legacy.cs | ✅ Parity |
| PayloadVariant | PayloadVariantTests.Fluent.cs | PayloadVariantTests.Legacy.cs | ✅ Parity |
| **Machines** |
| BasicBenchmark | BasicBenchmarkMachine.Fluent.cs | BasicBenchmarkMachine.Legacy.cs | ✅ Parity |
| CallbackOrder | CallbackOrderMachine.Fluent.cs | CallbackOrderMachine.Legacy.cs | ✅ Parity |
| CaseSensitive | CaseSensitiveMachine.Fluent.cs | CaseSensitiveMachine.Legacy.cs | ✅ Parity |
| ComplexCallback | ComplexCallbackMachine.Fluent.cs | ComplexCallbackMachine.Legacy.cs | ✅ Parity |
| ConflictingNames | ConflictingNamesMachine.Fluent.cs | ConflictingNamesMachine.Legacy.cs | ✅ Parity |
| CoreBenchmark | CoreBenchmarkMachine.Fluent.cs | CoreBenchmarkMachine.Legacy.cs | ✅ Parity |
| ExceptionCallback | ExceptionCallbackMachine.Fluent.cs | ExceptionCallbackMachine.Legacy.cs | ✅ Parity |
| FullMultiPayload | FullMultiPayloadMachine.Fluent.cs | FullMultiPayloadMachine.Legacy.cs | ✅ Parity |
| FullOrder | FullOrderMachine.Fluent.cs | FullOrderMachine.Legacy.cs | ✅ Parity |
| GuardedCallback | GuardedCallbackMachine.Fluent.cs | GuardedCallbackMachine.Legacy.cs | ✅ Parity |
| InitialState | InitialStateMachine.Fluent.cs | InitialStateMachine.Legacy.cs | ✅ Parity |
| InternalOnly | InternalOnlyMachine.Fluent.cs | InternalOnlyMachine.Legacy.cs | ✅ Parity |
| InternalTransition | InternalTransitionMachine.Fluent.cs | InternalTransitionMachine.Legacy.cs | ✅ Parity |
| KeywordState | KeywordStateMachine.Fluent.cs | KeywordStateMachine.Legacy.cs | ✅ Parity |
| LongName | LongNameMachine.Fluent.cs | LongNameMachine.Legacy.cs | ✅ Parity |
| MultipleCallbacks | MultipleCallbacksMachine.Fluent.cs | MultipleCallbacksMachine.Legacy.cs | ✅ Parity |
| NoGuardBenchmark | NoGuardBenchmarkMachine.Fluent.cs | NoGuardBenchmarkMachine.Legacy.cs | ✅ Parity |
| Numeric | NumericMachine.Fluent.cs | NumericMachine.Legacy.cs | ✅ Parity |
| PayloadState | PayloadStateMachine.Fluent.cs | PayloadStateMachine.Legacy.cs | ✅ Parity |
| SelfTransition | SelfTransitionMachine.Fluent.cs | SelfTransitionMachine.Legacy.cs | ✅ Parity |
| SingleState | SingleStateMachine.Fluent.cs | SingleStateMachine.Legacy.cs | ✅ Parity |
| Unicode | UnicodeMachine.Fluent.cs | UnicodeMachine.Legacy.cs | ✅ Parity |
| Unreachable | UnreachableMachine.Fluent.cs | UnreachableMachine.Legacy.cs | ✅ Parity |
| WithGuardBenchmark | WithGuardBenchmarkMachine.Fluent.cs | WithGuardBenchmarkMachine.Legacy.cs | ✅ Parity |

## Fluent-Only Machines (Missing Legacy) ❌

| Category | Fluent API | Legacy API | Action Required |
|----------|------------|------------|-----------------|
| **Features/Hsm/Runtime** |
| DebugHsm | DebugHsmTest.Fluent.cs | ❌ Missing | Create Legacy version |
| DeepHistory | DeepHistoryTests.Fluent.cs | ❌ Missing | Create Legacy version |
| HsmIsInHierarchy | HsmIsInHierarchyTests.Fluent.cs | ❌ Missing | Create Legacy version |
| Inheritance | InheritanceTests.Fluent.cs | ❌ Missing | Create Legacy version |
| InitialChild | InitialChildTests.Fluent.cs | ❌ Missing | Create Legacy version |
| InternalTransition | InternalTransitionTests.Fluent.cs | ❌ Missing | Create Legacy version |
| ShallowHistory | ShallowHistoryTests.Fluent.cs | ❌ Missing | Create Legacy version |
| SimpleParentChild | SimpleParentChildMachine.Fluent.cs | ❌ Missing | Create Legacy version |

## Legacy-Only Machines (Missing Fluent) ❌

| Category | Fluent API | Legacy API | Action Required |
|----------|------------|------------|-----------------|
| **Features/Core** |
| StateCallback | ❌ Missing | StateCallbackTests.Legacy.cs | Create Fluent version |
| **Features/EdgeCases** |
| EmptyMachine | ❌ Missing | EmptyMachineTests.Legacy.cs | Create Fluent version |
| NameCollision | ❌ Missing | NameCollisionTests.Legacy.cs | Create Fluent version |
| **Features/Hsm/CompileTime** |
| HsmAdditional | ❌ Missing | HsmAdditionalCompilationTests.Legacy.cs | Create Fluent version |
| **Features/Hsm/Runtime** |
| HierarchicalRuntime | ❌ Missing | HierarchicalRuntime.Legacy.cs | Create Fluent version |
| **Features/Performance** |
| Benchmark | ❌ Missing | BenchmarkTests.Legacy.cs | Create Fluent version |

## Naming Consistency Analysis

### ✅ Good Naming Patterns
- Most machines follow consistent naming: `MachineName.Fluent.cs` / `MachineName.Legacy.cs`
- Test files follow pattern: `FeatureTests.Fluent.cs` / `FeatureTests.Legacy.cs`

### ⚠️ Potential Naming Issues
1. **HSM Runtime Tests**: Fluent-only HSM tests might be intentional (HSM is newer feature)
2. **Performance Tests**: `BenchmarkTests.Legacy.cs` doesn't have Fluent counterpart
3. **Core Tests**: `StateCallbackTests.Legacy.cs` missing Fluent version

## Recommendations

### Priority 1: Create Missing Legacy Versions for HSM Runtime
The HSM (Hierarchical State Machine) runtime tests are all Fluent-only. These should have Legacy equivalents:
- [ ] Create `DebugHsmTest.Legacy.cs`
- [ ] Create `DeepHistoryTests.Legacy.cs`
- [ ] Create `HsmIsInHierarchyTests.Legacy.cs`
- [ ] Create `InheritanceTests.Legacy.cs`
- [ ] Create `InitialChildTests.Legacy.cs`
- [ ] Create `InternalTransitionTests.Legacy.cs`
- [ ] Create `ShallowHistoryTests.Legacy.cs`
- [ ] Create `SimpleParentChildMachine.Legacy.cs`

### Priority 2: Create Missing Fluent Versions
- [ ] Create `StateCallbackTests.Fluent.cs`
- [ ] Create `EmptyMachineTests.Fluent.cs`
- [ ] Create `NameCollisionTests.Fluent.cs`
- [ ] Create `HsmAdditionalCompilationTests.Fluent.cs`
- [ ] Create `HierarchicalRuntime.Fluent.cs`
- [ ] Create `BenchmarkTests.Fluent.cs`

### Priority 3: Test Unification
After achieving parity, ensure all test methods test both Fluent and Legacy versions:
- [ ] Review test methods to ensure they instantiate and test both API versions
- [ ] Consider using test theories/parameterized tests for API version selection
- [ ] Ensure consistent behavior between Fluent and Legacy implementations

## Next Steps

1. **Immediate**: Focus on HSM Runtime tests - create Legacy versions
2. **Short-term**: Create missing Fluent versions for Core and EdgeCases
3. **Long-term**: Implement test parameterization to automatically test both APIs