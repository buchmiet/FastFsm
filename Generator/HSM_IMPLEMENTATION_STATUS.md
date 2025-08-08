# HSM (Hierarchical State Machine) Implementation Status

## Completed Milestones

### ✅ Milestone 0: Prepare repo for HSM - ensure no regression
- No breaking changes to existing API
- HSM features are opt-in via `EnableHierarchy` flag

### ✅ Milestone 1: Extend model types for hierarchy
- Extended `StateModel` with HSM properties:
  - `ParentState`: Reference to parent state
  - `ChildStates`: List of child states
  - `History`: History mode (None/Shallow/Deep)
  - `IsInitial`: Marks initial substate
  - `IsComposite`: Computed property for composite states
- Extended `StateMachineModel` with hierarchy maps:
  - `ParentOf`: State → Parent mapping
  - `ChildrenOf`: Composite → Children mapping  
  - `Depth`: State depth in hierarchy
  - `InitialChildOf`: Composite → Initial child mapping
  - `HistoryOf`: Composite → History mode mapping
  - `HierarchyEnabled`: Feature flag
- Created `HistoryMode` enum in Generator.Model

### ✅ Milestone 2: Parser support for HSM attributes
- Extended `StateAttribute` with HSM properties:
  - `Parent`: Optional parent state
  - `History`: History mode for composites
  - `IsInitial`: Initial substate marker
- Extended `StateMachineAttribute`:
  - `EnableHierarchy`: Enable HSM features
- Parser enhancements:
  - Parse HSM attributes from state definitions
  - Build hierarchy relationships in `BuildHierarchy` method
  - Auto-enable hierarchy when HSM attributes detected
  - Validate hierarchy structure
- Added diagnostics:
  - FSM100: Invalid hierarchy configuration
  - FSM101: Orphan substate
  - FSM102: Circular hierarchy
  - FSM103: Multiple initial substates
  - FSM104: Invalid history configuration
  - FSM105: Conflicting transition targets

## Pending Milestones

### ⏳ Milestone 3: Runtime contracts and base classes
- Add `IsIn(state)` and `GetActivePath()` to IStateMachine
- Extend runtime base classes with hierarchy support
- Implement history state tracking

### ⏳ Milestone 4: Code generators for HSM
- Hierarchical trigger resolution (walk parent chain)
- LCA computation for exit/entry sequences
- History state restoration
- Generate hierarchy lookup tables

### ⏳ Milestone 5: DI and Logging integration
- No API changes needed
- Add hierarchy info to logs (optional)

### ⏳ Milestone 6: Test suite for HSM
- Composite state entry/exit
- History (shallow/deep)
- Transition inheritance
- Internal transitions on parents
- Exception handling in cascaded callbacks

### ⏳ Milestone 7: Diagnostics and developer UX
- User-friendly error messages
- Documentation updates

### ⏳ Milestone 8: NuGet/CI pipeline integration
- Package and test with local NuGet feed

## Technical Notes

### Design Decisions
1. **HistoryMode in Generator.Model**: Duplicated enum to avoid Abstractions dependency in Model project
2. **Parser Integration**: BuildHierarchy called after ParseMemberAttributes
3. **Auto-detection**: Hierarchy enabled automatically when HSM attributes used
4. **Diagnostics**: Using existing DiagnosticFactory infrastructure

### Key Files Modified
- `/Abstractions/Attributes/StateAttribute.cs`: Added HSM properties
- `/Abstractions/Attributes/StateMachineAttribute.cs`: Added EnableHierarchy
- `/Abstractions/Attributes/HistoryMode.cs`: Created enum
- `/Generator.Model/StateModel.cs`: Added hierarchy properties
- `/Generator.Model/StateMachineModel.cs`: Added hierarchy maps
- `/Generator.Model/HistoryMode.cs`: Created duplicate enum
- `/Generator.Rules/Definitions/RuleIdentifiers.cs`: Added FSM100-105
- `/Generator.Rules/Definitions/RuleDefinition.cs`: Added HSM rule definitions
- `/Generator/Parsers/StateMachineParser.cs`: Added BuildHierarchy method

### Next Steps
1. Implement runtime contracts (Milestone 3)
2. Modify code generators to support hierarchical resolution (Milestone 4)
3. Add comprehensive test coverage (Milestone 6)

## Build Status
✅ All projects build successfully with HSM changes
⚠️ Multiple nullable reference warnings (pre-existing)