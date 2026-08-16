# Generator.Rules Diagnostics

Total: 38 diagnostics

## FSM0100 — Potentially missing StateMachine attribute

- Id: FSM0100
- Title: Potentially missing StateMachine attribute
- Category: FSM.Generator
- Severity: Warning
- EnabledByDefault: true
- Message: Class '{0}' uses FSM transition attributes but is missing the [StateMachine(typeof(StateEnum), typeof(TriggerEnum))] attribute, or is not partial.
- Description: If this class is intended to be a FSM, it needs the [StateMachine] attribute and must be declared as partial.

## FSM0101 — State/Trigger types must be enums

- Id: FSM0101
- Title: State/Trigger types must be enums
- Category: FSM.Generator
- Severity: Error
- EnabledByDefault: true
- Message: State and Trigger types must be enums. '{0}' or '{1}' is not an enum.
- Description: The StateType and TriggerType arguments of the StateMachineAttribute must be enum types.

## FSM0200 — Invalid enum value in transition

- Id: FSM0200
- Title: Invalid enum value in transition
- Category: FSM.Generator
- Severity: Error
- EnabledByDefault: true
- Message: Invalid enum value '{0}' for enum type '{1}'. Use a valid enum member.
- Description: Enum values in transition attributes must be valid members of the specified enum type.

## FSM0300 — Invalid method signature for FSM callback

- Id: FSM0300
- Title: Invalid method signature for FSM callback
- Category: FSM.Generator
- Severity: Error
- EnabledByDefault: true
- Message: Method '{0}' used as {1} has an invalid signature. Expected: '{2}'.
- Description: Guard, Action, OnEntry, or OnExit methods must have a specific signature (e.g., guards return bool, actions are void; both can optionally take object? payload).

## FSM0301 — Guard with payload in non-payload machine

- Id: FSM0301
- Title: Guard with payload in non-payload machine
- Category: FSM.Generator
- Severity: Error
- EnabledByDefault: true
- Message: {0}
- Description: Guards that expect payload parameters cannot be used in state machines without payload support.

## FSM0302 — Callback returns 'async void'

- Id: FSM0302
- Title: Callback returns 'async void'
- Category: FSM.Generator
- Severity: Error
- EnabledByDefault: true
- Message: Callback method '{0}' returns 'async void'. Use 'Task' or 'ValueTask' instead to allow the state machine to correctly await its completion and handle exceptions.
- Description: 'async void' methods are fire-and-forget and can lead to unhandled exceptions and race conditions. State machine callbacks should always be awaitable.

## FSM0400 — Duplicate transition detected

- Id: FSM0400
- Title: Duplicate transition detected
- Category: FSM.Generator
- Severity: Warning
- EnabledByDefault: true
- Message: Duplicate transition from state '{0}' on trigger '{1}'. Only the first one will be used by the generator.
- Description: There are multiple transitions defined for the same 'from state' and 'trigger'. The generator will only consider the first one encountered.

## FSM0500 — Unreachable state detected

- Id: FSM0500
- Title: Unreachable state detected
- Category: FSM.Generator
- Severity: Info
- EnabledByDefault: true
- Message: State '{0}' might be unreachable based on defined transitions.
- Description: A state exists in the state enum that may not be reachable from the initial state or any other state via the defined transitions. This is a simplified check.

## FSM1100 — Mixed synchronous and asynchronous callbacks

- Id: FSM1100
- Title: Mixed synchronous and asynchronous callbacks
- Category: FSM.Generator.Async
- Severity: Warning
- EnabledByDefault: true
- Message: Cannot mix synchronous and asynchronous callbacks in the same state machine. Method '{0}' is {1}, but the machine is already configured as {2}.
- Description: All state machine callbacks (OnEntry, OnExit, Action, Guard) must be either all synchronous or all asynchronous to ensure consistent behavior.

## FSM1110 — Invalid async guard return type

- Id: FSM1110
- Title: Invalid async guard return type
- Category: FSM.Generator.Async
- Severity: Error
- EnabledByDefault: true
- Message: Asynchronous guards must return 'ValueTask<bool>', not 'Task<bool>'. Method '{0}' has an invalid return type.
- Description: Using Task<bool> for guards causes unnecessary memory allocations. Use ValueTask<bool> for optimal performance.

## FSM1120 — Asynchronous callback in synchronous state machine

- Id: FSM1120
- Title: Asynchronous callback in synchronous state machine
- Category: FSM.Generator.Async
- Severity: Error
- EnabledByDefault: true
- Message: Method '{0}' is asynchronous, but the state machine is synchronous. Either make all callbacks asynchronous or change the return type of this method.
- Description: A state machine must be consistently synchronous or asynchronous. Mixing callback types can lead to unexpected behavior and deadlocks.

## FSM2000 — Circular hierarchy detected

- Id: FSM2000
- Title: Circular hierarchy detected
- Category: FSM.Generator.HSM
- Severity: Error
- EnabledByDefault: true
- Message: State '{0}' is part of a circular hierarchy chain: {1}. Fix: Review the Parent relationships and remove the circular dependency.
- Description: State hierarchies cannot contain circular dependencies. A state cannot be its own ancestor or descendant.

## FSM2010 — Multiple or divergent parent

- Id: FSM2010
- Title: Multiple or divergent parent
- Category: FSM.Generator.HSM
- Severity: Error
- EnabledByDefault: true
- Message: State '{0}' references parent '{1}' which does not exist. Fix: Either define the parent state with [State({1})], or correct the Parent parameter to reference an existing state.
- Description: All parent states referenced by substates must be defined in the. Check for typos in the parent state name.

## FSM2020 — Composite without initial state

- Id: FSM2020
- Title: Composite without initial state
- Category: FSM.Generator.HSM
- Severity: Warning
- EnabledByDefault: true
- Message: Composite state '{0}' has no initial substate defined. Fix: Add [InitialSubstate({0}, YourInitialChild)] attribute, or set History = HistoryMode.Shallow/Deep on the composite state.
- Description: Composite states must have an initial substate to determine which child state to enter. Either define an initial substate or use history mode to remember the last active child.

## FSM2030 — Multiple initial children

- Id: FSM2030
- Title: Multiple initial children
- Category: FSM.Generator.HSM
- Severity: Error
- EnabledByDefault: true
- Message: Composite state '{0}' has multiple initial substates: '{1}' and '{2}'. Fix: Keep only one [InitialSubstate({0}, ...)] attribute.
- Description: A composite state can only have one initial substate. Remove duplicate InitialSubstate attributes.

## FSM2040 — History on non-composite

- Id: FSM2040
- Title: History on non-composite
- Category: FSM.Generator.HSM
- Severity: Error
- EnabledByDefault: true
- Message: State '{0}' has History = {1} but is not a composite state (has no children). Fix: Either remove the History parameter, or add child states with Parent = {0}.
- Description: Only composite states (states with children) can have history mode. History remembers which child was last active.

## FSM3000 — Open transition not finalized

- Id: FSM3000
- Title: Open transition not finalized
- Category: FSM.Generator.Fluent
- Severity: Error
- EnabledByDefault: true
- Message: Transition from state '{0}' on trigger '{1}' is not finalized with GoTo() or Internal(). Add .GoTo(targetState) or .Internal() to complete the transition.
- Description: Every transition must be finalized with either GoTo(targetState) for external transitions or Internal() for internal transitions.

## FSM3010 — Transition auto-finalized as internal

- Id: FSM3010
- Title: Transition auto-finalized as internal
- Category: FSM.Generator.Fluent
- Severity: Info
- EnabledByDefault: true
- Message: Transition from state '{0}' on trigger '{1}' was auto-finalized as internal. Add explicit .GoTo() or .Internal() to suppress this warning.
- Description: When a new On() or State() is encountered without finalizing the previous transition, it is auto-finalized as internal. This may not be the intended behavior.

## FSM3020 — Multiple payload definitions on transition

- Id: FSM3020
- Title: Multiple payload definitions on transition
- Category: FSM.Generator.Fluent
- Severity: Error
- EnabledByDefault: true
- Message: Transition from state '{0}' on trigger '{1}' has multiple Payload() calls. The last one ('{2}') will be used.
- Description: Each transition should have at most one payload type. Multiple Payload() calls will use the last specified type.

## FSM3030 — Invalid priority argument

- Id: FSM3030
- Title: Invalid priority argument
- Category: FSM.Generator.Fluent
- Severity: Error
- EnabledByDefault: true
- Message: Priority() requires an integer literal value.
- Description: The Priority() fluent call accepts only an integer literal argument used for transition ordering.

## FSM3040 — Priority() without active transition

- Id: FSM3040
- Title: Priority() without active transition
- Category: FSM.Generator.Fluent
- Severity: Error
- EnabledByDefault: true
- Message: Priority() can only be called while configuring a transition.
- Description: Priority() is valid only in the context of an active transition builder (after On()/OnInternal()).

## FSM3050 — Multiple global OnException handlers

- Id: FSM3050
- Title: Multiple global OnException handlers
- Category: FSM.Generator.Fluent
- Severity: Error
- EnabledByDefault: true
- Message: Multiple global OnException handlers specified; only one is allowed.
- Description: FastFSM supports exactly one global exception handler per state machine. Remove duplicate OnException() calls.

## FSM3060 — Invalid OnException handler signature

- Id: FSM3060
- Title: Invalid OnException handler signature
- Category: FSM.Generator.Fluent
- Severity: Error
- EnabledByDefault: true
- Message: Method '{0}' used as {1} has an invalid signature. Expected: {2}.
- Description: OnException handler must return ExceptionDirective or ValueTask<ExceptionDirective> and accept ExceptionContext<TState,TTrigger> as first parameter with optional CancellationToken.

## FSM9000 — Processing candidate

- Id: FSM9000
- Title: Processing candidate
- Category: FSM.Generator.Discovery
- Severity: Info
- EnabledByDefault: false
- Message: Processing candidate: {0}
- Description: Processing candidate trace.

## FSM9000 — Processing candidate

- Id: FSM9000
- Title: Processing candidate
- Category: FSM.Generator.Discovery
- Severity: Info
- EnabledByDefault: false
- Message: Processing candidate: {0}
- Description: Indicates that a discovered candidate is being processed by the generator.

## FSM9001 — Declaration plan

- Id: FSM9001
- Title: Declaration plan
- Category: FSM.Generator
- Severity: Info
- EnabledByDefault: false
- Message: DECLARATION_PLAN for {0}: ns='{1}', nesting='{2}', class='{3}', accessibility='{4}', partial={5}
- Description: Planned namespace, nesting and class accessibility for the generated state machine.

## FSM9002 — Empty code generated

- Id: FSM9002
- Title: Empty code generated
- Category: FSM.Generator
- Severity: Info
- EnabledByDefault: false
- Message: EMPTY_CODE for {0}; variant={1}; states={2}; transitions int={3}, ext={4}; payloads={5}; enumFallback={6}
- Description: Indicates that the generator produced empty or too small output for a candidate; includes basic metrics for diagnosis.

## FSM9003 — Enum-only states fallback

- Id: FSM9003
- Title: Enum-only states fallback
- Category: FSM.Generator
- Severity: Info
- EnabledByDefault: false
- Message: Enum-only states fallback applied for '{0}' — 0 [State] attributes found; using all enum members as states
- Description: Fallback path when no [State] attributes are found: all enum members are used as states.

## FSM9004 — MSBuild analyzer properties

- Id: FSM9004
- Title: MSBuild analyzer properties
- Category: FSM.Generator.Config
- Severity: Info
- EnabledByDefault: false
- Message: EmitCompilerGeneratedFiles={0}; CompilerGeneratedFilesOutputPath={1}
- Description: Displays analyzer-related MSBuild properties to aid debugging of generated files.

## FSM9005 — AddSource succeeded

- Id: FSM9005
- Title: AddSource succeeded
- Category: FSM.Generator.AddSource
- Severity: Info
- EnabledByDefault: false
- Message: AddSource ok: {0} (len={1})
- Description: Indicates a successful AddSource call with the hint name and content length.

## FSM9006 — State machine candidate skipped

- Id: FSM9006
- Title: State machine candidate skipped
- Category: FSM.Generator.Discovery
- Severity: Info
- EnabledByDefault: false
- Message: Skipped state machine candidate {0}: {1}
- Description: Provides the reason why a discovered state machine candidate was skipped.

## FSM9007 — Generator trace

- Id: FSM9007
- Title: Generator trace
- Category: FSM.Generator.Discovery
- Severity: Info
- EnabledByDefault: false
- Message: {0}
- Description: Generic trace or discovery diagnostic used for parser traces or additional info.

## FSM9008 — Starting parse

- Id: FSM9008
- Title: Starting parse
- Category: FSM.Generator.Parser
- Severity: Info
- EnabledByDefault: false
- Message: Starting parse for: {0}
- Description: Marks the start of parsing a specific candidate class.

## FSM9009 — Variant decision

- Id: FSM9009
- Title: Variant decision
- Category: FSM.Generator
- Severity: Info
- EnabledByDefault: false
- Message: {0} -> {1}; internalOnly={2}; payloadPresent={3}
- Description: Generator variant/features summary (payload, extensions, callbacks, internal-only). The second argument typically encodes feature flags.

## FSM9010 — Configuration sections

- Id: FSM9010
- Title: Configuration sections
- Category: FSM.Generator.Parser
- Severity: Info
- EnabledByDefault: false
- Message: {0} - StatesFrom: {1} | TransitionsFrom: {2} (ext={3}) | InternalFrom: {4} (int={5}) | PayloadTypes: {6}
- Description: Summary of configuration sources discovered during parsing (methods contributing [State], [Transition], internal transitions and payload types).

## FSM9011 — HSM Flag Tracking

- Id: FSM9011
- Title: HSM Flag Tracking
- Category: FSM.Generator
- Severity: Info
- EnabledByDefault: false
- Message: {0}
- Description: Diagnostic used to surface HSM-related feature flags and checkpoints during generation and in generated code comments.

## FSM9012 — Logging helper pre-AddSource

- Id: FSM9012
- Title: Logging helper pre-AddSource
- Category: FSM.Generator.AddSource
- Severity: Info
- EnabledByDefault: false
- Message: GenerateLogging={0}; ns='{1}'; class='{2}'; hint='{3}'; first='{4}'
- Description: Summary emitted before adding the optional logging helper source file.

## FSM9013 — MSBuild logging flags

- Id: FSM9013
- Title: MSBuild logging flags
- Category: FSM.Generator.Config
- Severity: Info
- EnabledByDefault: false
- Message: FsmGenerateLogging={0}; FsmGenerateDI={1}
- Description: Shows effective MSBuild flags controlling optional logging and DI generation.
