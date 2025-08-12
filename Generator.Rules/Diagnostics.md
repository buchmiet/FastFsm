# FastFSM Diagnostics System - Developer Guide & Tutorial

## Introduction

Welcome to the FastFSM Diagnostics System guide! This comprehensive tutorial will help you understand, use, and extend the diagnostic capabilities of the FastFSM source generator. Whether you're a user trying to understand error messages or a developer looking to add new validations, this guide has you covered.

## Table of Contents

1. [Overview](#overview)
2. [Architecture & Design](#architecture--design)
3. [Working with Diagnostics](#working-with-diagnostics)
4. [Complete Diagnostic Reference](#complete-diagnostic-reference)
5. [Adding New Diagnostics](#adding-new-diagnostics)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Overview

The FastFSM diagnostic system is a compile-time validation framework that helps developers catch errors early in the development process. With **35 diagnostic codes** covering everything from basic state machine configuration to complex hierarchical state machines, the system ensures your state machines are correct before they run.

### Why Diagnostics Matter

Consider this problematic state machine:

```csharp
[StateMachine(typeof(States), typeof(Triggers))]
public partial class DoorController
{
    [Transition(States.Open, Triggers.Close, States.Closed)]
    [Transition(States.Open, Triggers.Close, States.Locked)] // Duplicate!
    private void Configure() { }
}
```

Without diagnostics, this would compile but fail at runtime. With FSM001 (DuplicateTransition), you get an immediate compile-time error pointing to the exact problem.

### Current Coverage

- **Total Diagnostics:** 35 (FSM001-FSM014, FSM100-FSM105, FSM981-FSM999)
- **Implementation Rate:** 94% (33 of 35 implemented)
- **Categories:** Core validation, async patterns, payload handling, hierarchical states

---

## Architecture & Design

### The Validation Pipeline

The diagnostic system follows a clean, extensible architecture:

```
User Code → Parser → Rule Engine → Diagnostic Emission → IDE/Compiler
```

Let's trace through each step:

#### 1. **Rule Definition** (`RuleDefinition.cs`)
Each diagnostic starts as a rule definition:

```csharp
public static readonly RuleDefinition DuplicateTransition = new(
    id: "FSM001",
    title: "Duplicate transition detected",
    messageFormat: "State '{0}' already has a transition on trigger '{1}'",
    category: RuleCategories.FSM_Generator,
    defaultSeverity: RuleSeverity.Warning,
    description: "Each state can only have one transition per trigger"
);
```

#### 2. **Context Classes** (`Contexts/`)
Contexts carry validation data:

```csharp
public class DuplicateTransitionContext(
    string fromState,
    string trigger,
    string firstTarget,
    string duplicateTarget)
{
    public string FromState { get; } = fromState;
    public string Trigger { get; } = trigger;
    public string FirstTarget { get; } = firstTarget;
    public string DuplicateTarget { get; } = duplicateTarget;
}
```

#### 3. **Rule Implementation** (`Rules/`)
Rules contain the validation logic:

```csharp
public class DuplicateTransitionRule : IValidationRule<DuplicateTransitionContext>
{
    public IEnumerable<ValidationResult> Validate(DuplicateTransitionContext context)
    {
        if (context.FirstTarget != context.DuplicateTarget)
        {
            yield return ValidationResult.Fail(
                RuleIdentifiers.DuplicateTransition,
                $"Duplicate transition from {context.FromState}",
                RuleSeverity.Warning);
        }
    }
}
```

#### 4. **Parser Integration** (`StateMachineParser.cs`)
The parser instantiates and invokes rules:

```csharp
public class StateMachineParser
{
    private readonly DuplicateTransitionRule _duplicateRule = new();
    
    private void ValidateTransitions()
    {
        var context = new DuplicateTransitionContext(...);
        var results = _duplicateRule.Validate(context);
        ProcessRuleResults(results, location, ref hasErrors);
    }
}
```

---

## Working with Diagnostics

### Understanding Diagnostic Messages

Each diagnostic follows a consistent format:

```
FSM001: Duplicate transition from state 'Open' on trigger 'Close'
       ↑                        ↑
    Error Code              Contextual Information
```

### Severity Levels

- **Error** (🔴): Prevents code generation, must be fixed
- **Warning** (🟡): Potential issues, should be reviewed
- **Info** (🔵): Informational messages for awareness

### Common Scenarios and Solutions

#### Scenario 1: Duplicate Transitions (FSM001)

**Problem:**
```csharp
[Transition(States.Idle, Triggers.Start, States.Running)]
[Transition(States.Idle, Triggers.Start, States.Error)] // FSM001!
```

**Solution:**
```csharp
[Transition(States.Idle, Triggers.Start, States.Running)]
[Transition(States.Idle, Triggers.Error, States.Error)] // Different trigger
```

#### Scenario 2: Async/Sync Mismatch (FSM011)

**Problem:**
```csharp
[State(States.Loading, OnEntry = nameof(LoadData))]
[State(States.Processing, OnEntry = nameof(Process))]

private async Task LoadData() { ... } // Async
private void Process() { ... }        // Sync - FSM011!
```

**Solution:**
```csharp
private async Task LoadData() { ... }
private async Task Process() { ... } // Make all callbacks consistent
```

#### Scenario 3: Invalid Guard Return Type (FSM012)

**Problem:**
```csharp
private async Task<bool> CanProceed() // FSM012: Should be ValueTask<bool>
{
    await Task.Delay(100);
    return true;
}
```

**Solution:**
```csharp
private async ValueTask<bool> CanProceed() // Correct return type
{
    await Task.Delay(100);
    return true;
}
```

---

## Complete Diagnostic Reference

### Core Diagnostics (FSM001-FSM014)

| Code | Name | Severity | Description | Example |
|------|------|----------|-------------|---------|
| **FSM001** | DuplicateTransition | Warning | Multiple transitions from same state on same trigger | `[Transition(A, X, B)]` `[Transition(A, X, C)]` |
| **FSM002** | UnreachableState | Warning | State has no incoming transitions | State defined but never targeted |
| **FSM003** | InvalidMethodSignature | Error | Callback method has wrong signature | Guard returning `void` instead of `bool` |
| **FSM004** | MissingStateMachineAttribute | Warning | Has transitions but no `[StateMachine]` | Missing attribute on class |
| **FSM005** | InvalidTypesInAttribute | Error | State/Trigger types must be enums | Using `int` instead of enum |
| **FSM006** | InvalidEnumValueInTransition | Error | Invalid enum value in transition | `[Transition("Invalid", ...)]` |
| **FSM007** | MissingPayloadType | Error | Forced payload variant needs type | `Force = WithPayload` but no payload type |
| **FSM008** | ConflictingPayloadConfiguration | Error | WithPayload can't have trigger-specific types | Mixing payload patterns |
| **FSM009** | InvalidForcedVariantConfiguration | Error | Forced variant conflicts with usage | `Force = Pure` with callbacks |
| **FSM010** | GuardWithPayloadInNonPayloadMachine | Error | Guard expects payload but none defined | Parameter mismatch |
| **FSM011** | MixedSyncAsyncCallbacks | Error | Can't mix sync and async callbacks | Some `Task`, some `void` |
| **FSM012** | InvalidGuardTaskReturnType | Error | Guards must return `ValueTask<bool>` | Using `Task<bool>` |
| **FSM013** | AsyncCallbackInSyncMachine | Error | Can't add async to sync machine | Breaking consistency |
| **FSM014** | InvalidAsyncVoid | Warning | Avoid `async void` callbacks | Use `async Task` instead |

### Hierarchical State Machine Diagnostics (FSM100-FSM105)

| Code | Name | Severity | Description | Example |
|------|------|----------|-------------|---------|
| **FSM100** | CircularHierarchy | Error | Circular parent-child relationships | A→B→C→A |
| **FSM101** | OrphanSubstate | Error | References non-existent parent | `Parent = "NoSuchState"` |
| **FSM102** | InvalidHierarchyConfiguration | Error | Composite needs initial substate | Parent with no initial child |
| **FSM103** | MultipleInitialSubstates | Error | Only one initial substate allowed | Two `IsInitial = true` |
| **FSM104** | InvalidHistoryConfiguration | Warning | History only for composite states | History on leaf state |
| **FSM105** | ConflictingTransitionTargets | Info | Implicit transition to composite | Enters via initial/history |

### Information Diagnostics (FSM981-FSM999)

| Code | Name | Description |
|------|------|-------------|
| **FSM981** | NoTransitions | No transitions detected in state machine |
| **FSM982** | InternalOnlyMachine | State machine has only internal transitions |
| **FSM989** | ConfigurationSummary | Summary of configuration sections found |
| **FSM999** | ParserCriticalError | Critical error during parsing |

---

## Adding New Diagnostics

### Step-by-Step Guide

Let's add a new diagnostic that warns about states with too many transitions:

#### Step 1: Define the Rule

Add to `RuleDefinition.cs`:

```csharp
public static readonly RuleDefinition TooManyTransitions = new(
    id: "FSM015",
    title: "State has too many transitions",
    messageFormat: "State '{0}' has {1} transitions (recommended maximum: {2})",
    category: RuleCategories.FSM_Generator,
    defaultSeverity: RuleSeverity.Warning,
    description: "States with many transitions may indicate design issues"
);
```

#### Step 2: Create the Context

Create `TooManyTransitionsContext.cs`:

```csharp
namespace Generator.Rules.Contexts;

public class TooManyTransitionsContext(
    string stateName,
    int transitionCount,
    int recommendedMax = 5)
{
    public string StateName { get; } = stateName;
    public int TransitionCount { get; } = transitionCount;
    public int RecommendedMax { get; } = recommendedMax;
}
```

#### Step 3: Implement the Rule

Create `TooManyTransitionsRule.cs`:

```csharp
namespace Generator.Rules.Rules;

public class TooManyTransitionsRule : IValidationRule<TooManyTransitionsContext>
{
    public IEnumerable<ValidationResult> Validate(TooManyTransitionsContext context)
    {
        if (context.TransitionCount > context.RecommendedMax)
        {
            var message = string.Format(
                DefinedRules.TooManyTransitions.MessageFormat,
                context.StateName,
                context.TransitionCount,
                context.RecommendedMax);

            yield return ValidationResult.Fail(
                "FSM015",
                message,
                RuleSeverity.Warning);
        }
        else
        {
            yield return ValidationResult.Success();
        }
    }
}
```

#### Step 4: Integrate with Parser

In `StateMachineParser.cs`:

```csharp
// Add field
private readonly TooManyTransitionsRule _tooManyTransitionsRule = new();

// In validation method
var transitionsByState = model.Transitions.GroupBy(t => t.FromState);
foreach (var group in transitionsByState)
{
    var context = new TooManyTransitionsContext(
        group.Key,
        group.Count());
    
    var results = _tooManyTransitionsRule.Validate(context);
    ProcessRuleResults(results, location, ref errors);
}
```

#### Step 5: Add Tests

Create test in `DiagnosticTests.cs`:

```csharp
[Fact]
public void FSM015_TooManyTransitions_ShouldEmit()
{
    const string sourceCode = @"
        [StateMachine(typeof(States), typeof(Triggers))]
        public partial class Machine {
            [Transition(States.A, Triggers.T1, States.B)]
            [Transition(States.A, Triggers.T2, States.B)]
            [Transition(States.A, Triggers.T3, States.B)]
            [Transition(States.A, Triggers.T4, States.B)]
            [Transition(States.A, Triggers.T5, States.B)]
            [Transition(States.A, Triggers.T6, States.B)] // 6th transition
            private void Config() { }
        }";

    var diags = CompileAndRunGenerator(sourceCode);
    Assert.Contains(diags, d => d.Id == "FSM015");
}
```

---

## Best Practices

### For Users

1. **Fix Errors First**: Errors prevent generation, warnings don't
2. **Read the Full Message**: Context often suggests the solution
3. **Check Related Diagnostics**: One issue may trigger multiple diagnostics
4. **Use Consistent Patterns**: All async or all sync, not mixed

### For Developers

1. **Follow the Pattern**: Definition → Context → Rule → Integration → Test
2. **Choose Appropriate Severity**: 
   - Error: Prevents correct generation
   - Warning: Works but potentially problematic
   - Info: FYI only
3. **Provide Clear Messages**: Include what's wrong AND how to fix it
4. **Test Edge Cases**: Empty states, null values, extreme counts
5. **Document Thoroughly**: Examples are worth a thousand words

### Performance Considerations

- Rules are instantiated once per parser instance
- Contexts should be lightweight (no heavy computations)
- Validation should be O(1) or O(n) where possible
- Cache expensive computations in the parser

---

## Troubleshooting

### Common Issues

#### "Diagnostic not appearing"

Check:
1. Rule is instantiated in parser constructor
2. Rule is invoked at appropriate validation point
3. Context is properly populated
4. ProcessRuleResults is called with results

#### "Wrong severity level"

Check:
1. RuleDefinition defaultSeverity
2. ValidationResult.Fail severity parameter
3. ProcessRuleResults severity mapping

#### "Performance issues"

Check:
1. Rules aren't doing expensive computations
2. Not creating unnecessary object allocations
3. Using appropriate data structures

### Debug Tips

Enable diagnostic reporting in parser:

```csharp
Action<string> report = msg => context.ReportDiagnostic(
    Diagnostic.Create(debugDescriptor, Location.None, msg));
```

Add context inspection:

```csharp
report?.Invoke($"Validating {context.StateName} with {context.TransitionCount} transitions");
```

---

## Implementation Status Summary

### Fully Implemented (94%)
- ✅ All core diagnostics (FSM001-FSM014)
- ✅ All HSM diagnostics (FSM100-FSM105)
- ✅ Most info diagnostics (FSM981-FSM999)

### Architecture Strengths
- **Modular**: Each diagnostic is independent
- **Testable**: Clear separation of concerns
- **Extensible**: Easy to add new rules
- **Performant**: Minimal overhead during generation

### Future Enhancements
- Performance diagnostics (state count, transition complexity)
- Style suggestions (naming conventions, organization)
- Advanced HSM validations (deep history conflicts)
- Cross-state machine validations

---

## Conclusion

The FastFSM diagnostic system represents a mature, well-architected approach to compile-time validation. With 94% implementation coverage and a clean, extensible design, it provides both users and developers with the tools they need to create robust state machines.

Whether you're debugging a complex hierarchical state machine or adding new validation rules, this system has been designed to make your work easier and your code more reliable.

### Quick Reference Links

- **Source Code**: `/Generator.Rules/`
- **Parser Integration**: `/Generator/Parsers/StateMachineParser.cs`
- **Tests**: `/Generator.Tests/*DiagnosticTests.cs`
- **Rule Definitions**: `/Generator.Rules/Definitions/RuleDefinition.cs`

---

*Last Updated: January 2025 | Version: 3.0 | Coverage: 94%*