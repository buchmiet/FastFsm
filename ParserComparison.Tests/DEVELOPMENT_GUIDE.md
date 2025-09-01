# FluentParser Development Guide for LLM Agents

## Quick Start for LLM Agents

### Your Mission
Implement the FluentParser to parse state machine definitions and produce the same model as StateMachineParser.

### Essential Commands
```bash
# 1. See what needs to be parsed
cat SimpleStateMachine.cs

# 2. Check current parser state
cat ../Generator/Parsers/FluentParser.cs

# 3. Edit the parser
# Modify ../Generator/Parsers/FluentParser.cs

# 4. Build and check results
dotnet build

# 5. Compare JSON outputs
tail -200 Generated/Generator/Generator.StateMachineGenerator/global__ParserComparison.Tests.SimpleStateMachine.Generated.cs | grep -A 50 "PARSER COMPARISON"
```

## Step-by-Step Implementation

### Step 1: Understand the Target Model
The StateMachineModel you need to create has this structure:
```json
{
  "ClassName": "SimpleStateMachine",
  "Namespace": "ParserComparison.Tests", 
  "StateType": "ParserComparison.Tests.State",
  "TriggerType": "ParserComparison.Tests.Trigger",
  "States": {
    "Idle": { "Name": "Idle", "OrdinalValue": 0, ... },
    "Processing": { "Name": "Processing", "OrdinalValue": 1, ... },
    "Completed": { "Name": "Completed", "OrdinalValue": 2, ... }
  },
  "Transitions": [
    {
      "SourceState": "Idle",
      "Trigger": "Start", 
      "TargetState": "Processing",
      "ActionMethod": "OnStart",
      "IsInternal": false
    },
    // ... more transitions
  ]
}
```

### Step 2: Parse StateMachine Attribute
```csharp
// In FluentParser.TryParse method:

// Find [StateMachine(typeof(State), typeof(Trigger))] attribute
var stateMachineAttr = classDeclaration.AttributeLists
    .SelectMany(al => al.Attributes)
    .FirstOrDefault(a => a.Name.ToString().Contains("StateMachine"));

if (stateMachineAttr?.ArgumentList?.Arguments.Count >= 2)
{
    // Extract State and Trigger type names
    var stateTypeArg = stateMachineAttr.ArgumentList.Arguments[0];
    var triggerTypeArg = stateMachineAttr.ArgumentList.Arguments[1];
    
    // Parse typeof(State) -> "ParserComparison.Tests.State"
    // You'll need to handle the typeof() expression
}
```

### Step 3: Parse State Methods
```csharp
// Find all methods with [State(State.XXX)] attribute
var states = new Dictionary<string, StateModel>();

foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
{
    var stateAttr = method.AttributeLists
        .SelectMany(al => al.Attributes)
        .FirstOrDefault(a => a.Name.ToString().Contains("State"));
        
    if (stateAttr != null)
    {
        // Extract state enum value from attribute
        // [State(State.Idle)] -> "Idle"
        var stateValue = ExtractEnumValue(stateAttr);
        
        states[stateValue] = new StateModel
        {
            Name = stateValue,
            OrdinalValue = states.Count,
            // ... other properties
        };
    }
}
```

### Step 4: Parse Transitions
```csharp
var transitions = new List<TransitionModel>();

foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
{
    // Check for [Transition] attribute
    var transitionAttr = method.AttributeLists
        .SelectMany(al => al.Attributes)
        .FirstOrDefault(a => a.Name.ToString() == "Transition");
        
    if (transitionAttr?.ArgumentList?.Arguments.Count >= 3)
    {
        // [Transition(State.Idle, Trigger.Start, State.Processing)]
        var sourceState = ExtractEnumValue(transitionAttr.ArgumentList.Arguments[0]);
        var trigger = ExtractEnumValue(transitionAttr.ArgumentList.Arguments[1]);
        var targetState = ExtractEnumValue(transitionAttr.ArgumentList.Arguments[2]);
        
        transitions.Add(new TransitionModel
        {
            SourceState = sourceState,
            Trigger = trigger,
            TargetState = targetState,
            ActionMethod = method.Identifier.Text,
            IsInternal = false
        });
    }
    
    // Check for [InternalTransition] attribute
    var internalAttr = method.AttributeLists
        .SelectMany(al => al.Attributes)
        .FirstOrDefault(a => a.Name.ToString().Contains("InternalTransition"));
        
    if (internalAttr != null)
    {
        // Parse similarly but set IsInternal = true
    }
}
```

### Step 5: Helper Methods You'll Need
```csharp
private string ExtractEnumValue(AttributeArgumentSyntax arg)
{
    // Handle: State.Idle -> "Idle"
    // Handle: nameof(OnProcess) -> "OnProcess"
    var expression = arg.Expression.ToString();
    if (expression.Contains("."))
    {
        return expression.Split('.').Last();
    }
    if (expression.StartsWith("nameof("))
    {
        return expression.Replace("nameof(", "").Replace(")", "");
    }
    return expression;
}

private string GetFullTypeName(AttributeArgumentSyntax typeofArg)
{
    // Handle: typeof(State) -> "ParserComparison.Tests.State"
    // For now, hardcode the namespace
    var typeName = typeofArg.Expression.ToString()
        .Replace("typeof(", "")
        .Replace(")", "");
    return $"ParserComparison.Tests.{typeName}";
}
```

## Testing Your Implementation

### Validation Points
1. **States Count**: Should find 3 states (Idle, Processing, Completed)
2. **Transitions Count**: Should find 4 transitions
3. **Internal Transition**: OnProcess should have `IsInternal = true`
4. **Action Methods**: Each transition should have correct method name

### Expected JSON Structure
Your FluentParser should produce JSON matching this structure:
```json
{
  "Transitions": [
    {
      "SourceState": "Idle",
      "Trigger": "Start",
      "TargetState": "Processing",
      "ActionMethod": "OnStart",
      "IsInternal": false
    },
    {
      "SourceState": "Processing", 
      "Trigger": "Process",
      "TargetState": "Processing",
      "ActionMethod": "OnProcess",
      "IsInternal": true
    }
    // ... more
  ]
}
```

## Common Issues and Solutions

### Issue: Can't resolve type names
**Solution**: For this test, hardcode namespace as "ParserComparison.Tests"

### Issue: Enum values not extracted correctly
**Solution**: Use string manipulation on Expression.ToString()

### Issue: Missing using statements
**Add to FluentParser.cs**:
```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
```

## Success Verification
```bash
# Build and check for differences
dotnet build
tail -300 Generated/**/*.Generated.cs | grep -A 100 "PARSER COMPARISON"

# Success = both JSON models have same structure
# (FluentParser model should no longer be empty)
```

## Remember
- Start simple - just get states and transitions working
- Use report?.Invoke() for debugging output
- Check StateMachineParser.cs for reference (but don't copy all its complexity)
- Focus on SimpleStateMachine.cs requirements only