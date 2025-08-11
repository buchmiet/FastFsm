# Hierarchical State Machine (HSM) Documentation

## Overview

FastFsm now supports **Hierarchical State Machines** (HSM), allowing you to organize states into parent-child relationships. This feature enables more maintainable and scalable state machine designs through state composition, transition inheritance, and history modes.

## Enabling HSM

Hierarchical features are **disabled by default** for backward compatibility. Enable them in one of two ways:

1. **Globally via attribute:**
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class MyHsmMachine { }
```

2. **Automatically when using HSM attributes:**
Using any HSM-specific attribute parameters (`Parent`, `History`) automatically enables hierarchy support.

## Core Concepts

### 1. State Hierarchy

Define parent-child relationships between states:

```csharp
[State(States.Menu, Parent = States.Root)]
void Menu() { }

[State(States.Settings, Parent = States.Menu)]
void Settings() { }
```

### 2. Composite States

A **composite state** is any state that has children. When entering a composite state, the machine must determine which child to activate.

### 3. Initial Substates

Every composite state needs an initial substate (unless using history):

```csharp
[State(States.Menu, Parent = States.Root)]
void Menu() { }

[State(States.MainMenu, Parent = States.Menu, IsInitial = true)]
void MainMenu() { }
```

### 4. History Modes

History allows composite states to remember their last active child:

- **Shallow History**: Remembers only the direct child
- **Deep History**: Remembers the entire substate path

```csharp
[State(States.Menu, Parent = States.Root, History = HistoryMode.Shallow)]
void Menu() { }
```

## Transition Inheritance

Child states inherit transitions from their parents:

```csharp
[Transition(States.Menu, Triggers.Exit, States.Quit)]  // All menu substates can exit
void MenuExit() { }

[Transition(States.Settings, Triggers.Exit, States.MainMenu)]  // Override for Settings
void SettingsBack() { }
```

**Resolution rules:**
- Transitions are resolved from most specific (current state) to least specific (root)
- First matching transition wins
- Child transitions override parent transitions

## Internal vs External Transitions

- **External Transition** (default): Exits and re-enters states, triggering callbacks
- **Internal Transition**: Executes action without changing state or triggering exit/entry

```csharp
[InternalTransition(States.Menu, Triggers.Refresh)]
void RefreshMenu() { 
    // No exit/entry callbacks, state remains unchanged
}
```

## Exit/Entry Sequences

When transitioning between states in different branches:

1. **Exit sequence**: From current leaf up to (but not including) the Lowest Common Ancestor (LCA)
2. **State change**: Update current state
3. **Entry sequence**: From LCA down to target state

Example: Transitioning from `Settings` to `GamePlay`:
```
Settings (child of Menu) → GamePlay (child of InGame)

Exit: Settings → Menu
Entry: InGame → GamePlay
```

## Public API

### Structural Query Methods

```csharp
// Check if a parent state is in the active hierarchy
bool IsInHierarchy(TState parentState);

// Per-state helper methods for hierarchy checking (bitmask-based)
// Generated for each composite state:
bool IsInWork();  // Returns true if current state is Work or any of its children

// DEBUG-only helper for diagnostics
#if DEBUG
string DumpActivePath();  // Returns e.g. "Working / Working_Initializing"
#endif
```

### Example Usage

```csharp
var machine = new GameStateMachine(States.MainMenu);

// Check active states using generated helpers
if (machine.IsInMenu()) {
    // Currently in menu or any of its substates
}

// Debug-only path dump
#if DEBUG
var path = machine.DumpActivePath();
// Returns: "Root / Menu / MainMenu"
#endif
```

## Exception Handling in HSM

Exception behavior follows these rules:

1. **OnExit exception**: Interrupts transition, state remains unchanged
2. **OnEntry/Action exception**: Handled by `OnException` directive
   - `Continue`: Swallow exception, continue transition
   - `Propagate`: Re-throw exception
3. **OperationCanceledException**: Always propagates (no special handling)

## Diagnostics (FSM100-FSM106)

FastFsm provides comprehensive diagnostics for HSM configuration:

### FSM100: Circular Hierarchy
```csharp
// ERROR: A → B → C → A
[State(States.A, Parent = States.C)]
```
**Fix**: Review parent relationships and remove cycles

### FSM101: Orphan Substate
```csharp
// ERROR: Parent doesn't exist
[State(States.Child, Parent = States.NonExistent)]
```
**Fix**: Define the parent state or correct the reference

### FSM102: Composite Without Initial
```csharp
// ERROR: Has children but no initial
[State(States.Menu, Parent = States.Root)]
void Menu() { }

[State(States.Item1, Parent = States.Menu)]
void Item1() { }
// Missing: IsInitial = true on one child
```
**Fix**: Add `IsInitial = true` to exactly one child state

### FSM103: Multiple Initial Children
```csharp
// ERROR: Multiple initial substates
[State(States.Item1, Parent = States.Menu, IsInitial = true)]
[State(States.Item2, Parent = States.Menu, IsInitial = true)]  // Duplicate!
```
**Fix**: Keep only one initial substate

### FSM104: History on Non-Composite
```csharp
// WARNING: Leaf state with history
[State(States.Leaf, History = HistoryMode.Shallow)]
```
**Fix**: Remove history or add child states

### FSM105: Implicit Composite Entry
```csharp
// INFO: Transitioning to composite without specifying child
[Transition(States.Other, Triggers.ToMenu, States.Menu)]
```
**Note**: Machine will use initial substate or history (declaration order break-tie for FSM106 equal priorities)

## Best Practices

1. **Keep hierarchies shallow** (2-3 levels max) for maintainability
2. **Use meaningful state groupings** that reflect your domain
3. **Prefer explicit transitions** to specific substates over implicit composite entries
4. **Document transition inheritance** when overriding parent transitions
5. **Use history modes judiciously** - they add complexity
6. **Test exit/entry sequences** thoroughly, especially with exceptions

## Migration Guide

Existing state machines continue to work without changes. To adopt HSM:

1. Add `EnableHierarchy = true` to your `[StateMachine]` attribute
2. Define parent relationships with `Parent` parameter
3. Set initial substates for all composite states
4. Optionally add history modes where state memory is needed
5. Test thoroughly, especially transition inheritance

## Performance Considerations

- **Flat machines**: Fastest, minimal overhead
- **HSM without history**: Small overhead for path traversal
- **Shallow history**: Moderate overhead for state tracking
- **Deep history**: Higher overhead for full path restoration
- **Current fast path**: ≤64 states (bitmask), >64 uses array masks
- **Planned (0.8+)**: Support for >256 states
- **Zero allocation guarantee**: Maintained across all HSM features

Benchmark your specific use case if performance is critical.

## Examples

### Simple Menu System
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class MenuSystem
{
    [State(State.Root)]
    void Root() { }

    [State(State.Menu, Parent = State.Root, History = HistoryMode.Shallow)]
    void Menu() { }

    [State(State.MainMenu, Parent = State.Menu, IsInitial = true)]
    void MainMenu() { }

    [State(State.Settings, Parent = State.Menu)]
    void Settings() { }

    [State(State.Game, Parent = State.Root)]
    void Game() { }

    // Menu substates inherit this
    [Transition(State.Menu, Trigger.StartGame, State.Game)]
    void MenuToGame() { }

    // Settings overrides the exit behavior
    [Transition(State.Settings, Trigger.Back, State.MainMenu)]
    void SettingsBack() { }
}
```

### Game State with Deep History
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class GameStateMachine
{
    [State(State.Playing, Parent = State.Root, History = HistoryMode.Deep)]
    void Playing() { }

    [State(State.Level, Parent = State.Playing, IsInitial = true)]
    void Level() { }

    [State(State.Combat, Parent = State.Level, History = HistoryMode.Deep, IsInitial = true)]
    void Combat() { }

    [State(State.PlayerTurn, Parent = State.Combat, IsInitial = true)]
    void PlayerTurn() { }

    [State(State.Paused, Parent = State.Root)]
    void Paused() { }

    // Pause from anywhere in Playing hierarchy
    [Transition(State.Playing, Trigger.Pause, State.Paused)]
    void PauseGame() { }

    // Resume restores full state path due to Deep history
    [Transition(State.Paused, Trigger.Resume, State.Playing)]
    void ResumeGame() { }
}
```