# Generator Parser Patch - Syntax Fallback for GenTest Compatibility

## Problem
GenTest creates minimal Roslyn compilations where `fsmAttribute.ConstructorArguments` returns empty even for valid `[StateMachine(typeof(State), typeof(Trigger))]` attributes, causing:
- FSM004 false positives ("requires at least two arguments")
- Parser validation failures
- No code generation

## Solution
Add syntax-based fallback when semantic model fails to resolve attribute arguments.

## Patch for StateMachineParser.cs

### Step 1: Add Helper Methods (add these private methods to the class)

```csharp
// ——— Syntax Fallback Helpers ———
private static AttributeSyntax? FindStateMachineAttributeSyntax(ClassDeclarationSyntax classDecl)
    => classDecl.AttributeLists
        .SelectMany(al => al.Attributes)
        .FirstOrDefault(attr =>
        {
            var n = attr.Name.ToString();
            return n == "StateMachine"
                || n == "StateMachineAttribute"
                || n.EndsWith(".StateMachine")
                || n.EndsWith(".StateMachineAttribute");
        });

private static int GetCtorArgCountFromSyntax(AttributeSyntax? attrSyntax)
    => attrSyntax?.ArgumentList?.Arguments.Count ?? 0;

// Extract type names from typeof(...) in attribute: [StateMachine(typeof(X), typeof(Y))]
private static (string? stateTypeName, string? triggerTypeName) TryGetTypesFromSyntax(AttributeSyntax? attrSyntax)
{
    if (attrSyntax?.ArgumentList is null || attrSyntax.ArgumentList.Arguments.Count < 2)
        return (null, null);

    static string? ExtractTypeName(AttributeArgumentSyntax arg)
    {
        // Expecting: typeof(X) -> TypeOfExpressionSyntax
        if (arg.Expression is TypeOfExpressionSyntax tof && tof.Type is TypeSyntax ts)
            return ts.ToString(); // raw name; will resolve semantically later if possible
        return null;
    }

    var a0 = attrSyntax.ArgumentList.Arguments[0];
    var a1 = attrSyntax.ArgumentList.Arguments[1];
    return (ExtractTypeName(a0), ExtractTypeName(a1));
}
```

### Step 2: Fix Section 5 - Constructor Argument Count

**FIND** (around line 290):
```csharp
// Get constructor argument count early (needed in multiple places)
var ctorArgCount = fsmAttribute?.ConstructorArguments.Length ?? 0;
```

**REPLACE WITH**:
```csharp
// Get constructor argument count early (needed in multiple places)
var ctorArgCount = fsmAttribute?.ConstructorArguments.Length ?? 0;

// ── Syntax fallback: if SemanticModel returned 0, count arguments from Syntax
if (ctorArgCount == 0)
{
    var attrSyntaxFallback = FindStateMachineAttributeSyntax(classDeclaration);
    var syntaxCount = GetCtorArgCountFromSyntax(attrSyntaxFallback);
    if (syntaxCount >= 2)
    {
        ctorArgCount = syntaxCount;
        report?.Invoke($"[Fallback] Using syntax arg count = {syntaxCount}");
    }
}
```

### Step 3: Fix Section 6 - Type Resolution

**FIND** (around line 380-390):
```csharp
var stateTypeArg = fsmAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
var triggerTypeArg = fsmAttribute.ConstructorArguments[1].Value as INamedTypeSymbol;
```

**REPLACE WITH**:
```csharp
INamedTypeSymbol? stateTypeArg = null;
INamedTypeSymbol? triggerTypeArg = null;

if (fsmAttribute is not null && fsmAttribute.ConstructorArguments.Length >= 2)
{
    // Normal semantic path
    stateTypeArg = fsmAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
    triggerTypeArg = fsmAttribute.ConstructorArguments[1].Value as INamedTypeSymbol;
}
else
{
    // ── Syntax fallback: try to read type names from typeof(...)
    var attrSyntaxFallback = FindStateMachineAttributeSyntax(classDeclaration);
    var (stateName, triggerName) = TryGetTypesFromSyntax(attrSyntaxFallback);

    if (!string.IsNullOrWhiteSpace(stateName))
    {
        // Use SemanticModel to resolve syntactic name to symbol (if possible)
        var parsed = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseTypeName(stateName);
        var typeInfo = compilation.GetSemanticModel(classDeclaration.SyntaxTree)
            .GetTypeInfo(parsed, context.CancellationToken);
        stateTypeArg = typeInfo.Type as INamedTypeSymbol;
    }
    if (!string.IsNullOrWhiteSpace(triggerName))
    {
        var parsed = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseTypeName(triggerName);
        var typeInfo = compilation.GetSemanticModel(classDeclaration.SyntaxTree)
            .GetTypeInfo(parsed, context.CancellationToken);
        triggerTypeArg = typeInfo.Type as INamedTypeSymbol;
    }

    report?.Invoke($"[Fallback] Types from syntax: state={stateTypeArg?.ToDisplayString() ?? stateName ?? "?"}, " +
                   $"trigger={triggerTypeArg?.ToDisplayString() ?? triggerName ?? "?"}");
}
```

## Why This Works

1. **FSM004 Prevention**: By counting arguments from syntax when semantic model fails, we avoid false FSM004 warnings
2. **Type Resolution**: Even when ConstructorArguments are empty, we can extract type names from syntax and attempt resolution
3. **Graceful Degradation**: If syntax parsing also fails, the existing error handling remains intact

## Testing After Patch

1. Apply patch to `Generator/Parsers/StateMachineParser.cs`
2. Rebuild Generator:
   ```bash
   cd Generator
   dotnet build -c Release
   ```
3. Test with GenTest:
   ```bash
   cd GenTest
   dotnet run -- --in test_hsm.cs --logging --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --out output_final/
   ```

## Expected Results

### Before Patch:
```
warning FSM004: The [StateMachine] attribute requires at least two arguments
info FSM997: Parser validation failed
```

### After Patch:
```
[Fallback] Using syntax arg count = 2
[Fallback] Types from syntax: state=TestHsm.HState, trigger=TestHsm.HTrigger
info FSM996: Successfully generated state machine for TestHsm.HsmMachine
```

## Additional Notes

- GenTest improvements (automatic reference collection) remain valuable for other scenarios
- This patch is backwards compatible - normal compilations continue using semantic model
- The fallback only activates when semantic model fails (GenTest scenario)
- Consider adding integration tests for this edge case

## Related Files
- `Generator/Parsers/StateMachineParser.cs` - Apply patch here
- `GenTest/Program.cs` - Already improved with reference collection
- `GenTest/GENTEST_BUG_REPORT.md` - Full bug analysis
- `GenTest/FIX_STATUS.md` - Implementation status