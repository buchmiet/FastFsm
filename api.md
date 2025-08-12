# FastFSM API Documentation

## Overview

FastFSM is a high-performance, zero-overhead finite state machine framework for .NET that leverages C# source generators to create highly optimized state machines at compile time. This document provides comprehensive API documentation for all projects in the FastFSM solution.

### Key Features
- **Zero Runtime Reflection** - All transitions compile to simple switch statements
- **Zero Heap Allocations** - No garbage collection pressure in state logic
- **Compile-time Validation** - Invalid states and transitions caught during build
- **Hierarchical State Machines (HSM)** - Support for composite states, substates, and history
- **Native AOT Ready** - Fully compatible with trimming and ahead-of-time compilation
- **Extensible** - Support for logging, dependency injection, and custom extensions

## Solution Structure

```
FastFSM/
├── Core Libraries/
│   ├── Abstractions/                    # Core attributes and contracts
│   ├── StateMachine/                    # Runtime components and base classes
│   ├── StateMachine.DependencyInjection/# DI integration
│   └── StateMachine.Logging/            # Structured logging support
│
├── Code Generation/
│   ├── Generator/                       # Main source generator
│   ├── Generator.Model/                 # Data models for generation
│   ├── Generator.Rules/                 # Validation rules
│   ├── Generator.DependencyInjection/   # DI factory generator
│   └── IndentedStringBuilder/           # Code generation utilities
│
├── Testing/
│   ├── StateMachine.Tests/              # Core unit tests
│   ├── StateMachine.Async.Tests/        # Async functionality tests
│   └── StateMachine.DependencyInjection.Tests/ # DI integration tests
│
└── Benchmarks/
    ├── Benchmark/                       # .NET benchmarks
    ├── Benchmark.cpp/                   # C++ comparison
    ├── Benchmark.Java/                  # Java comparison
    ├── Benchmark.JavaScript/            # JavaScript comparison
    ├── Benchmark.Rust/                  # Rust comparison
    └── Benchmark.TypeScript/            # TypeScript comparison
```

## Project: `Abstractions`

**Purpose**: Defines the core attributes and contracts for declaring state machines. This is the foundational API that users interact with directly.

### Attributes

#### Core State Machine Attributes

##### `StateMachineAttribute`
Marks a partial class as a state machine that should be generated.

```csharp
[StateMachine(typeof(TState), typeof(TTrigger))]
public partial class MyStateMachine { }
```

**Properties:**
- `Type StateType` - Enum type defining possible states (required)
- `Type TriggerType` - Enum type defining possible triggers (required)
- `Type DefaultPayloadType` - Default payload type for all triggers
- `bool GenerateExtensibleVersion` - Enable extension support (default: true)
- `bool GenerateStructuralApi` - Generate structural analysis methods
- `bool ContinueOnCapturedContext` - Control async continuation context (default: false)
- `bool EnableHierarchy` - Enable HSM features (auto-enabled when HSM attributes used)

**Nested Classes Support:**

State machine classes may be declared as nested partial classes inside other types. The generator will emit matching nested partial declarations so that callback methods (OnEntry/OnExit, guards, actions) resolve correctly in their containing scopes.

##### `TransitionAttribute`
Defines a state transition with optional guard and action.

```csharp
[Transition(FromState, Trigger, ToState, Guard = "method", Action = "method")]
```

**Parameters:**
- `object fromState` - Source state
- `object trigger` - Trigger that causes transition
- `object toState` - Destination state
- `string Guard` - Optional guard method name (returns bool)
- `string Action` - Optional action method name

##### `InternalTransitionAttribute`
Defines an internal transition that executes an action without changing state.

```csharp
[InternalTransition(State, Trigger, Guard = "method", Action = "method")]
```

##### `StateAttribute`
Configures state-specific behavior and hierarchy.

```csharp
[State(MyState.Active, 
    OnEntry = "EnterActive", 
    OnExit = "ExitActive",
    Parent = MyState.Running,
    History = HistoryMode.Shallow,
    IsInitial = true)]
```

**Properties:**
- `object State` - The state to configure (required)
- `string OnEntry` - Method to execute when entering state
- `string OnExit` - Method to execute when leaving state
- `object Parent` - Parent state for HSM (optional)
- `HistoryMode History` - History behavior for composite states
- `bool IsInitial` - Marks as initial substate of parent

#### Hierarchical State Machine (HSM) Support

##### `HistoryMode`
Defines history behavior for composite states.

```csharp
public enum HistoryMode
{
    None = 0,    // No history - always enter through initial substate
    Shallow = 1, // Remember only the direct child state
    Deep = 2     // Remember the full nested state path
}
```

##### HSM Runtime Helpers

###### `bool IsInHierarchy(TState ancestor)`
Checks if the current state lies within the hierarchy of a given ancestor state.

**Returns:** `true` if `ancestor` is the current state or any of its parents; `false` otherwise.

**Availability:** All build configurations (Debug and Release) for HSM-enabled machines.

```csharp
// Example usage
if (machine.IsInHierarchy(WorkflowState.Processing))
{
    // Current state is within the Processing composite
}
```

###### `string DumpActivePath()`
Returns the active path from root to current leaf state as a string (DEBUG-only).

**Returns:** Path string like `"Root / Menu / Menu_Settings"`

**Availability:** DEBUG builds only for HSM-enabled machines.

#### Additional Attributes

##### `PayloadTypeAttribute`
Specifies payload type for specific triggers.

```csharp
[PayloadType(typeof(OrderData), Triggers = new[] { OrderTrigger.Submit })]
```

##### `OnExceptionAttribute`
Specifies exception handling method.

```csharp
[OnException(nameof(HandleError))]
private void HandleError(ExceptionContext<TState, TTrigger> context) { }
```

##### `GenerateLoggingAttribute`
Controls logging code generation.

```csharp
[GenerateLogging]
public partial class MyStateMachine { }
```

## Project: `StateMachine`

**Purpose**: Contains the core runtime components and base classes that generated state machines inherit from. This is the runtime library that applications depend on.

### Builder

#### `StateMachineBuilder<TState, TTrigger>`
A fluent builder for creating state machine instances at runtime (for dynamic scenarios).

**Methods:**
- `Build(TState initialState)` - Creates a configured state machine instance
- `WithTransition(from, trigger, to)` - Adds a transition
- `WithGuard(guard)` - Adds a guard condition
- `WithAction(action)` - Adds a transition action
- `WithOnEntry(state, callback)` - Sets entry callback
- `WithOnExit(state, callback)` - Sets exit callback

### Contracts

#### Core Interfaces

##### `IStateMachineSync<TState, TTrigger>`
Synchronous state machine interface.

**Properties:**
- `TState CurrentState` - Current state of the machine
- `bool IsStarted` - Whether machine has been started

**Methods:**
- `void Start()` - Initialize and start the machine
- `bool TryFire(TTrigger, object? payload)` - Attempt transition
- `void Fire(TTrigger, object? payload)` - Execute transition (throws if invalid)
- `bool CanFire(TTrigger)` - Check if trigger is valid
- `IReadOnlyList<TTrigger> GetPermittedTriggers()` - Get valid triggers

##### `IStateMachineAsync<TState, TTrigger>`
Asynchronous state machine interface.

**Properties:**
- `TState CurrentState` - Current state of the machine
- `bool IsStarted` - Whether machine has been started

**Methods:**
- `ValueTask StartAsync(CancellationToken)` - Initialize and start
- `ValueTask<bool> TryFireAsync(TTrigger, object?, CancellationToken)` - Attempt transition
- `ValueTask FireAsync(TTrigger, object?, CancellationToken)` - Execute transition
- `ValueTask<bool> CanFireAsync(TTrigger, CancellationToken)` - Check validity
- `ValueTask<IReadOnlyList<TTrigger>> GetPermittedTriggersAsync(CancellationToken)` - Get valid triggers

#### Extension Interfaces

##### `IStateMachineExtension`
Interface for implementing cross-cutting concerns.

**Methods:**
- `void OnTransitionStarting(IStateMachineContext context)` - Before transition
- `void OnTransitionCompleted(IStateMachineContext context)` - After transition
- `void OnTransitionError(IStateMachineContext context, Exception ex)` - On error

##### `IStateMachineContext`
Context passed to extensions.

**Properties:**
- `object FromState` - Source state
- `object ToState` - Target state
- `object Trigger` - Triggering event
- `object? Payload` - Optional payload
- `bool IsInternal` - Whether internal transition

##### `IStateMachineContext<TState, TTrigger>`
Strongly-typed context for extensions.

**Inherits:** `IStateMachineContext`
**Additional Properties:**
- `TState TypedFromState` - Typed source state
- `TState TypedToState` - Typed target state
- `TTrigger TypedTrigger` - Typed trigger

#### Factory Interfaces

##### `IStateMachineFactory<TStateMachine, TState, TTrigger>`
Factory for creating state machine instances.

**Methods:**
- `TStateMachine Create(TState initialState)` - Create unstarted instance
- `TStateMachine CreateStarted(TState initialState)` - Create and start
- `Task<TStateMachine> CreateStartedAsync(TState initialState)` - Async create and start

#### Snapshot Interface

##### `IStateSnapshot`
Provides transition information without generics.

**Properties:**
- `string FromStateName` - Source state name
- `string ToStateName` - Target state name
- `string TriggerName` - Trigger name
- `bool HasPayload` - Whether payload present
- `Type? PayloadType` - Payload type if present

### Dependency Injection

#### `FsmServiceCollectionExtensions`
Extension methods for Microsoft.Extensions.DependencyInjection.

**Methods:**
- `AddStateMachineFactory()` - Register factory services
- `AddStateMachine<T>()` - Register state machine type
- `AddStateMachine<T>(ServiceLifetime)` - Register with specific lifetime

#### `StateMachineFactory<TInterface, TImplementation, TState, TTrigger>`
Factory that selects appropriate variant at runtime.

**Features:**
- Automatic variant selection
- Service provider integration
- Lifetime management
- Started/unstarted creation options

### Exceptions

#### `ExceptionContext<TState, TTrigger>`
Provides detailed context when exceptions occur.

**Properties:**
- `TState FromState` - State when exception occurred
- `TState ToState` - Target state (if applicable)
- `TTrigger Trigger` - Trigger that caused exception
- `TransitionStage Stage` - Where exception occurred
- `Exception Exception` - The actual exception
- `object? Payload` - Payload if present

#### `ExceptionDirective`
Enum controlling exception handling behavior.

```csharp
public enum ExceptionDirective
{
    Rethrow,    // Rethrow the exception (default)
    Suppress,   // Suppress and continue
    Abort       // Abort transition and stay in current state
}
```

#### `TransitionStage`
Identifies where in transition an exception occurred.

```csharp
public enum TransitionStage
{
    Guard,      // During guard evaluation
    OnExit,     // During exit callback
    Action,     // During transition action
    OnEntry,    // During entry callback
    Internal    // During internal transition
}
```

#### `SyncCallOnAsyncMachineException`
Thrown when calling sync methods on async machine.

**Message:** "Cannot call synchronous method '{method}' on an asynchronous state machine"

### Runtime

#### Base Classes

##### `StateMachineBase<TState, TTrigger>`
Base class for all generated state machines.

**Protected Fields:**
- `_currentState` - Current state
- `_started` - Whether started
- `_transitions` - Transition table
- `_extensions` - Registered extensions

**Protected Methods:**
- `ValidateStarted()` - Ensure machine is started
- `GetTransition(state, trigger)` - Look up transition
- `ExecuteTransition(transition, payload)` - Execute transition
- `InvokeExtensions(context)` - Call extension hooks

##### `AsyncStateMachineBase<TState, TTrigger>`
Base class for async state machines.

**Inherits:** `StateMachineBase<TState, TTrigger>`

**Additional Methods:**
- `ExecuteTransitionAsync(transition, payload, token)` - Async execution
- `InvokeExtensionsAsync(context, token)` - Async extension hooks

#### Support Classes

##### `ExtensionRunner`
Manages extension execution and error handling.

**Methods:**
- `RunBeforeTransition(extensions, context)` - Pre-transition hooks
- `RunAfterTransition(extensions, context)` - Post-transition hooks
- `RunOnError(extensions, context, exception)` - Error hooks
- `SafeInvoke(action, errorHandler)` - Safe execution wrapper

##### `StateMachineContext<TState, TTrigger>`
Concrete context implementation.

**Implements:** `IStateMachineContext<TState, TTrigger>`

**Constructor Parameters:**
- `TState fromState` - Source state
- `TState toState` - Target state
- `TTrigger trigger` - Trigger
- `object? payload` - Optional payload
- `bool isInternal` - Internal transition flag

##### `TransitionEntry<TState, TTrigger>`
Represents a single transition definition.

**Properties:**
- `TState FromState` - Source state
- `TTrigger Trigger` - Trigger
- `TState ToState` - Target state
- `Func<bool>? Guard` - Guard condition
- `Action? Action` - Transition action
- `bool IsInternal` - Internal transition flag

##### `TransitionResult`
Internal result of transition attempt.

```csharp
public enum TransitionResult
{
    Success,        // Transition executed
    GuardFailed,    // Guard returned false
    NotPermitted,   // No matching transition
    Error           // Exception occurred
}
```

## Project: `IndentedStringBuilder`

**Purpose**: Provides utilities for generating properly formatted and indented source code.

### `IndentedStringBuilder`
A specialized StringBuilder for code generation with automatic indentation management.

#### Core Methods

##### Indentation Control
- `IDisposable Indent()` - Increases indentation level (returns disposable for automatic unindent)
- `void IncreaseIndent()` - Manually increase indentation
- `void DecreaseIndent()` - Manually decrease indentation

##### Content Writing
- `AppendLine(string? text)` - Append line with current indentation
- `Append(string text)` - Append without newline
- `AppendLines(params string[] lines)` - Append multiple lines

##### Block Structures
- `IDisposable Block(string header)` - Create indented block with braces
- `IDisposable Region(string name)` - Create #region block
- `IDisposable Namespace(string ns)` - Create namespace block

##### Documentation
- `WriteSummary(string summary)` - Write XML doc summary
- `WriteParam(string name, string description)` - Write parameter doc
- `WriteReturns(string description)` - Write return value doc

#### Usage Example

```csharp
var sb = new IndentedStringBuilder();
sb.AppendLine("namespace MyApp");
using (sb.Block("{"))
{
    sb.WriteSummary("My class");
    sb.AppendLine("public class MyClass");
    using (sb.Block("{"))
    {
        sb.AppendLine("// Implementation");
    }
}
```

**Output:**
```csharp
namespace MyApp
{
    /// <summary>
    /// My class
    /// </summary>
    public class MyClass
    {
        // Implementation
    }
}
```

## Project: `Generator`

**Purpose**: The core source generator that analyzes state machine definitions and generates optimized implementation code at compile time.

### Main Components

#### `StateMachineGenerator`
The main Roslyn incremental generator entry point.

```csharp
public class StateMachineGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Register syntax provider for finding state machine classes
        // 2. Combine with compilation data
        // 3. Generate source code for each valid state machine
    }
}
```

**Key Responsibilities:**
- Identifies classes marked with `[StateMachine]` attribute
- Validates state machine definitions
- Selects appropriate generation variant
- Outputs generated C# code

### Analyzers

#### `StateMachineAnalyzer`
Roslyn analyzer providing compile-time validation and diagnostics.

**Validates:**
- State and trigger types are enums
- Class is marked as partial
- No duplicate transitions
- All states are reachable
- Method signatures match expected patterns
- No mixed sync/async patterns
- HSM hierarchy is valid (no cycles)

### Helpers

#### Code Generation Helpers

##### `AsyncGenerationHelper`
Centralizes sync/async code transformations.

**Key Methods:**
- `GetAsyncKeyword(bool isAsync)` - Returns "async" or empty
- `GetAwaitKeyword(bool isAsync)` - Returns "await" or empty
- `GetTaskType(bool isAsync)` - Returns appropriate Task/ValueTask type

##### `CallbackGenerationHelper`
Generates callback invocations for all signature variants.

**Handles:**
- Parameterless callbacks
- Payload-only callbacks
- CancellationToken-only callbacks
- Combined payload and token callbacks
- Sync and async variants

##### `GuardGenerationHelper`
Generates guard condition checks.

**Features:**
- Supports sync and async guards
- Handles payload parameters
- Generates appropriate return statements

#### Analysis Helpers

##### `CallbackSignatureAnalyzer`
Analyzes method signatures to determine callback characteristics.

**Detects:**
- Async methods (returns Task/ValueTask)
- Payload parameters
- CancellationToken parameters
- Method overloads

##### `TypeSystemHelper`
Provides type system operations for code generation.

**Operations:**
- Type name resolution
- Namespace extraction
- Generic type handling
- Using directive generation

#### Factory Helpers

##### `DiagnosticFactory`
Creates Roslyn diagnostics from validation rules.

**Features:**
- Maps rule definitions to diagnostic descriptors
- Formats diagnostic messages
- Sets appropriate severity levels

##### `FactoryGenerationModelBuilder`
Builds models for DI factory generation.

**Creates:**
- Factory interfaces
- Implementation selection logic
- Service registration extensions

### Parsers

#### `StateMachineParser`
Parses and validates state machine definitions from source code.

**Main Process:**
1. **Initialization** - Set up compilation context and validation rules
2. **Attribute Parsing** - Extract StateMachine attribute and configuration
3. **Validation** - Ensure class is partial and types are valid enums
4. **Member Parsing** - Process all transition, state, and configuration attributes
5. **HSM Building** - Construct hierarchical relationships if enabled
6. **Variant Selection** - Determine optimal generation strategy
7. **Reachability Check** - Ensure all states are reachable
8. **Model Creation** - Build complete StateMachineModel

**Key Methods:**

##### `TryParse(ClassDeclarationSyntax, out StateMachineModel?)`
Main parsing entry point.

**Returns:** `true` if parsing succeeded, `false` if critical errors occurred

##### `ParseMemberAttributes(...)`
Processes all member-level attributes:
- `[Transition]` - State transitions
- `[InternalTransition]` - Internal transitions
- `[State]` - State configurations and hierarchy
- `[PayloadType]` - Trigger-specific payloads
- `[OnException]` - Exception handling

##### `BuildHierarchy(StateMachineModel, ...)`
Constructs HSM hierarchy from parsed state attributes.

**Performs:**
- Parent-child relationship mapping
- Circular dependency detection
- Depth calculation for each state
- Initial substate resolution
- History mode configuration

**Validates:**
- No circular parent-child relationships
- Each composite has at most one initial substate
- History modes are only on composite states

### Planning (HSM Support)

#### Transition Planning Architecture

The generator uses a planning system to determine the sequence of operations for state transitions, especially important for hierarchical states.

##### `ITransitionPlanner`
Interface for transition planning strategies.

```csharp
public interface ITransitionPlanner
{
    TransitionPlan BuildPlan(TransitionBuildContext context);
}
```

##### `FlatTransitionPlanner`
Planner for non-hierarchical state machines.

**Generates:**
- Simple direct transitions
- OnExit → Guard → Action → OnEntry sequence

##### `HierarchicalTransitionPlanner`
Planner for hierarchical state machines with composite states.

**Handles:**
- Exit sequence from nested states to LCA (Lowest Common Ancestor)
- Entry sequence from LCA to target state
- History state restoration
- Initial substate entry

**Algorithm:**
1. Find LCA of source and target states
2. Generate exit steps up to LCA
3. Generate entry steps down from LCA
4. Handle history if applicable
5. Enter initial substates for composites

#### Planning Models

##### `TransitionPlan`
Represents the complete execution plan for a transition.

**Properties:**
- `bool IsInternal` - Whether transition changes state
- `int FromStateIndex` - Source state index
- `int ToStateIndex` - Target state index
- `int LcaIndex` - Lowest common ancestor index (HSM)
- `List<PlanStep> Steps` - Ordered execution steps

##### `PlanStep`
Represents a single step in transition execution.

**Properties:**
- `PlanStepKind Kind` - Type of operation
- `string? StateName` - Associated state
- `string? GuardMethod` - Guard to check
- `string? ActionMethod` - Action to execute
- `string? CallbackMethod` - OnEntry/OnExit to call
- `bool IsAsync` - Whether operation is async
- `bool HasPayload` - Whether payload is passed

##### `PlanStepKind`
Types of transition steps:
- `GuardCheck` - Evaluate guard condition
- `OnExit` - Execute exit callback
- `OnEntry` - Execute entry callback
- `Action` - Execute transition action
- `InternalAction` - Execute internal transition
- `StateChange` - Update current state
- `SaveHistory` - Store history state
- `RestoreHistory` - Restore from history

### Source Generators

#### Generator Architecture

The generator uses a variant-based approach where different generators produce optimized code based on the features actually used.

##### `StateMachineCodeGenerator`
Abstract base class for all variant generators.

**Core Responsibilities:**
- Common code generation patterns
- Using directive management
- Type name resolution
- HSM transition handling

**Key Methods:**
- `Generate()` - Main generation entry point
- `WriteTransitionLogic()` - Generates transition implementation
- `WriteHierarchicalTransition()` - Handles HSM transitions
- `WriteGuardCheck()` - Generates guard evaluation
- `WriteActionCall()` - Generates action execution
- `WriteOnEntryCall()` - Generates entry callbacks
- `WriteOnExitCall()` - Generates exit callbacks

#### Variant Generators

##### `CoreVariantGenerator`
Generates minimal state machines (Pure and Basic variants).

**Features:**
- Simple switch-based transitions
- Optional OnEntry/OnExit callbacks
- No payload support
- No extension support

**Optimizations:**
- Inline transitions
- No allocations
- Minimal method calls

##### `PayloadVariantGenerator`
Adds typed payload support to state machines.

**Features:**
- Typed payload parameters
- Overloaded Fire methods
- Payload validation
- Type-safe casting

**Generated Methods:**
- `Fire(trigger, payload)`
- `TryFire(trigger, payload)`
- `FireAsync(trigger, payload, token)`

##### `ExtensionsVariantGenerator`
Adds extension support for cross-cutting concerns.

**Features:**
- Extension registration
- Hook points for transitions
- Context passing
- Error handling

**Extension Points:**
- Before transition
- After transition
- On error

##### `FullVariantGenerator`
Combines all features (payloads + extensions).

**Inherits from:** `PayloadVariantGenerator`
**Adds:** Extension support to payload variant

#### Feature Writers

##### `ExtensionsFeatureWriter`
Encapsulates extension-related code generation.

**Generates:**
- Extension collection field
- Registration methods
- Hook invocations
- Error handling

##### `VariantSelector`
Determines optimal generation variant based on usage.

**Selection Process:**
1. Detect used features (callbacks, payloads, extensions, HSM)
2. Check for forced variant via attribute
3. Select minimal variant that supports all features
4. Adjust generation flags

**Variants (in order of complexity):**
1. `Pure` - Transitions only
2. `Basic` - Add callbacks
3. `WithPayload` - Add payloads
4. `WithExtensions` - Add extensions
5. `Full` - All features

### HSM Code Generation

Hierarchical state machine support adds complexity to code generation:

#### Hierarchical Transition Generation

**Exit Sequence:**
1. Start from current state
2. Call OnExit for each state up to LCA
3. Save history if configured

**Entry Sequence:**
1. Start from LCA
2. Restore history or enter initial substates
3. Call OnEntry for each state down to target

#### History Management

**Shallow History:**
- Stores only direct child state
- Restores to stored child on re-entry

**Deep History:**
- Stores complete state path
- Restores full nested state on re-entry

#### Generated HSM Fields

```csharp
private Dictionary<TState, TState> _shallowHistory;
private Dictionary<TState, TState> _deepHistory;
private Stack<TState> _stateStack; // Current state path
```

## Project: `Generator.Model`

**Purpose**: Contains the data models and DTOs used during code generation. These models represent the parsed state machine structure and configuration.

### Core Models

#### `StateMachineModel`
The main model representing a complete state machine definition.

**Key Properties:**
- `string Namespace` - Target namespace
- `string ClassName` - State machine class name
- `List<string> ContainerClasses` - Names of containing types if the state machine class is nested
- `string StateType` - Fully qualified state enum type
- `string TriggerType` - Fully qualified trigger enum type
- `List<TransitionModel> Transitions` - All defined transitions
- `Dictionary<string, StateModel> States` - All states with configurations
- `GenerationConfig GenerationConfig` - Generation settings
- `ExceptionHandlerModel? ExceptionHandler` - Exception handling configuration
- `string? DefaultPayloadType` - Fully qualified name of default payload type
- `Dictionary<string, string> TriggerPayloadTypes` - Trigger-specific payload types
- `bool GenerateLogging` - Whether to generate logging support
- `bool GenerateDependencyInjection` - Whether to generate DI support
- `bool EmitStructuralHelpers` - Whether to emit structural API helpers
- `bool ContinueOnCapturedContext` - Control async continuation context (default: false)

**HSM-Specific Properties:**
- `Dictionary<string, string?> ParentOf` - Maps states to parent states
- `Dictionary<string, List<string>> ChildrenOf` - Maps composite states to children
- `Dictionary<string, int> Depth` - Hierarchy depth of each state
- `Dictionary<string, string?> InitialChildOf` - Initial substates for composites
- `Dictionary<string, HistoryMode> HistoryOf` - History mode for composite states
- `bool HierarchyEnabled` - Whether HSM features are enabled
- `bool HasHierarchy` - Whether hierarchy is actually used
- `bool UsedEnumOnlyFallback` - Whether enum-only fallback was used (no [State] attributes found)

#### `StateModel`
Represents a single state with its callbacks and hierarchy information.

**Core Properties:**
- `string Name` - State name
- `string OnEntryMethod` - Entry callback method name
- `string OnExitMethod` - Exit callback method name
- `CallbackSignatureInfo OnEntrySignature` - Entry method signature details
- `CallbackSignatureInfo OnExitSignature` - Exit method signature details

**Convenience Properties (derived from signatures):**
- `bool OnEntryIsAsync` - Whether OnEntry method is async
- `bool OnExitIsAsync` - Whether OnExit method is async
- `bool OnEntryExpectsPayload` - Whether OnEntry expects a payload parameter
- `bool OnEntryHasParameterlessOverload` - Whether OnEntry has parameterless overload
- `bool OnExitExpectsPayload` - Whether OnExit expects a payload parameter
- `bool OnExitHasParameterlessOverload` - Whether OnExit has parameterless overload

**HSM Properties:**
- `string? ParentState` - Parent state name for hierarchical states
- `List<string> ChildStates` - List of child state names
- `bool IsComposite` - Whether state has children
- `HistoryMode History` - History behavior for composite states
- `bool IsInitial` - Whether state is initial substate of parent

**Factory Methods:**
- `static StateModel Create(string name, string? onEntryMethod, string? onExitMethod)` - Creates a state model

#### `TransitionModel`
Represents a single state transition.

**Core Properties:**
- `string FromState` - Source state
- `string Trigger` - Triggering event
- `string ToState` - Target state
- `string? GuardMethod` - Guard condition method
- `string? ActionMethod` - Transition action method
- `bool IsInternal` - Whether transition is internal (no state change, set explicitly only for [InternalTransition])
- `int Priority` - Transition priority for HSM (default: 0)
- `CallbackSignatureInfo GuardSignature` - Guard method signature
- `CallbackSignatureInfo ActionSignature` - Action method signature
- `string? ExpectedPayloadType` - Expected payload type for this transition

**Convenience Properties (derived from signatures):**
- `bool GuardIsAsync` - Whether guard method is async
- `bool ActionIsAsync` - Whether action method is async
- `bool GuardExpectsPayload` - Whether guard expects a payload parameter
- `bool GuardHasParameterlessOverload` - Whether guard has parameterless overload
- `bool ActionExpectsPayload` - Whether action expects a payload parameter
- `bool ActionHasParameterlessOverload` - Whether action has parameterless overload

**Factory Methods:**
- `static TransitionModel Create(string fromState, string toState, string trigger, string? guardMethod, string? actionMethod, string? expectedPayloadType)` - Creates a transition model

#### `CallbackSignatureInfo`
Describes callback method signatures with support for multiple overloads.

**Properties:**
- `bool IsAsync` - Whether method is async
- `bool HasParameterless` - Has parameterless overload
- `bool HasPayloadOnly` - Has payload-only overload
- `bool HasTokenOnly` - Has CancellationToken-only overload
- `bool HasPayloadAndToken` - Has both payload and token overload
- `string? PayloadType` - Fully qualified payload type name

### Configuration Models

#### `GenerationConfig`
Contains settings that control code generation.

**Properties:**
- `GenerationVariant Variant` - Selected generation variant
- `bool UsePayloads` - Whether payloads are used
- `bool UseExtensions` - Whether extensions are enabled
- `bool UseCallbacks` - Whether state callbacks are used
- `bool IsAsync` - Whether machine is async

#### `GenerationVariant`
Enum defining generation strategies:
- `Pure` - Minimal, transitions only
- `Basic` - Adds OnEntry/OnExit callbacks
- `WithPayload` - Adds typed payload support
- `WithExtensions` - Adds extension support
- `Full` - All features enabled

#### `ExceptionHandlerModel`
Represents exception handling configuration.

**Properties:**
- `string MethodName` - Handler method name
- `CallbackSignatureInfo Signature` - Handler signature details

### Data Transfer Objects

#### `FactoryGenerationModel`
Data model for dependency injection factory generation.

**Properties:**
- `string Namespace` - Target namespace
- `string InterfaceName` - Generated interface name
- `string ImplementationName` - Implementation class name
- `List<VariantInfo> Variants` - Available state machine variants

#### `TypeGenerationInfo`
Pre-processed type information for efficient code generation.

**Properties:**
- `string FullName` - Fully qualified type name
- `string SimpleName` - Simple type name
- `bool RequiresUsing` - Whether using directive needed
- `string? Namespace` - Type namespace

## Project: `Generator.Rules`

**Purpose**: Provides compile-time validation rules and diagnostics for state machine definitions.

### Validation Architecture

#### Rule System

Each rule consists of:
1. **Rule Definition** - Metadata about the rule
2. **Rule Implementation** - Validation logic
3. **Diagnostic Descriptor** - Roslyn diagnostic information

### Core Components

#### Definitions

##### `RuleDefinition`
Defines a validation rule's metadata.

**Properties:**
- `string Id` - Unique identifier (e.g., "FSM001")
- `string Title` - Short description
- `string MessageFormat` - Diagnostic message template
- `RuleSeverity Severity` - Error, Warning, or Info
- `string Category` - Rule category

##### `RuleCatalog`
Central, validated catalog of all rule definitions.

**Static Methods:**
- `RuleDefinition Get(string id)` - Returns rule definition by ID or throws if unknown
- `bool TryGet(string id, out RuleDefinition def)` - Tries to get a rule by ID
- `IReadOnlyList<RuleDefinition> All` - Returns all rule definitions

**Features:**
- Validates unique rule IDs at static initialization
- Ensures all rules have required fields (Id, Title, MessageFormat, Category)
- Provides centralized access to rule definitions

##### `RuleCategories`
Central constants for rule categories.

**Constants:**
- `FSM_Generator` - "FSM.Generator" - General generator rules
- `FSM_Generator_Async` - "FSM.Generator.Async" - Async-specific rules
- `FSM_Generator_HSM` - "FSM.Generator.HSM" - HSM-specific rules

##### `RuleLookup`
Provides strongly-typed access to rule definitions (if present).

##### `SeverityDefaults`
Defines default severity levels for rules (if present).

##### `RuleSeverity`
Validation severity levels.

```csharp
public enum RuleSeverity
{
    Error,      // Prevents compilation
    Warning,    // Compilation succeeds with warning
    Info        // Informational message
}
```

##### `ValidationResult`
Result from rule validation.

**Properties:**
- `bool IsValid` - Whether validation passed
- `string? ErrorMessage` - Error message if failed
- `Location? Location` - Source location
- `DiagnosticSeverity Severity` - Diagnostic severity

### Validation Rules

#### Structural Rules

##### `MissingStateMachineAttributeRule`
**ID:** FSM001  
**Validates:** Class has [StateMachine] attribute  
**Error:** "Class must be marked with [StateMachine] attribute"

##### `NonPartialClassRule`
**ID:** FSM002  
**Validates:** Class is marked as partial  
**Error:** "State machine class must be partial"

##### `InvalidEnumTypeRule`
**ID:** FSM003  
**Validates:** State and Trigger types are enums  
**Error:** "State and Trigger types must be enums"

#### Transition Rules

##### `DuplicateTransitionRule`
**ID:** FSM010  
**Validates:** No duplicate transitions  
**Error:** "Duplicate transition from {0} on {1}"

##### `InvalidEnumValueInTransitionRule`
**ID:** FSM011  
**Validates:** Transition states/triggers are valid enum values  
**Error:** "Invalid enum value {0} for type {1}"

##### `UnreachableStateRule`
**ID:** FSM012  
**Validates:** All states are reachable from initial state  
**Warning:** "State {0} is unreachable"

#### Method Signature Rules

##### `InvalidMethodSignatureRule`
**ID:** FSM020  
**Validates:** Callback methods have valid signatures  
**Error:** "Method {0} has invalid signature for {1}"

##### `MixedModeRule`
**ID:** FSM021  
**Validates:** Consistent sync/async usage  
**Error:** "Cannot mix sync and async methods in state machine"

##### `GuardWithPayloadInNonPayloadMachineRule`
**ID:** FSM022  
**Validates:** Payload usage consistency  
**Error:** "Guard expects payload but machine has no payload type"

#### HSM Rules

##### `CircularHierarchyRule`
**ID:** FSM030  
**Validates:** No circular parent-child relationships  
**Error:** "Circular hierarchy detected: {0}"

##### `MultipleInitialStatesRule`
**ID:** FSM031  
**Validates:** At most one initial substate per parent  
**Error:** "Parent state {0} has multiple initial substates"

##### `InvalidHistoryModeRule`
**ID:** FSM032  
**Validates:** History only on composite states  
**Error:** "History mode can only be set on composite states"

##### `OrphanSubstateRule`
**ID:** FSM033  
**Validates:** Substates have valid parents  
**Error:** "State {0} references non-existent parent {1}"

### Contexts

Validation contexts provide information to rules:

#### `ValidationContext`
Base context for all validations.

**Properties:**
- `Compilation Compilation` - Roslyn compilation
- `INamedTypeSymbol ClassSymbol` - State machine class
- `StateMachineModel Model` - Parsed model

#### `TransitionValidationContext`
Context for transition validation.

**Inherits:** `ValidationContext`  
**Additional Properties:**
- `TransitionModel Transition` - Transition being validated
- `AttributeData AttributeData` - Source attribute

#### `StateValidationContext`
Context for state validation.

**Inherits:** `ValidationContext`  
**Additional Properties:**
- `StateModel State` - State being validated
- `AttributeData AttributeData` - Source attribute

## Project: `Generator.DependencyInjection`

**Purpose**: Generates dependency injection integration code for state machines, including factories and service registration extensions.

### `FactoryCodeGenerator`
Generates DI factory and registration extensions.

#### Generated Components

##### Factory Interface
Interface for creating state machine instances.

```csharp
public interface IOrderWorkflowFactory
{
    IOrderWorkflow Create(OrderState initialState);
    IOrderWorkflow CreateStarted(OrderState initialState);
    Task<IOrderWorkflow> CreateStartedAsync(OrderState initialState);
}
```

##### Factory Implementation
Selects appropriate variant based on features.

```csharp
public class OrderWorkflowFactory : IOrderWorkflowFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public IOrderWorkflow Create(OrderState initialState)
    {
        // Variant selection logic
        return variant switch
        {
            "Pure" => new OrderWorkflow_Pure(initialState),
            "WithPayload" => new OrderWorkflow_WithPayload(initialState),
            _ => new OrderWorkflow_Full(initialState)
        };
    }
}
```

##### Service Registration Extensions
Extension methods for IServiceCollection.

```csharp
public static class OrderWorkflowServiceExtensions
{
    public static IServiceCollection AddOrderWorkflow(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.Add(new ServiceDescriptor(
            typeof(IOrderWorkflowFactory),
            typeof(OrderWorkflowFactory),
            lifetime));
        return services;
    }
}
```

#### Features

- **Automatic Variant Selection** - Chooses optimal implementation
- **Service Provider Integration** - Resolves dependencies
- **Lifetime Management** - Supports all DI lifetimes
- **Async Support** - CreateStartedAsync for async machines
- **Interface-Based** - Enables testing and mocking

## Additional Projects

### Testing Projects

#### `StateMachine.Tests`
**Purpose**: Core unit tests for state machine functionality.

**Coverage:**
- Basic transitions
- Guards and actions
- State callbacks
- Payload handling
- Exception handling
- HSM features

#### `StateMachine.Async.Tests`
**Purpose**: Tests for asynchronous state machine features.

**Coverage:**
- Async transitions
- Async callbacks
- CancellationToken support
- ValueTask performance
- ConfigureAwait behavior

#### `StateMachine.DependencyInjection.Tests`
**Purpose**: Tests for DI integration.

**Coverage:**
- Factory creation
- Service registration
- Lifetime management
- Variant selection

### Benchmark Projects

#### `Benchmark`
**Purpose**: .NET performance benchmarks using BenchmarkDotNet.

**Scenarios:**
- Basic transitions
- Guards and actions
- Payload handling
- Async operations
- Memory allocations

#### Cross-Language Benchmarks

**Purpose**: Compare FastFSM performance against other languages.

- **`Benchmark.cpp`** - C++ with Boost.SML
- **`Benchmark.Java`** - Java with Spring StateMachine
- **`Benchmark.JavaScript`** - Pure JavaScript implementation
- **`Benchmark.Rust`** - Rust with Statig library
- **`Benchmark.TypeScript`** - TypeScript implementation

### Extension Projects

#### `StateMachine.Logging`
**Purpose**: Structured logging support for state machines.

**Features:**
- Transition logging
- State change events
- Performance metrics
- Error logging
- Integration with ILogger

## API Usage Examples

### Basic State Machine

```csharp
[StateMachine(typeof(DoorState), typeof(DoorTrigger))]
public partial class DoorController
{
    [Transition(DoorState.Closed, DoorTrigger.Open, DoorState.Open)]
    [Transition(DoorState.Open, DoorTrigger.Close, DoorState.Closed)]
    private void Configure() { }
}

// Usage
var door = new DoorController(DoorState.Closed);
door.Start();
door.Fire(DoorTrigger.Open);
```

### Hierarchical State Machine

```csharp
[StateMachine(typeof(PhoneState), typeof(PhoneTrigger), EnableHierarchy = true)]
public partial class PhoneController
{
    // Define hierarchy
    [State(PhoneState.OnHook)]
    [State(PhoneState.OffHook)]
    [State(PhoneState.Connected, Parent = PhoneState.OffHook, IsInitial = true)]
    [State(PhoneState.OnHold, Parent = PhoneState.OffHook)]
    [State(PhoneState.Ringing, Parent = PhoneState.OffHook)]
    private void ConfigureStates() { }
    
    // Transitions respect hierarchy
    [Transition(PhoneState.OnHook, PhoneTrigger.IncomingCall, PhoneState.Ringing)]
    [Transition(PhoneState.Ringing, PhoneTrigger.Answer, PhoneState.Connected)]
    private void ConfigureTransitions() { }
}
```

### With Dependency Injection

```csharp
// Registration
services.AddStateMachineFactory();
services.AddScoped<OrderWorkflow>();

// Usage
public class OrderService
{
    private readonly IStateMachineFactory _factory;
    
    public async Task ProcessOrder()
    {
        var fsm = await _factory.CreateStartedAsync<OrderWorkflow>(
            OrderState.New);
        await fsm.FireAsync(OrderTrigger.Submit);
    }
}
```

## Performance Characteristics

### Zero-Overhead Abstraction

- **Compile-time Generation** - No runtime reflection
- **Switch-based Dispatch** - CPU branch prediction friendly
- **Struct-based Context** - Stack allocated, no heap pressure
- **ValueTask Returns** - Allocation-free async hot path

### Benchmark Results (AMD Ryzen 5 9600X)

| Operation | Time | Allocations |
|-----------|------|-------------|
| Basic Transition | 0.81 ns | 0 B |
| With Guard | 2.18 ns | 0 B |
| With Payload | 0.83 ns | 0 B |
| Async (hot path) | 444 ns | 0 B |

## Further Reading

- [README.md](README.md) - Getting started guide
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines
- [Examples/](Examples/) - Sample applications
- [Docs/](Docs/) - Additional documentation
