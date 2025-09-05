# Technical Report: OnException Implementation Issue in Fluent API

**Date:** 2025-01-05  
**Author:** Development Team  
**Subject:** Inconsistency in OnException Handler Implementation between Legacy and Fluent API  
**Status:** 🔴 **REQUIRES TEAM DISCUSSION**

---

## Executive Summary

During the implementation of `OnException` support for Fluent API, a critical architectural inconsistency was discovered. The Fluent API parser currently requires the `.OnException()` method to be called on a `StateBuilder` (after `.State()`), while conceptually and in Legacy API, the exception handler is a **global machine-level configuration**, not state-specific.

This document outlines the issue, demonstrates the inconsistency with code examples, and proposes solutions for team discussion.

---

## 1. Problem Statement

### 1.1 Conceptual Model
Exception handlers in FastFSM are **global** - they handle exceptions for the entire state machine, regardless of which state the exception occurs in. This is evident from:
- The `[OnException]` attribute is applied at the **class level** in Legacy API
- There is only **one** exception handler per state machine
- The handler receives `ExceptionContext` with full transition information

### 1.2 Current Implementation Issue
The Fluent API currently requires:
```csharp
// ❌ Current (incorrect) requirement
private static void Configure() => FSM
    .State<State>(State.Initial)
        .OnException(nameof(HandleException))  // Must be after State()
        .On(Trigger.Start).GoTo(State.Next);
```

This suggests the handler is state-specific, which is **architecturally incorrect**.

---

## 2. Code Comparison: Legacy vs Fluent

### 2.1 Legacy API (Attribute-based) - CORRECT ✅

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
[OnException(nameof(HandleException))]  // ✅ Class-level, global handler
public partial class LegacyMachine
{
    [Transition(State.A, Trigger.Go, State.B, Action = nameof(DoWork))]
    private void Configure() { }
    
    private void DoWork() => throw new InvalidOperationException("boom");
    
    private ExceptionDirective HandleException(ExceptionContext<State, Trigger> ctx)
        => ExceptionDirective.Continue;
}
```

### 2.2 Fluent API - Expected Design 🎯

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class FluentMachine
{
    // ✅ EXPECTED: Global handler definition
    private static void Configure() => FSM
        .OnException<State>(nameof(HandleException))  // Global, before states
        .State(State.A)
            .On(Trigger.Go).Action(nameof(DoWork)).GoTo(State.B)
        .State(State.B);
    
    private void DoWork() => throw new InvalidOperationException("boom");
    
    private ExceptionDirective HandleException(ExceptionContext<State, Trigger> ctx)
        => ExceptionDirective.Continue;
}
```

### 2.3 Fluent API - Current Requirement ❌

```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class FluentMachine
{
    // ❌ CURRENT: Handler tied to first state definition
    private static void Configure() => FSM
        .State<State>(State.A)
            .OnException(nameof(HandleException))  // Confusing - seems state-specific
            .On(Trigger.Go).Action(nameof(DoWork)).GoTo(State.B)
        .State(State.B);
    
    // Same handler implementation...
}
```

---

## 3. Parser Model Comparison

### 3.1 Legacy Parser Output (StateMachineParser)

When parsing the Legacy API machine with `[OnException]` attribute:

```json
{
  "ClassName": "LegacyMachine",
  "StateType": "State",
  "TriggerType": "Trigger",
  "ExceptionHandler": {
    "MethodName": "HandleException",
    "IsAsync": false,
    "AcceptsCancellationToken": false,
    "ExceptionContextClosedType": "global::FastFsm.Exceptions.ExceptionContext<State, Trigger>"
  },
  "Transitions": [
    {
      "FromState": "A",
      "ToState": "B",
      "Trigger": "Go",
      "ActionMethod": "DoWork"
    }
  ],
  "States": {
    "A": { "Name": "A" },
    "B": { "Name": "B" }
  }
}
```

**Key Point:** `ExceptionHandler` is a top-level property of the model, not associated with any state.

### 3.2 Fluent Parser Output (Current Issue)

When parsing with current Fluent API implementation:

```json
{
  "ClassName": "FluentMachine",
  "StateType": "State",
  "TriggerType": "Trigger",
  "ExceptionHandler": null,  // ❌ NOT FOUND!
  "Transitions": [
    {
      "FromState": "A",
      "ToState": "B",
      "Trigger": "Go",
      "ActionMethod": "DoWork"
    }
  ],
  "States": {
    "A": { "Name": "A" },
    "B": { "Name": "B" }
  }
}
```

**Problem:** The parser doesn't recognize `.OnException()` when called on `StateBuilder`.

---

## 4. Generated Code Comparison

### 4.1 Legacy API - Generated Code (WITH Handler)

```csharp
protected override bool TryFireInternal(Trigger trigger, object? payload) {
    switch (_currentState) {
        case State.A: {
            switch (trigger) {
                case Trigger.Go: {
                    var prevState = _currentState;  // Store for exception context
                    _currentState = State.B;
                    try {
                        DoWork();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) {
                        // ✅ Exception handler invoked
                        var exceptionContext = new ExceptionContext<State, Trigger>(
                            prevState, State.B, Trigger.Go, ex, 
                            TransitionStage.Action, stateAlreadyChanged: true);
                        
                        var directive = HandleException(exceptionContext);
                        
                        if (directive != ExceptionDirective.Continue)
                            throw;
                        // Exception swallowed by Continue directive
                    }
                    return true;
                }
            }
        }
    }
    return false;
}
```

### 4.2 Fluent API - Generated Code (WITHOUT Handler)

```csharp
protected override bool TryFireInternal(Trigger trigger, object? payload) {
    switch (_currentState) {
        case State.A: {
            switch (trigger) {
                case Trigger.Go: {
                    // No prevState stored - no handler
                    _currentState = State.B;
                    #if FASTFSM_SAFE_ACTIONS
                    try {
                        DoWork();
                    }
                    catch (Exception ex) {
                        return false;  // ❌ Just swallow, no handler call
                    }
                    #else
                    DoWork();  // ❌ No exception handling at all
                    #endif
                    return true;
                }
            }
        }
    }
    return false;
}
```

---

## 5. Technical Analysis

### 5.1 Why Current Implementation Fails

1. **Parser Logic Issue**: `ParseOnException` in `FluentParser.cs` is called when processing method invocations on `StateBuilder`, but the model's `ExceptionHandler` property is set at the machine level.

2. **Conceptual Mismatch**: Having `.OnException()` on `StateBuilder` suggests you could have different handlers for different states, which is not supported by the architecture.

3. **Chaining Confusion**: The current approach requires remembering to call `.OnException()` on the first state, which is non-intuitive.

### 5.2 Parser Code Issue

In `FluentParser.cs`, the `ParseFluentChain` method processes the chain sequentially:

```csharp
case "OnException":
    report?.Invoke($"[FluentParser] Processing OnException");
    ParseOnException(invocation, model, report);
    break;
```

This is called when processing state methods, but `FSM.OnException<TState>()` at the beginning of the chain is not properly handled.

---

## 6. Proposed Solutions

### Solution A: Fix FSM.OnException Parsing (Recommended) ✅

Make `FSM.OnException<TState>()` work as the first call in the chain:

```csharp
private static void Configure() => FSM
    .OnException<State>(nameof(HandleException))  // Global, clear intent
    .State(State.A)
        .On(Trigger.Go).Action(nameof(DoWork)).GoTo(State.B);
```

**Implementation:**
- Modify `FluentParser` to handle `OnException` when called directly on `FSM`
- Remove `OnException` from `StateBuilder` to avoid confusion

### Solution B: New Initialization Pattern

Introduce a dedicated configuration method:

```csharp
private static void Configure() => FSM
    .WithConfiguration(cfg => cfg
        .OnException(nameof(HandleException))
        .ContinueOnCapturedContext(false))
    .State(State.A)
        .On(Trigger.Go).Action(nameof(DoWork)).GoTo(State.B);
```

### Solution C: Keep Current, Document Clearly (Not Recommended) ⚠️

Keep the current implementation but clearly document that:
- `.OnException()` must be called on the first state
- It applies globally despite being on `StateBuilder`
- This is a Fluent API limitation

---

## 7. Impact Assessment

### 7.1 Breaking Changes
- **Solution A**: Minor - requires updating any existing Fluent API code using OnException
- **Solution B**: None - additive change
- **Solution C**: None - documentation only

### 7.2 Testing Requirements
- All exception handling tests need to be verified
- Comparison tests between Legacy and Fluent must pass
- Generated code must be functionally identical

### 7.3 Documentation Updates
- FluentAPI.md needs updating
- Migration guide may be needed
- Example code in tests should reflect best practices

---

## 8. Recommendations for Team Discussion

1. **Architectural Consistency**: We should maintain that exception handlers are global, machine-level concerns.

2. **API Clarity**: The Fluent API should make it clear that `OnException` is global, not state-specific.

3. **Parser Priority**: Fix the parser to properly handle `FSM.OnException<TState>()` as the preferred pattern.

4. **Timeline**: This should be addressed before the v0.8.0 release to avoid breaking changes later.

---

## 9. Action Items

- [ ] Team discussion on preferred solution
- [ ] Implement chosen solution
- [ ] Update all tests to use correct pattern
- [ ] Update documentation
- [ ] Add migration notes if needed
- [ ] Verify parity between Legacy and Fluent generated code

---

## Appendix: Test Results

### Current Test Status:
- ❌ `ExceptionDirective_Continue_Action_Tests_Fluent` - Handler not found
- ❌ `ExceptionDirective_Propagate_Action_Tests_Fluent` - Handler not found  
- ❌ `ExceptionDirective_Cancellation_Tests_Fluent` - Handler not found
- ❌ `ExceptionDirective_Comparison_Tests` - Parity check fails

### Expected After Fix:
- ✅ All tests passing
- ✅ Generated code identical between Legacy and Fluent
- ✅ Clear, intuitive API

---

**End of Report**

*Please review and provide feedback for team discussion.*