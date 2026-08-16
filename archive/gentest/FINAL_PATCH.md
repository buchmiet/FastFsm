# Final GenTest Parser Patch - Complete Solution

## Problem Summary
GenTest creates minimal Roslyn compilations where:
1. `fsmAttribute.ConstructorArguments` returns empty even for valid attributes
2. Type symbols can't be resolved from the compilation
3. This causes FSM004 warnings and parser validation failures

## Solution Overview
Implement a three-tier fallback system:
1. Try normal semantic model (works in full compilations)
2. Use syntax-based counting and type extraction (for GenTest)
3. Accept type names without full resolution for GenTest context

## Implementation Status

### ✅ Successfully Fixed
- FSM004 warning no longer appears
- Argument counting works via syntax fallback
- Type names extracted from syntax

### ❌ Remaining Issue
- Type symbols still can't be fully resolved in minimal compilations
- Parser still fails validation due to enum type check

## Next Step: Lenient Mode for GenTest

The parser needs to recognize when it's in a limited compilation context and be more lenient. Since GenTest is a testing tool, we can:

1. Skip the enum type validation when types can't be resolved
2. Use type names from syntax as-is
3. Generate code based on syntax information alone

## Recommended Approach

Instead of trying to resolve types that don't exist in the compilation yet, we should:

1. Detect GenTest context (when ConstructorArguments are empty but syntax has arguments)
2. Set a flag indicating "limited compilation mode"
3. Skip strict type validations in this mode
4. Use enum member names from syntax directly

## Alternative Solution

Modify GenTest to:
1. Parse the test file first
2. Extract enum definitions
3. Create temporary compilation with these types
4. Then run the generator

This would provide full type information to the generator.

## Current State

The patch successfully:
- Eliminates FSM004 warning
- Counts arguments correctly
- Extracts type names

But still needs:
- Way to bypass enum type validation
- OR way to provide enum types to compilation

## Testing Results

```
Before patch:
warning FSM004: The [StateMachine] attribute requires at least two arguments

After patch:
info FSM997: Skipped state machine candidate: Parser validation failed
(No more FSM004!)
```

## Conclusion

The syntax fallback approach works for eliminating false FSM004 warnings. To fully enable code generation in GenTest, we need either:

1. **Parser modification**: Add a "lenient mode" that skips type validation when in limited compilation context
2. **GenTest enhancement**: Pre-parse and add type definitions to compilation
3. **Generator feature**: Add a special mode for testing that uses syntax-only information

The first option is simplest and most backward-compatible.