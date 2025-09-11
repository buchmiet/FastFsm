# ENUM CONVERSION ISSUES – DEEP DIVE & REPROS

## Executive Summary

The FastFsm.Tests project experiences **systematic enum conversion failures** affecting 34 out of 42 matrix tests. The root cause is a **configuration mismatch** between `MatrixConfig.MatrixEntries` (which specifies incorrect initial states) and the actual enum values in the state machines. Additionally, there's **inconsistent usage** of `InitialStateResolver` fallback logic and `EnumConverter` across wrappers.

**Key Findings:**
- 6 machines are affected by enum conversion issues
- Primary failure pattern: Hardcoded initial states in `MatrixEntries` don't match actual enum values
- Secondary issue: Some wrappers use `EnumConverter` instead of `InitialStateResolver`
- No aliases are currently defined in `EnumConverterV2.Maps`

## Taxonomy of Failures

### 1. **Incorrect Initial State Names** (Primary Issue)
- `MatrixEntries` specifies states that don't exist in the target enums
- Examples:
  - `InternalTransition`: Specifies "Active" but InitialStateResolver returns "Idle" (not in enum)
  - `PayloadStateMachine`: Specifies "Initial" which exists, but fallback logic still triggers
  - `ExceptionCallback`: Specifies "A" which exists, but EnumConverter fails

### 2. **Mixed Converter Usage** (Secondary Issue)
- Some wrappers use `EnumConverter.ConvertEnum()` which doesn't handle fallbacks
- Others use `InitialStateResolver.ResolveOrDefault()` but parse result incorrectly

### 3. **Fallback Logic Failure**
- `InitialStateResolver` returns invalid values (e.g., "Idle" for InternalState which only has Active/Inactive)
- The resolver's common state list prioritizes "Idle" over "Active"

### 4. **No Alias Mappings**
- `EnumConverterV2.Maps` is empty - no manual mappings defined
- No `[EnumAlias]` attributes found on enum values

## Per-Machine Maps

| Machine | Fluent States | Legacy States | Issue | Root Cause |
|---------|--------------|---------------|-------|------------|
| **GuardPermitted** | Idle, Done | Idle, Done | "A" not found | MatrixConfig passes null, EnumConverter gets "A" somehow |
| **InternalTransition** | Active, Inactive | Active, Inactive | "Idle" not found | InitialStateResolver fallback returns "Idle" |
| **PayloadStateMachine** | Initial, Processing, Completed, Failed | Initial, Processing, Completed, Failed | "New" not found | Old default value in code |
| **FullMultiPayload** | Initial, Configured, Processing, Failed | Initial, Configured, Processing, Failed | "Idle" not found | Wrong fallback |
| **ExceptionCallback** | A, B | A, B | "Normal" not found | Wrong MatrixEntries value |
| **CoreBenchmark** | A, B, C, D | A, B, C, D | ✓ Working | Correct config |

## Representative Stack Traces

### 1. InitialStateResolver Returns Invalid State
```
System.ArgumentException : Requested value 'Idle' was not found.
  at System.Enum.Parse(Type enumType, String value, Boolean ignoreCase)
  at InternalTransitionMachineFluentWrapper..ctor(String initialStateName)
```
**Cause:** InitialStateResolver returns "Idle" (from common states list) but InternalState enum only has Active/Inactive.

### 2. EnumConverter Fails Without Fallback
```
System.InvalidOperationException : Cannot convert enum value 'A' to type State. Available values: Idle, Done
  at EnumConverter.ConvertEnum(Type targetType, String name)
  at GuardPermittedFluentWrapper..ctor(String initialStateName)
```
**Cause:** GuardPermitted wrapper uses EnumConverter which doesn't have fallback logic.

### 3. Hardcoded Default Values
```
System.ArgumentException : Requested value 'New' was not found.
  at PayloadStateMachineFluentWrapper..ctor(String initialStateName)
```
**Cause:** Legacy default values still present in some code paths.

## Minimal Repros

```bash
# Reproduce InternalTransition failure
dotnet test --filter "FullyQualifiedName~DualApiMatrixTests.Machine_BasicOperations_WorkOnBothApis" \
  --filter "machineName=InternalTransition&apiType=Fluent"
# Expected: ArgumentException 'Idle' was not found

# Reproduce GuardPermitted failure  
dotnet test --filter "FullyQualifiedName~DualApiMatrixTests.Machine_BasicOperations_WorkOnBothApis" \
  --filter "machineName=GuardPermitted&apiType=Fluent"
# Expected: InvalidOperationException Cannot convert 'A' to State
```

## Proposed Remediation Plan

### P0 - Critical Fixes (Immediate)
1. **Fix InitialStateResolver Priority Order**
   - Change common states order to: ["Initial", "A", "Active", "New", "Idle", ...]
   - This ensures correct fallback for each machine type

2. **Fix MatrixConfig.MatrixEntries**
   ```csharp
   new MatrixEntry("InternalTransition", null, ...), // Let resolver handle
   new MatrixEntry("ExceptionCallback", null, ...),  // Let resolver handle
   new MatrixEntry("PayloadStateMachine", null, ...), // Let resolver handle
   ```

3. **Replace EnumConverter with InitialStateResolver in GuardPermittedWrappers**
   ```csharp
   // Before: var state = EnumConverter.ConvertEnum<State>(initialStateName);
   // After:
   var resolvedName = InitialStateResolver.ResolveOrDefault<State>("GuardPermitted", initialStateName);
   var state = (State)Enum.Parse(typeof(State), resolvedName);
   ```

### P1 - Important Fixes (Next Sprint)
1. **Add Alias Mappings to EnumConverterV2**
   ```csharp
   Maps["GuardPermitted"] = new() {
       ["ToFluent.A"] = "Idle",
       ["ToLegacy.Idle"] = "A"
   };
   ```

2. **Add Validation Tests**
   - Verify each machine's initial state exists in enum
   - Validate trigger sequences are valid

### P2 - Long-term Improvements
1. **Generate Aliases from Reflection**
   - Auto-detect naming patterns (e.g., State_A vs StateA)
   - Build Maps automatically

2. **Unify Conversion Strategy**
   - All wrappers should use same pattern
   - Consider base class implementation

## Acceptance Criteria

✅ **All tests must pass:**
- `DualApiMatrixTests`: 36 tests, 0 failures
- `CoverageParityTests`: 6 tests, 0 failures
- `MatrixConfigValidationTests`: All green

✅ **Metrics:**
- 100% of machines in MatrixConfig can be instantiated
- 100% of trigger sequences can execute without exceptions
- Bidirectional enum conversion works for all state/trigger pairs

## Next Steps

1. **Apply P0 fixes** to InitialStateResolver and GuardPermittedWrappers
2. **Update MatrixConfig.MatrixEntries** to use null for all initial states
3. **Run full test suite** to verify fixes
4. **Add EnumConverterV2.Maps entries** for known problematic conversions
5. **Create PR** with fixes and updated tests