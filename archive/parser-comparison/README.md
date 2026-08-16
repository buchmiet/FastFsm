# ParserComparison.Tests - Parser Development Infrastructure

## Overview
This project provides infrastructure for developing and comparing different state machine parsers for the FastFsm generator. It allows parallel development of parsers while comparing their output models in JSON format.

## Project Structure
```
ParserComparison.Tests/
├── SimpleStateMachine.cs       # Example state machine for testing
├── ParserTests.cs              # Unit tests
├── README.md                   # This documentation
└── Generated/                  # Generated code with JSON comparisons
    └── Generator/
        └── Generator.StateMachineGenerator/
            └── *.Generated.cs  # Contains JSON models at the end
```

## How It Works

### 1. Parser Infrastructure
The system uses two parsers that implement `IStateMachineParser`:
- **StateMachineParser** - The original, production parser
- **FluentParser** - New parser under development (currently returns empty model)

Both parsers process the same state machine definition and produce a `StateMachineModel`.

### 2. JSON Comparison
After code generation, both models are serialized to JSON and added as comments at the end of the generated file:
```csharp
/*
====== PARSER COMPARISON DEBUG INFO ======
Original StateMachineParser Model:
{ ... full JSON model ... }

FluentParser Model:
{ ... full JSON model ... }
==========================================
*/
```

### 3. Development Workflow

#### For Human Developers:
1. **Modify FluentParser** in `/Generator/Parsers/FluentParser.cs`
2. **Build the project**: `dotnet build`
3. **Check generated JSON** in `Generated/` folder
4. **Compare models** to ensure parity with original parser
5. **Iterate** until FluentParser produces correct model

#### For LLM Agents:
```bash
# 1. Read current parser implementation
cat ../Generator/Parsers/FluentParser.cs

# 2. Understand the model structure
cat ../Generator/Model/StateMachineModel.cs

# 3. Modify FluentParser to parse state machine attributes
# Edit ../Generator/Parsers/FluentParser.cs

# 4. Rebuild and check results
dotnet build
tail -200 Generated/Generator/Generator.StateMachineGenerator/*.Generated.cs

# 5. Compare JSON outputs to verify correctness
```

## Key Files for Parser Development

### Parser Interface
**Location**: `/Generator/Parsers/IStateMachineParser.cs`
```csharp
interface IStateMachineParser
{
    bool TryParse(
        ClassDeclarationSyntax classDeclaration,
        out StateMachineModel? model,
        Action<string>? report = null);
}
```

### Model Structure
**Location**: `/Generator/Model/StateMachineModel.cs`

Key properties to populate:
- `States` - Dictionary of state names to StateModel
- `Transitions` - List of TransitionModel objects
- `StateType` - Fully qualified name of state enum
- `TriggerType` - Fully qualified name of trigger enum
- `ClassName` - Name of the state machine class
- `Namespace` - Namespace of the class

### Example State Machine
**Location**: `SimpleStateMachine.cs`

Features to parse:
- `[StateMachine(typeof(State), typeof(Trigger))]` - Main attribute
- `[State(State.Idle)]` - State definitions
- `[Transition(from, trigger, to)]` - External transitions
- `[InternalTransition(state, trigger, action)]` - Internal transitions

## Testing Your Parser

### Quick Test
```bash
# Clean build and check JSON output
rm -rf bin obj Generated
dotnet build
grep -A 100 "FluentParser Model:" Generated/**/*.Generated.cs
```

### Verification Checklist
- [ ] States are correctly identified
- [ ] Transitions have correct source, trigger, and target
- [ ] Internal transitions are marked with `IsInternal = true`
- [ ] Action methods are linked to transitions
- [ ] State and Trigger types are correctly resolved

## Common Parser Tasks

### 1. Parse State Attributes
```csharp
// Find all methods with [State] attribute
var stateMethods = classDeclaration.Members
    .OfType<MethodDeclarationSyntax>()
    .Where(m => m.AttributeLists
        .SelectMany(al => al.Attributes)
        .Any(a => a.Name.ToString().Contains("State")));
```

### 2. Parse Transition Attributes
```csharp
// Find transition attributes and extract parameters
var transitions = classDeclaration.Members
    .OfType<MethodDeclarationSyntax>()
    .SelectMany(m => m.AttributeLists
        .SelectMany(al => al.Attributes)
        .Where(a => a.Name.ToString().Contains("Transition"))
        .Select(a => ParseTransition(a, m)));
```

### 3. Build Model
```csharp
model = new StateMachineModel
{
    ClassName = classDeclaration.Identifier.Text,
    Namespace = GetNamespace(classDeclaration),
    StateType = "ParserComparison.Tests.State",
    TriggerType = "ParserComparison.Tests.Trigger",
    States = states,
    Transitions = transitions,
    GenerationConfig = new GenerationConfig()
};
```

## Debugging Tips

1. **Use report parameter** for logging:
```csharp
report?.Invoke($"[FluentParser] Found {states.Count} states");
```

2. **Check compilation context** for type resolution:
```csharp
var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
```

3. **Handle edge cases**:
- Nested classes
- Generic types
- Inherited attributes
- Multiple state machines in one file

## Success Criteria
Your FluentParser is complete when:
1. JSON output matches StateMachineParser for all test cases
2. Generated code compiles without errors
3. All transitions and states are correctly identified
4. Tests in ParserTests.cs pass

## Next Steps
After FluentParser works for SimpleStateMachine:
1. Test with more complex state machines
2. Add support for hierarchical states
3. Handle payload types
4. Support async state machines
5. Optimize performance

## Questions?
Check the original parser implementation in `/Generator/Parsers/StateMachineParser.cs` for reference implementation details.