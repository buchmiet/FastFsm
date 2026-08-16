# Diagnostics Examples (from tests)

The examples below come from tests in `Generator.Tests/Diagnostics/*` and show minimal code snippets that trigger each diagnostic. Each example includes a simple state machine and the configuration that leads to the diagnostic being emitted.

## FSM0100 — Potentially missing StateMachine attribute

If this class is intended to be a FSM, it needs the [StateMachine] attribute and must be declared as partial.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    // Missing [StateMachine(typeof(State), typeof(Trigger))]
    public class Machine {
        [Transition(State.A, Trigger.Go, State.B)]
        private void Config() { }
}
}
```

How to fix: Add the [StateMachine(typeof(State), typeof(Trigger))] attribute to the class and make the class partial.

## FSM0101 — State/Trigger types must be enums

The StateType and TriggerType arguments of the StateMachineAttribute must be enum types.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum Trigger { Go }
    [StateMachine(typeof(int), typeof(Trigger))]
    public partial class Machine {
        [Transition(0, Trigger.Go, 0)]
        private void Config() { }
}
}
```

How to fix: Ensure both State and Trigger arguments of [StateMachine] refer to enum types.

## FSM0200 — Invalid enum value in transition

Enum values in transition attributes must be valid members of the specified enum type.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State : byte { Low = 0, High = 255 }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition((State)0, Trigger.Go, (State)255)] // valid
        [Transition((State)128, Trigger.Go, State.Low)] // invalid value 128 not defined
        private void Config() { }
}
}
```

How to fix: Use only defined enum members when specifying states/triggers in attributes.

## FSM0300 — Invalid method signature for FSM callback

Guard, Action, OnEntry, or OnExit methods must have a specific signature (e.g., guards return bool, actions are void; both can optionally take object? payload).

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        private int BadGuard() => 1; // invalid: guards must return bool/ValueTask<bool>

        [Transition(State.A, Trigger.Go, State.B, Guard = nameof(BadGuard))]
        private void Config() { }
    }
}
```

How to fix: Adjust the method signature to the expected form (e.g., guards return bool/ValueTask<bool>, actions are void/ValueTask).

## FSM0301 — Guard with payload in non-payload machine

Guards that expect payload parameters cannot be used in state machines without payload support.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        private bool Guard(object payload) => true; // expects payload, but machine has no payload config

        [Transition(State.A, Trigger.Go, State.B, Guard = nameof(Guard))]
        private void Config() { }
    }
}
```

How to fix: Remove the payload parameter from the guard, or enable payload support for the machine (e.g., via [PayloadType] or default payload configuration).

## FSM0302 — Callback returns 'async void'

'async void' methods are fire-and-forget and can lead to unhandled exceptions and race conditions. State machine callbacks should always be awaitable.

Example:

```csharp
using System.Threading.Tasks;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        private async void BadAction() { await Task.Delay(1); }

        [Transition(State.A, Trigger.Go, State.B, Action = nameof(BadAction))]
        private void Config() { }
    }
}
```

How to fix: Replace async void with Task or ValueTask so the callback is awaitable.

## FSM0400 — Duplicate transition detected

There are multiple transitions defined for the same 'from state' and 'trigger'. The generator will only consider the first one encountered.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition(State.A, Trigger.X, State.B)]
        [Transition(State.A, Trigger.X, State.B)] // duplicate
        private void Config() { }
    }
}
```

How to fix: Remove or merge duplicate transitions so only one transition exists for a given (from, trigger) pair.

## FSM0500 — Unreachable state detected

A state exists in the state enum that may not be reachable from the initial state or any other state via the defined transitions. This is a simplified check.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { Start, Mid, Unreach }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        // No [State] attributes on purpose -> fallback uses all enum members
        [Transition(State.Start, Trigger.Go, State.Mid)]
        private void Config() { }
    }
}
```

How to fix: Add transitions that reach the state from the initial graph, or remove the unused state.

## FSM1100 — Mixed synchronous and asynchronous callbacks

All state machine callbacks (OnEntry, OnExit, Action, Guard) must be either all synchronous or all asynchronous to ensure consistent behavior.

Example:

```csharp
using Abstractions.Attributes;
using System.Threading.Tasks;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [State(State.A, OnEntry = nameof(SyncEntry))]
        [State(State.B, OnEntry = nameof(AsyncEntry))]
        private void Config() { }

        private void SyncEntry() { }
        private async Task AsyncEntry() { await Task.Delay(1); }
    }
}
```

How to fix: Make all callbacks either synchronous or asynchronous consistently (convert sync to async or vice versa).

## FSM1110 — Invalid async guard return type

Using Task<bool> for guards causes unnecessary memory allocations. Use ValueTask<bool> for optimal performance.

Przykład:

```csharp
using Abstractions.Attributes;
using System.Threading.Tasks;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition(State.A, Trigger.X, State.B, Guard = nameof(AsyncGuard))]
        private void Config() { }

        private async Task<bool> AsyncGuard() { await Task.Delay(1); return true; }
    }
}
```

How to fix: Change guard return type from Task<bool> to ValueTask<bool>.

## FSM1120 — Asynchronous callback in synchronous state machine

A state machine must be consistently synchronous or asynchronous. Mixing callback types can lead to unexpected behavior and deadlocks.

Note (current behavior): in this configuration the generator first emits FSM1100 (mixed sync/async), so FSM1120 is currently not emitted (see: Generator.Tests/Diagnostics/Async/FSM1120_AsyncCallbackInSyncMachineTests.cs).

Example:

```csharp
using Abstractions.Attributes;
using System.Threading.Tasks;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [State(State.A, OnEntry = nameof(SyncEntry))]
        [State(State.B, OnEntry = nameof(AsyncEntry))]
        private void Config() { }

        private void SyncEntry() { }
        private async Task AsyncEntry() { await Task.Delay(1); }
    }
}
```

How to fix: Avoid async callbacks in a synchronous machine—either convert all callbacks to async or make this method synchronous. Note: currently FSM1100 may be emitted first, suppressing FSM1120.

## FSM2000 — Circular hierarchy detected

State hierarchies cannot contain circular dependencies. A state cannot be its own ancestor or descendant.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B, C }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.A, Parent = State.B)]
        [State(State.B, Parent = State.C)]
        [State(State.C, Parent = State.A)] // circular
        private void Config() { }
    }
}
```

How to fix: Remove circular parent-child relationships so no state is its own ancestor/descendant.

## FSM2010 — Multiple or divergent parent

All parent states referenced by substates must be defined in the. Check for typos in the parent state name.

Note (current behavior): with the current parser, all enum values are treated as states (fallback), so parent “C” exists in the model and FSM2010 is not emitted. FSM2020 (composite without initial substate) appears instead — see Generator.Tests/Diagnostics/HSM/FSM2010_OrphanSubstateTests.cs.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B, C }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.A)]
        // 'C' exists in enum but is not defined via [State] => orphan parent
        [State(State.B, Parent = State.C)]
        private void Config() { }
    }
}
```

How to fix: Define the parent state via [State] (if missing) or correct the Parent reference to an existing state. Note: current parser behavior may emit FSM2020 instead.

## FSM2020 — Composite without initial state

Composite states must have an initial substate to determine which child state to enter. Either define an initial substate or use history mode to remember the last active child.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { Parent, Child1, Child2 }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.Parent)]
        [State(State.Child1, Parent = State.Parent)]
        [State(State.Child2, Parent = State.Parent)]
        private void Config() { }
    }
}
```

How to fix: Mark exactly one child as initial (e.g., IsInitial = true) or enable history to restore the last active child.

## FSM2030 — Multiple initial children

A composite state can only have one initial substate. Remove duplicate InitialSubstate attributes.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { Parent, C1, C2 }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.Parent)]
        [State(State.C1, Parent = State.Parent, IsInitial = true)]
        [State(State.C2, Parent = State.Parent, IsInitial = true)] // multiple initial
        private void Config() { }
    }
}
```

How to fix: Keep only one initial substate for the composite state; remove extra initial markers.

## FSM2040 — History on non-composite

Only composite states (states with children) can have history mode. History remembers which child was last active.

Example:

```csharp
using Abstractions.Attributes;
namespace Test {
    public enum State { Lone }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.Lone, History = HistoryMode.Shallow)] // no children -> non-composite
        private void Config() { }
    }
}
```

How to fix: Either remove the History setting or make the state composite by adding child states.

## FSM3000 — Open transition not finalized

Every transition must be finalized with either GoTo(targetState) for external transitions or Internal() for internal transitions.

Example:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X, Y }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A).On(Trigger.X);
    }
}
```

How to fix: Finalize every transition with .GoTo(target) for external transitions or .Internal() for internal transitions.

## FSM3010 — Transition auto-finalized as internal

When a new On() or State() is encountered without finalizing the previous transition, it is auto-finalized as internal. This may not be the intended behavior.

Example:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X, Y }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A).On(Trigger.X).On(Trigger.Y).GoTo(State.A);
    }
}
```

How to fix: Explicitly finalize the previous transition with .GoTo() or .Internal() before starting a new .On()/.State().

## FSM3020 — Multiple payload definitions on transition

Each transition should have at most one payload type. Multiple Payload() calls will use the last specified type.

Example:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .On(Trigger.X)
            .Payload(typeof(string))
            .Payload(typeof(int))
            .Internal();
    }
}
```

How to fix: Specify at most one .Payload(...) per transition; remove extras.

## FSM3030 — Invalid priority argument

The Priority() fluent call accepts only an integer literal argument used for transition ordering.

Example:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .On(Trigger.X)
            .Priority(1 + 2) // not a literal
            .GoTo(State.B);
    }
}
```

How to fix: Pass a numeric literal to .Priority(...) (e.g., .Priority(100)).

## FSM3040 — Priority() without active transition

Priority() is valid only in the context of an active transition builder (after On()/OnInternal()).

Przykład:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .Priority(1) // no active transition here
            .On(Trigger.X).GoTo(State.B);
    }
}
```

How to fix: Call .Priority(...) only while configuring an active transition (after .On(...) /.OnInternal(...)).

## FSM3050 — Multiple global OnException handlers

FastFSM supports exactly one global exception handler per state machine. Remove duplicate OnException() calls.

Przykład:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test.FluentDup {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .OnException(nameof(Handle1))
            .OnException(nameof(Handle2))
            .On(Trigger.X).GoTo(State.B);

        // Handlers intentionally use simple signatures; duplicate should still be emitted
        private static FastFsm.Exceptions.ExceptionDirective Handle1(
            FastFsm.Exceptions.ExceptionContext<State, Trigger> ctx) => FastFsm.Exceptions.ExceptionDirective.Propagate;
        private static FastFsm.Exceptions.ExceptionDirective Handle2(
            FastFsm.Exceptions.ExceptionContext<State, Trigger> ctx) => FastFsm.Exceptions.ExceptionDirective.Propagate;
    }
}
```

How to fix: Keep a single .OnException(...) per state machine; remove duplicates.

## FSM3060 — Invalid OnException handler signature

OnException handler must return ExceptionDirective or ValueTask<ExceptionDirective> and accept ExceptionContext<TState,TTrigger> as first parameter with optional CancellationToken.

Przykład:

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .OnException(nameof(BadHandler))
            .On(Trigger.X).GoTo(State.B);

        // Invalid: wrong parameter type (no ExceptionContext)
        private static FastFsm.Exceptions.ExceptionDirective BadHandler(int notContext) =>
            FastFsm.Exceptions.ExceptionDirective.Propagate;
    }
}
```

How to fix: Use ExceptionDirective or ValueTask<ExceptionDirective> return type, first parameter ExceptionContext<TState,TTrigger>, and optional CancellationToken.
