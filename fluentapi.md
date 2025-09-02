# Implementation Plan for FastFSM 0.7.5 (Fluent API Integration)

## Parser Development Approach Using Parallel Parsing Infrastructure

**IMPORTANT**: Development of the Fluent API parser should utilize the parallel parsing infrastructure that has been implemented. This allows for iterative development while comparing outputs against the existing attribute-based parser.

**PROGRESS TRACKING**: All implementation progress should be documented in `progressfluent.md` file. This includes completed milestones, current issues, and next steps.

### Current Setup Overview

The parallel parsing infrastructure is now fully operational with the following components:

#### 1. **Dual Parser Architecture**
- **StateMachineParser** - Original attribute-based parser (existing)
- **FluentParser** - New fluent API parser (in development)
- Both implement `IStateMachineParser` interface for consistency

#### 2. **Test Project Structure**
The `ParserComparison.Tests` project contains:
- **SimpleStateMachine.cs** - Uses attribute-based API (reference implementation)
- **SimpleFluentMachine.cs** - Uses fluent API (parallel implementation)
- Both machines implement identical functionality for comparison

#### 3. **Generator Integration**
Generator.cs has been modified to:
- Run both parsers on every state machine class
- Use FluentParser as fallback when StateMachineParser fails
- Embed JSON models from both parsers in generated code comments

### Development Workflow:

1. **Define Test Machines in ParserComparison.Tests**
   - Create two versions: one with attributes, one with fluent API
   - Both should represent the same state machine logic
   - Use consistent naming: `XxxStateMachine` (attributes) and `XxxFluentMachine` (fluent)

2. **Implement Parsing Logic in FluentParser.cs**
   - Located in `/Generator/Parsers/FluentParser.cs`
   - Parse the `Configure()` method to extract state machine definition
   - Build `StateMachineModel` matching the attribute parser's output

3. **Build and Verify**
   ```bash
   cd ParserComparison.Tests
   dotnet build
   ```

4. **Check Generated Files**
   - Generated files are in `ParserComparison.Tests/Generated/Generator/Generator.StateMachineGenerator/`
   - Each file ends with JSON comparison section

5. **Compare JSON Models**
   - Look for `====== PARSER COMPARISON DEBUG INFO ======` section
   - Contains "Original StateMachineParser Model" and "FluentParser Model"
   - Models should be identical when parsing is correctly implemented

### How to Use the Setup:

#### For LLM Agents/Developers:

1. **To add a new test case:**
   ```csharp
   // In ParserComparison.Tests, create attribute version:
   [StateMachine(typeof(State), typeof(Trigger))]
   public partial class NewTestStateMachine
   {
       [State(State.A)]
       private void StateA() { }
       // ... transitions with attributes
   }
   
   // Create equivalent fluent version:
   [StateMachine(typeof(State), typeof(Trigger))]
   public partial class NewTestFluentMachine
   {
       private static void Configure() => FSM
           .State(State.A)
           // ... transitions with fluent API
   }
   ```

2. **To verify your implementation:**
   - Build the project
   - Open the generated file
   - Scroll to the bottom
   - Compare the two JSON models
   - If they match, your fluent parser is working correctly for that case

3. **To debug parser issues:**
   - FluentParser includes diagnostic logging via `report` parameter
   - Check build output for `[FluentParser]` messages
   - Add more `report?.Invoke()` calls to trace parsing steps

#### Example JSON Comparison (from actual generated file):
```json
====== PARSER COMPARISON DEBUG INFO ======
Original StateMachineParser Model:
{
  "Transitions": [
    {
      "FromState": "Idle",
      "ToState": "Processing",
      "Trigger": "Start",
      ...
    }
  ],
  ...
}

FluentParser Model:
{
  "Transitions": [
    {
      "FromState": "Idle", 
      "ToState": "Processing",
      "Trigger": "Start",
      ...
    }
  ],
  ...
}
```

### Current Implementation Status:

✅ **Working Features:**
- Basic state definitions
- Simple transitions (`.On().GoTo()`)
- Internal transitions (`.On().Action()` without `.GoTo()`)
- Action methods via `nameof()`
- JSON serialization for comparison

✅ **Completed Features:**
- Guard conditions (`.Guard(nameof(Method))`)
- Entry/Exit actions (`.OnEntry()`, `.OnExit()`, `.OnEntryAsync()`, `.OnExitAsync()`)
- Async actions (`.GuardAsync()`, `.ActionAsync()`)
- Payload support (`.Payload<T>()` and `DefaultPayloadType`)
- Internal transitions (`.OnInternal().Action().Internal()`)

⚠️ **In Progress:**
- Hierarchical states (HSM)
- History modes
- Priorities

❌ **Not Started:**
- Extension hooks
- Complex HSM scenarios

### Tips for Successful Development:

1. **Start Simple**: Begin with basic FSM features before tackling HSM
2. **Test Incrementally**: Add one feature at a time and verify JSON output
3. **Match Exactly**: JSON models must be identical, including all properties
4. **Use Existing Tests**: SimpleStateMachine/SimpleFluentMachine are good references
5. **Check Both Parsers**: Sometimes the issue is understanding what StateMachineParser generates

## Core Design Principles (Immutable Rules):
- Only `enum`/literals/`nameof`/`typeof` in DSL arguments
- **Zero** lambdas/delegates in DSL (actions/guards only via `nameof`)
- **No** imperative logic in `Configure()` method (no `if/for/...`)
- Everything enclosed within the machine class (enums, payloads, methods, DSL)

---

## 1. Review Current Codebase (Branch fluentapiv2)
Start by familiarizing with the existing FastFSM code, especially the gen-upd branch. Understand how state machines are currently defined using attributes and how the Roslyn source generator processes them. Note any initial implementations or scaffolding for a fluent API in this branch. This review will guide the design to ensure the new fluent approach aligns with the existing architecture (e.g. how states, triggers, transitions, and hierarchical states (HSM) are represented internally).

## 2. Design of the Fluent API Structure

### Example State Machine Definitions

#### 1) Simple FSM (minimal)
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleMachine
{
    public enum State { A, B }
    public enum Trigger { Next }
    
    private static void Configure() => FSM
        .State(State.A)
            .On(Trigger.Next).GoTo(State.B)
        .State(State.B);
}
```

#### 2) FSM with Actions and Guards
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class GuardActionMachine
{
    public enum State { Idle, Running, Stopped }
    public enum Trigger { Start, Stop }
    
    private int _quota;
    
    private static void Configure() => FSM
        .State(State.Idle)
            .OnEntry(nameof(OnIdleEntry))
            .On(Trigger.Start).GoTo(State.Running)
                .Guard(nameof(HasQuota)).Action(nameof(OnStart))
        .State(State.Running)
            .On(Trigger.Stop).GoTo(State.Stopped)
                .Action(nameof(OnStop))
        .State(State.Stopped)
            .OnExit(nameof(OnStoppedExit));
    
    // GUARD / ACTIONS
    private bool HasQuota() => _quota > 0;
    private void OnStart() { _quota--; }
    private void OnStop() { /* ... */ }
    private void OnIdleEntry() { /* ... */ }
    private void OnStoppedExit() { /* ... */ }
}
```

#### 3) FSM with Single Payload (DefaultPayloadType)
```csharp
[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(JobData))]
public partial class SinglePayloadMachine
{
    public enum State { Idle, Running }
    public enum Trigger { Start, Update, Stop }
    
    public sealed class JobData
    {
        public required string Id { get; init; }
        public int Priority { get; init; }
    }
    
    private int _runningCount;
    
    private static void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start)
                .Guard(nameof(CanStart))
                .Action(nameof(StartJob))
                .GoTo(State.Running)
        .State(State.Running)
            .On(Trigger.Update)
                .Action(nameof(UpdateJob))
                .GoTo(State.Running)
            .On(Trigger.Stop)
                .Action(nameof(StopJob))
                .GoTo(State.Idle);
    
    // When DefaultPayloadType is set, all transitions automatically use it
    // payload-aware guard/action signatures:
    private bool CanStart(JobData data) => _runningCount < 4 && data.Priority >= 0;
    private void StartJob(JobData data) { _runningCount++; /* ... */ }
    private void UpdateJob(JobData data) { /* ... */ }
    private void StopJob() { _runningCount--; /* ... */ }
}
```

#### 4) FSM with Multiple Payload Types (using .Payload<T>())
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class MultiPayloadMachine
{
    public enum State { Initial, Processing, Complete, Failed }
    public enum Trigger { Submit, Process, Success, Error }
    
    // Different payload types for different transitions
    public sealed class SubmitRequest { public required string Id { get; init; } }
    public sealed class ProcessData { public int Count { get; init; } }
    public sealed class ErrorInfo { public required string Message { get; init; } }
    
    private static void Configure() => FSM
        .State(State.Initial)
            .On(Trigger.Submit)
                .Payload<SubmitRequest>()  // Specify payload type for this transition
                .Guard(nameof(ValidateSubmit))
                .Action(nameof(HandleSubmit))
                .GoTo(State.Processing)
        .State(State.Processing)
            .On(Trigger.Process)
                .Payload<ProcessData>()    // Different payload type
                .Action(nameof(ProcessItem))
                .GoTo(State.Processing)    // Self-transition
            .On(Trigger.Success)
                .Action(nameof(Complete))  // No payload
                .GoTo(State.Complete)
            .On(Trigger.Error)
                .Payload<ErrorInfo>()      // Error-specific payload
                .Action(nameof(HandleError))
                .GoTo(State.Failed);
    
    // Methods with specific payload types
    private bool ValidateSubmit(SubmitRequest req) => !string.IsNullOrEmpty(req.Id);
    private void HandleSubmit(SubmitRequest req) { /* ... */ }
    private void ProcessItem(ProcessData data) { /* ... */ }
    private void HandleError(ErrorInfo error) { /* ... */ }
    private void Complete() { /* ... */ }
}
```

#### 5) FSM with Composite Payload (alternative multi-data approach)
> When multiple data needs to be passed together, use a composite type.

```csharp
[StateMachine(typeof(State), typeof(Trigger), DefaultPayloadType = typeof(OperationData))]
public partial class CompositePayloadMachine
{
    public enum State { Ready, Busy }
    public enum Trigger { Begin, Tick, End }
    
    public sealed class OperationData
    {
        public required string CorrelationId { get; init; }
        public required string User { get; init; }
        public int Attempt { get; init; }
    }
    
    private static void Configure() => FSM
        .State(State.Ready)
            .On(Trigger.Begin).GoTo(State.Busy)
                .Guard(nameof(ValidateBegin)).Action(nameof(OnBegin))
        .State(State.Busy)
            .On(Trigger.Tick).GoTo(State.Busy)
                .Action(nameof(OnTick))
            .On(Trigger.End).GoTo(State.Ready)
                .Action(nameof(OnEnd));
    
    private bool ValidateBegin(OperationData d) => d.Attempt >= 0 && d.User != null;
    private void OnBegin(OperationData d) { /* ... */ }
    private void OnTick(OperationData d) { /* ... */ }
    private void OnEnd() { /* ... */ }
}
```

#### 6) FSM with Extensions (generator hooks)
```csharp
[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class ExtensibleMachine
{
    public enum State { S1, S2 }
    public enum Trigger { Go }
    
    private static void Configure() => FSM
        .State(State.S1)
            .On(Trigger.Go).GoTo(State.S2)
                .Action(nameof(OnGo))
        .State(State.S2);
    
    private void OnGo() { /* ... */ }
    
    // EXTENSION HOOKS (called by generated base, contractual names)
    protected void OnBeforeTransition(object ctx) { /* trace start */ }
    protected void OnGuardEvaluation(object ctx, string guardName) { /* ... */ }
    protected void OnGuardEvaluated(object ctx, string guardName, bool result) { /* ... */ }
    protected void OnAfterTransition(object ctx, bool success) { /* trace end */ }
}
```

#### 7) HSM — Basics (parent/child + internal)
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class HsmBasicMachine
{
    public enum State
    {
        Idle,
        Processing,           // parent
        Processing_Loading,   // child
        Processing_Working,   // child
        Done
    }
    
    public enum Trigger { Start, Progress, Finish }
    
    private static void Configure() => FSM
        // PARENT with Internal:
        .State(State.Processing)
            .OnInternal(Trigger.Progress).Action(nameof(LogProgress))
        
        // CHILDREN + initial:
        .State(State.Processing_Loading).Parent(State.Processing).IsInitial()
        .State(State.Processing_Working).Parent(State.Processing)
        
        // Entry to parent -> auto enters Initial child:
        .State(State.Idle)
            .On(Trigger.Start).GoTo(State.Processing)
        
        // Exit to Done (e.g. from Working):
        .State(State.Processing_Working)
            .On(Trigger.Finish).GoTo(State.Done)
        .State(State.Done);
    
    private void LogProgress() { /* internal action, no exit/entry */ }
}
```

#### 8) HSM — History (Shallow & Deep)
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class HsmHistoryMachine
{
    public enum State
    {
        Root,
        A, A1, A2, A2a,  // A – with history
        B
    }
    
    public enum Trigger { ToA, ToB, Next }
    
    private static void Configure() => FSM
        // Parent A with SHALLOW history:
        .State(State.A).WithHistory(HistoryMode.Shallow)
        .State(State.A1).Parent(State.A).IsInitial()
        .State(State.A2).Parent(State.A)
        .State(State.A2a).Parent(State.A2)  // nested descendant
        
        // Root -> A; A remembers last direct child (A1 or A2):
        .State(State.Root)
            .On(Trigger.ToA).GoTo(State.A)
        
        // Example navigation within A:
        .State(State.A1)
            .On(Trigger.Next).GoTo(State.A2)
        .State(State.A2)
            .On(Trigger.Next).GoTo(State.A1)
        
        // Exit from A to B and return to A restores shallow-history:
        .State(State.B)
            .On(Trigger.ToA).GoTo(State.A)
        
        // transition to B from anywhere in A:
        .State(State.A2a)
            .On(Trigger.ToB).GoTo(State.B);
    
    // DEEP variant (alternate parent) – example:
    // .State(State.A).WithHistory(HistoryMode.Deep) // remembers deepest descendant (e.g. A2a)
}
```

#### 9) HSM — Transition Priorities (parent vs child)
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class HsmPriorityMachine
{
    public enum State { Parent, Child, Other }
    public enum Trigger { X }
    
    private static void Configure() => FSM
        .State(State.Parent)
            // Parent responds to X, but with lower priority:
            .On(Trigger.X).GoTo(State.Other).Priority(0)
        .State(State.Child).Parent(State.Parent)
            // Child also responds to X – we want Child to WIN:
            .On(Trigger.X).GoTo(State.Parent).Priority(10);
    
    // Semantics: on X both transitions are considered;
    // higher Priority decides the choice (Child wins).
}
```

#### 10) HSM — Internal in parent + normal in child (parallel rules)
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class HsmInternalVsChildMachine
{
    public enum State { Parent, Child }
    public enum Trigger { Ping }
    
    private static void Configure() => FSM
        .State(State.Parent)
            .OnInternal(Trigger.Ping).Action(nameof(ParentPing))  // no state change
        .State(State.Child).Parent(State.Parent)
            .On(Trigger.Ping).GoTo(State.Child).Action(nameof(ChildPing));  // self-loop in child
    
    private void ParentPing() { /* ... */ }
    private void ChildPing() { /* ... */ }
}
```

#### 11) FSM — Async actions (ValueTask/Task)
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class AsyncMachine
{
    public enum State { Disconnected, Connecting, Connected }
    public enum Trigger { Connect, ConnectedOk, Disconnect }
    
    private static void Configure() => FSM
        .State(State.Disconnected)
            .On(Trigger.Connect)
                .GuardAsync(nameof(CanConnectAsync))  // Async guard
                .ActionAsync(nameof(BeginConnectAsync))  // Async action
                .GoTo(State.Connecting)
        .State(State.Connecting)
            .OnEntryAsync(nameof(OnConnectingEntryAsync))  // Async OnEntry
            .On(Trigger.ConnectedOk).GoTo(State.Connected)
        .State(State.Connected)
            .OnExitAsync(nameof(OnConnectedExitAsync))  // Async OnExit
            .On(Trigger.Disconnect)
                .ActionAsync(nameof(CloseAsync))
                .GoTo(State.Disconnected);
    
    // Async methods with CancellationToken support
    private async ValueTask<bool> CanConnectAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return true;
    }
    
    private async Task BeginConnectAsync(CancellationToken ct)
    {
        await Task.Delay(500, ct);
    }
    
    private async Task OnConnectingEntryAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
    }
    
    private async ValueTask OnConnectedExitAsync()  // OnExit never receives payload
    {
        await Task.Delay(50);
    }
    
    private async Task CloseAsync(CancellationToken ct)
    {
        await Task.Delay(200, ct);
    }
}
```

#### 12) FSM — Async with Payload and CancellationToken
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class AsyncMachine
{
    public enum State { Disconnected, Connecting, Connected }
    public enum Trigger { Connect, ConnectedOk, Disconnect }
    
    private static void Configure() => FSM
        .State(State.Disconnected)
            .On(Trigger.Connect).GoTo(State.Connecting).Action(nameof(BeginConnectAsync))
        .State(State.Connecting)
            .On(Trigger.ConnectedOk).GoTo(State.Connected)
        .State(State.Connected)
            .On(Trigger.Disconnect).GoTo(State.Disconnected).Action(nameof(CloseAsync));
    
    private async ValueTask BeginConnectAsync()
    {
        await Task.Yield();  // simulation
    }
    
    private async Task CloseAsync()
    {
        await Task.Yield();  // simulation
    }
}
```

#### 13) FSM — Internal Transitions with Payload
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class InternalTransitionMachine
{
    public enum State { Active, Inactive }
    public enum Trigger { Update, Toggle }
    
    public sealed class UpdateData { public int Value { get; init; } }
    
    private int _counter;
    
    private static void Configure() => FSM
        .State(State.Active)
            .OnInternal(Trigger.Update)  // Internal transition (no state change)
                .Payload<UpdateData>()
                .Guard(nameof(ValidateUpdate))
                .Action(nameof(ApplyUpdate))
                .Internal()  // Marks as internal
            .On(Trigger.Toggle).GoTo(State.Inactive)
        .State(State.Inactive)
            .On(Trigger.Toggle).GoTo(State.Active);
    
    private bool ValidateUpdate(UpdateData data) => data.Value >= 0;
    private void ApplyUpdate(UpdateData data) { _counter += data.Value; }
}
```

#### 14) FSM — Method Overloading (with and without payload)
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class OverloadingMachine
{
    public enum State { Ready, Busy }
    public enum Trigger { Start }
    
    public sealed class StartParams { public bool FastMode { get; init; } }
    
    private static void Configure() => FSM
        .State(State.Ready)
            .On(Trigger.Start)
                .Payload<StartParams>()  // Optional payload
                .Guard(nameof(CanStart))  // Has overloads
                .Action(nameof(DoStart))  // Has overloads
                .GoTo(State.Busy);
    
    // Method overloading - generator picks based on payload availability
    private bool CanStart() => true;  // Called when no payload
    private bool CanStart(StartParams p) => !p.FastMode || IsReady();  // Called with payload
    
    private void DoStart() { /* default start */ }
    private void DoStart(StartParams p) { /* start with params */ }
    
    private bool IsReady() => true;
}
```

#### 15) HSM + Async (internal async in parent)
```csharp
[StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
public partial class HsmAsyncInternalMachine
{
    public enum State { Parent, Child }
    public enum Trigger { Tick }
    
    private static void Configure() => FSM
        .State(State.Parent)
            .OnInternal(Trigger.Tick).Action(nameof(ParentTickAsync))
        .State(State.Child).Parent(State.Parent).IsInitial();
    
    private async ValueTask ParentTickAsync()
    {
        await Task.Yield();
    }
}
```

### DSL Implementation Notes (Fixed Grammar)

For the above examples, we assume the following **rigid** set of methods (all compile-time only, runtime no-op):

**State Definitions**
* `.State(TState)`
* `.Parent(TState)` *(HSM)*
* `.IsInitial()` *(HSM)*
* `.WithHistory(HistoryMode)` *(HSM parent)*
* `.OnEntry(string methodName)`
* `.OnExit(string methodName)`

**Transitions from/to**
* `.On(TTrigger)` - Start defining a transition
* `.OnInternal(TTrigger)` - Start defining internal transition (no state change)
* `.GoTo(TState)` - Set target state (for external transitions)
* `.Internal()` - Mark as internal transition (alternative to GoTo)

**Transition Modifiers (must be called BEFORE .GoTo())**
* `.Payload<T>()` or `.Payload(typeof(T))` - Specify payload type for transition
* `.Guard(string methodName)` - Synchronous guard
* `.GuardAsync(string methodName)` - Async guard
* `.Action(string methodName)` - Synchronous action
* `.ActionAsync(string methodName)` - Async action
* `.Priority(int priority)` - HSM parent/child conflict resolution

**Important:** The order matters! Use: `.On().Payload().Guard().Action().GoTo()`

Ensure the design covers **all features** currently supported by attributes – including hierarchical state relationships (parent/child states for HSMs), internal transitions, entry/exit actions, guard conditions, and event payloads (both single and multiple parameters). The design should also determine how the fluent definitions will be recognized by the source generator (e.g. perhaps still using a [StateMachine] attribute to mark the class and indicate state/trigger types, or an alternative marker). Aim for a clear, intuitive API that will become the default way to define state machines, while coexisting with the attribute system.

## 3. Implement Fluent API Classes (FastFsm.Fluent)

Develop the classes and methods that realize the fluent interface designed in the previous step. Create a new namespace or module FastFsm.Fluent (or similar) containing the builder classes and extension methods needed for the DSL. For example, implement a StateMachineBuilder<TState, TTrigger> class that accumulates the state machine definition. Provide methods to add states (State(stateEnumValue)), mark initial state (IsInitial() or AsInitial()), define state hierarchy (AsSubstateOf(parentState) for HSM), and configure transitions (On(trigger).MoveTo(state) or similar fluent syntax). Include methods to attach **guards** (WithGuard(Func<bool>) or guard method names), **actions** (WithAction(Action) or action method names), and **payload** types (this might be through generic parameters or separate builder for events with data). Make sure the builder internally stores the definition (states, transitions, etc.) in a structure that can later be consumed by the source generator. Also implement support for special cases like asynchronous actions (if the generator needs to know about async Task methods, ensure the fluent API can mark or accept those). This step results in a fully functional fluent DSL in code, but not yet hooked into generation.

## 4. Extend the Source Generator to Parse Fluent Definitions

**CRITICAL**: Use the parallel parsing infrastructure for development!

Modify the FastFSM Roslyn source generator so that it can detect and process the new fluent-style state machine definitions **in parallel** with the existing attribute approach. The implementation should be done in `FluentParser.cs` which already implements the `IStateMachineParser` interface.

### Implementation Strategy:
1. **Detect fluent definitions**: Scan for classes with `Configure()` method using fluent API
2. **Parse fluent calls**: Traverse the syntax tree of the Configure method body
3. **Build the model**: Populate the same `StateMachineModel` structure used by attribute parser
4. **Test iteratively**: Use ParserComparison.Tests to verify JSON models match

The parser should recognize:
- Builder method calls and their arguments (state enums, trigger enums, target states)
- Guard/action method names passed via `nameof()`
- Parent-child relationships for HSM
- History modes, priorities, and other modifiers

### Testing During Development:
- Add test machines to ParserComparison.Tests project
- Build to generate code with both parsers
- Check the JSON comparison at the end of generated files
- Iterate until models match exactly

## 5. Maintain Backward Compatibility (Parallel Attribute & Fluent Support)

As you implement the above, ensure that the old attribute-based system remains fully functional and unchanged for existing users. The fluent implementation should be additive – **no breaking changes**. The source generator must support projects that use attributes, projects that use fluent, or even a mix of both styles, without conflicts. Verify that a class using fluent DSL does not require attribute annotations (aside from possibly the main [StateMachine] attribute for types, if that's part of the design), and vice versa. If both systems are used in one project, the generator should be able to process them concurrently. This may involve differentiating definitions by context (e.g. if a class has attribute-defined transitions, use that data; if it instead uses the builder, use that). Ensure that there are no name collisions or double-processing (the generator should not generate duplicate code). It might be useful to unify the internal representation such that whether a state machine is defined by attributes or fluent calls, they end up in the same data model for code generation. By the end of this task, the code generation output for a given state machine (its auto-generated .g.cs code) should be equivalent regardless of definition style. This guarantees that existing attribute-based machines behave exactly the same after the update, while new fluent-defined machines generate correct code too.

## 6. Testing the Fluent Implementation Thoroughly

Create a comprehensive test suite for the new fluent API alongside existing tests. Use the ParserComparison.Tests project as the primary testing ground:

**Test Coverage Required:**
- **Basic FSM**: Simple two-state machine with transitions
- **Guards & Actions**: Transitions with guard conditions and actions
- **Payload Events**: Triggers with associated payload types
- **Hierarchical States (HSM)**: Parent-child state relationships
- **Mixed Definitions**: Both attribute and fluent in same project

**Verification Process:**
1. Define each test case in ParserComparison.Tests
2. Build and check generated JSON models
3. Ensure both parsers produce identical models
4. Verify generated code compiles and runs correctly

All existing unit tests and examples for attribute-based definitions should still pass. If any regressions appear, fix them promptly to maintain backward compatibility.

## 7. Update Documentation and Examples

Revise the FastFSM documentation to incorporate the new fluent API as the primary method of defining state machines. This includes updating the README.md and any other docs or example projects:

- Provide **side-by-side examples**: Show fluent (recommended) vs attribute (legacy) approaches
- Update code snippets using fluent syntax
- Include hierarchical state examples with fluent API
- Document payload and internal transition definitions
- Emphasize **no breaking changes** - existing attribute code continues to work

## 8. Release Preparation and Version 0.7.5 Rollout

Finalize the update for release:
- Bump version to 0.7.5
- Compile changelog highlighting Fluent API as major feature
- Note Fluent API as recommended approach, attributes still supported
- Ensure all tests pass in CI pipeline
- Publish NuGet package with new components

## Payload Support in FluentAPI

### Overview
FluentAPI fully supports payload functionality equivalent to attribute-based definitions:

#### Setting Payload Types
1. **Default Payload** - Set in `[StateMachine]` attribute, applies to all transitions
2. **Per-Transition Payload** - Use `.Payload<T>()` on specific transitions
3. **Mixed Approach** - Default for most, override with `.Payload<T>()` where needed

#### Key Rules
- **OnEntry** callbacks receive payload from the triggering transition
- **OnExit** callbacks never receive payload (by design)
- **Guards and Actions** can have payload-aware signatures
- **Method Overloading** is supported - generator picks correct overload
- **CancellationToken** can be combined with payload parameters
- **Internal Transitions** support payload via `.OnInternal().Payload<T>()`

#### Syntax Order
The correct order for FluentAPI calls is:
```csharp
.On(Trigger)
    .Payload<PayloadType>()    // Optional: specify payload type
    .Guard(nameof(GuardMethod)) // Optional: guard condition
    .Action(nameof(ActionMethod)) // Optional: action to execute
    .GoTo(TargetState)         // Required: target state (or .Internal())
```

#### Usage Examples
```csharp
// With default payload (set in StateMachine attribute)
.On(Trigger.Submit).Guard(nameof(Validate)).Action(nameof(Process)).GoTo(State.Next)

// With specific payload type
.On(Trigger.Submit).Payload<OrderData>().Guard(nameof(ValidateOrder)).GoTo(State.Processing)

// Internal transition with payload
.OnInternal(Trigger.Update).Payload<UpdateData>().Action(nameof(ApplyUpdate)).Internal()

// Async with payload and CancellationToken
.On(Trigger.Start).Payload<Config>().GuardAsync(nameof(CanStartAsync)).ActionAsync(nameof(StartAsync)).GoTo(State.Running)
```

## Success Criteria

The Fluent API implementation is complete when:
1. **All test machines in ParserComparison.Tests produce identical JSON models** from both parsers
2. Generated code is functionally equivalent regardless of definition style
3. All existing attribute-based tests continue to pass
4. Documentation is updated with fluent examples
5. No breaking changes to existing functionality
6. **Payload support is fully functional** with both default and per-transition types