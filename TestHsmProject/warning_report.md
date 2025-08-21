# FastFsm.Net 0.6.9.5 Warning Report

## Test Environment
- **Package Version**: FastFsm.Net 0.6.9.5 (from local NuGet source)
- **Project Type**: Console Application
- **Target Framework**: .NET 9.0
- **Test Date**: August 21, 2025
- **Test Project**: HsmMediaPlayer (Hierarchical State Machine example from README.md)

## Build Results

### Summary
The project builds successfully but generates **7 warnings** related to the generated code.

### Warning Details

#### CS0108 Warnings (6 occurrences)
These warnings indicate that generated fields and methods in the `MediaPlayer` class hide inherited members from `StateMachineBase<PlayerState, PlayerTrigger>`.

1. **Line 31**: `MediaPlayer.s_parent` hides inherited member
2. **Line 32**: `MediaPlayer.s_depth` hides inherited member  
3. **Line 33**: `MediaPlayer.s_initialChild` hides inherited member
4. **Line 34**: `MediaPlayer.s_history` hides inherited member
5. **Line 56**: `MediaPlayer._lastActiveChild` hides inherited member
6. **Line 79**: `MediaPlayer.GetCompositeEntryTarget(int)` hides inherited member

**Recommendation**: Add the `new` keyword to these declarations in the generated code to explicitly indicate intentional hiding.

#### CS0168 Warning (1 occurrence)
- **File**: ExtensionRunner.cs (line 86)
- **Issue**: Variable `ex` is declared but never used
- **Location**: `/home/lukasz/.nuget/packages/fastfsm.net/0.6.9.5/contentFiles/cs/any/ExtensionRunner.cs`

**Recommendation**: Either use the exception variable or replace `catch (Exception ex)` with `catch (Exception)`.

## Impact Assessment

### Severity: **Low to Medium**

These warnings do not affect the functionality of the state machine but indicate code quality issues that should be addressed:

1. **CS0108 warnings** suggest that the source generator is creating members that conflict with the base class. While this works due to member hiding, it's not ideal for code clarity and may cause confusion.

2. **CS0168 warning** is a minor code quality issue that should be easily fixed.

## Recommended Fixes for Version 0.6.9.6

### For CS0108 Warnings
In the source generator, when generating static and instance fields for hierarchical state machines, add the `new` keyword:

```csharp
// Instead of:
private static readonly int[] s_parent = ...;

// Generate:
private static new readonly int[] s_parent = ...;
```

### For CS0168 Warning
In ExtensionRunner.cs:

```csharp
// Instead of:
catch (Exception ex)
{
    // code that doesn't use ex
}

// Use:
catch (Exception)
{
    // same code
}
```

## Comparison with Previous Version

Based on the user's comment, version 0.6.9.5 was expected to prevent warning generation. However, the warnings persist, indicating that:

1. The fixes may not have been fully applied to the source generator
2. The hierarchical state machine feature may introduce new scenarios not covered by the fixes

## Conclusion

While FastFsm.Net 0.6.9.5 successfully generates and executes hierarchical state machines, it still produces compiler warnings that should be addressed in the next version. These warnings are primarily cosmetic and do not affect runtime behavior, but they may trigger build failures in projects with `TreatWarningsAsErrors` enabled.

## Test Code Used

The test successfully implemented a Media Player HSM with:
- Parent state (PowerOn) with shallow history
- Three child states (Stopped, Playing, Paused)
- Internal transitions
- All features working as expected despite the warnings

The functionality is intact, but the code generation needs refinement to eliminate these warnings completely.