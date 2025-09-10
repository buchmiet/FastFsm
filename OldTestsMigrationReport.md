# Old.Tests Migration Report

## Overview
The `old.tests` folder contains the original test suite from before the Fluent API implementation. This analysis identifies which Legacy machines and tests need to be migrated to achieve full parity.

## Structure Comparison

### old.tests Structure
```
old.tests/
├── Machines/ (24 files)
├── Features/
│   ├── Core/ (3 files)
│   ├── EdgeCases/ (3 files)
│   ├── Exceptions/ (9 files)
│   ├── Extensions/ (4 files)
│   ├── Hsm/
│   │   ├── CompileTime/ (2 files)
│   │   └── Runtime/ (3 files)
│   ├── Integration/ (1 file)
│   ├── Lifecycle/ (2 files)
│   ├── Payload/ (2 files)
│   └── Performance/ (1 file)
```

Total: 55 C# files

## Migration Status

### ✅ Already Migrated (have both Fluent and Legacy in FastFsm.Tests)
- All files in `Machines/` folder (24 files)
- Most files in `Features/Exceptions/` 
- Files in `Features/Extensions/`
- Files in `Features/Payload/`

### ❌ Need Migration from old.tests to FastFsm.Tests

#### HSM Runtime Tests
| old.tests File | Current Status | Action Required |
|----------------|----------------|-----------------|
| `HierarchicalRuntime.cs` | Exists as `HierarchicalRuntime.Legacy.cs` | Need Fluent version |
| `HsmIsInHierarchyTests.cs` | Has Fluent version only | Need to verify/port Legacy |
| `debug_history_test.cs` | Maps to `DebugHsmTest.Fluent.cs` | Need Legacy version |

#### Core Tests
| old.tests File | Current Status | Action Required |
|----------------|----------------|-----------------|
| `StateCallbackTests.cs` | Exists as `StateCallbackTests.Legacy.cs` | Need Fluent version |
| `CoreMinimalTests.cs` | Not found in FastFsm.Tests | Need to port both versions |
| `GuardPermittedTriggersTests.cs` | Not found in FastFsm.Tests | Need to port both versions |

#### EdgeCases Tests
| old.tests File | Current Status | Action Required |
|----------------|----------------|-----------------|
| `EmptyMachineTests.cs` | Exists as `EmptyMachineTests.Legacy.cs` | Need Fluent version |
| `NameCollisionTests.cs` | Exists as `NameCollisionTests.Legacy.cs` | Need Fluent version |
| `NoTransitionsMachine.cs` | Has both versions ✅ | Complete |

#### Performance Tests
| old.tests File | Current Status | Action Required |
|----------------|----------------|-----------------|
| `BenchmarkTests.cs` | Exists as `BenchmarkTests.Legacy.cs` | Need Fluent version |

#### Integration Tests
| old.tests File | Current Status | Action Required |
|----------------|----------------|-----------------|
| `LifecycleIntegrationTests.cs` | Not found in FastFsm.Tests | Need to port both versions |

#### Lifecycle Tests
| old.tests File | Current Status | Action Required |
|----------------|----------------|-----------------|
| `LifecycleInitializationTests.cs` | Not found in FastFsm.Tests | Need to port both versions |
| `LifecycleOrderTests.cs` | Not found in FastFsm.Tests | Need to port both versions |

## Key Findings

1. **Naming Inconsistency**: 
   - `debug_history_test.cs` → should be `DebugHistoryTest.cs`
   - Files in old.tests don't follow `.Legacy.cs` convention

2. **Missing Test Categories**:
   - `Integration` tests not present in FastFsm.Tests
   - `Lifecycle` tests not present in FastFsm.Tests
   - Some `Core` tests missing (CoreMinimalTests, GuardPermittedTriggersTests)

3. **HSM Tests Observations**:
   - `debug_history_test.cs` references `ShallowHistoryTests.ShallowHistoryMachine`
   - This suggests it's testing the same machine that exists in `ShallowHistoryTests.Fluent.cs`
   - Need to create Legacy versions for all HSM Runtime tests

## Recommended Migration Steps

### Phase 1: Rename files in old.tests to follow convention
```bash
# In old.tests folder, rename to .Legacy.cs pattern
mv debug_history_test.cs DebugHistoryTest.Legacy.cs
# etc...
```

### Phase 2: Port missing Legacy tests
1. Copy missing test files from old.tests to FastFsm.Tests
2. Update namespaces and class names to follow Legacy convention
3. Ensure they compile and pass

### Phase 3: Create Fluent versions for Legacy-only tests
Priority order:
1. StateCallbackTests.Fluent.cs
2. EmptyMachineTests.Fluent.cs  
3. NameCollisionTests.Fluent.cs
4. BenchmarkTests.Fluent.cs
5. CoreMinimalTests (both versions)
6. GuardPermittedTriggersTests (both versions)
7. Lifecycle tests (both versions)
8. Integration tests (both versions)

### Phase 4: Create Legacy versions for Fluent-only HSM tests
All HSM Runtime tests need Legacy versions:
1. DebugHsmTest.Legacy.cs
2. DeepHistoryTests.Legacy.cs
3. InitialChildTests.Legacy.cs
4. InternalTransitionTests.Legacy.cs
5. ShallowHistoryTests.Legacy.cs
6. SimpleParentChildMachine.Legacy.cs
7. InheritanceTests.Legacy.cs

## Summary Statistics

- **Total files in old.tests**: 55
- **Already migrated**: ~38
- **Need migration**: ~17
- **Missing Fluent versions**: 6
- **Missing Legacy versions**: 8 (HSM Runtime)
- **Missing both versions**: 3-5 (Core, Lifecycle, Integration)

## Next Steps

1. Start with Phase 1: Rename files in old.tests
2. Identify exact mapping between old.tests and FastFsm.Tests
3. Port missing tests maintaining the same functionality
4. Ensure all tests run both Fluent and Legacy versions