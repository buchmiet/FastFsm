# GenTest - FastFSM Source Generator Testing Tool

## Overview

GenTest is a command-line tool designed for rapid testing and development of Roslyn source generators, specifically optimized for the FastFSM state machine generator. It enables hot-reload testing of generators without requiring full Visual Studio compilation contexts.

## Key Features

- **Hot-reload support**: Automatically reloads generator assemblies when files change
- **Minimal compilation context**: Tests generators with only essential references
- **Lenient mode**: Continues code generation even with incomplete type information
- **Real-time feedback**: Immediate output of generated code and diagnostics
- **LLM-friendly**: Designed for use by both humans and AI agents

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- FastFSM Generator assemblies built in Release mode

### Build

```bash
cd GenTest
dotnet build -c Release
```

## Usage

### Basic Command Structure

```bash
dotnet run -- --in <input_file> --generator <generator_dll> --out <output_dir>
```

### Parameters

| Parameter | Required | Description | Example |
|-----------|----------|-------------|---------|
| `--in` | Yes | Input C# source file to process | `test.cs` |
| `--generator` | Yes | Path to generator DLL | `../Generator/bin/Release/netstandard2.0/Generator.dll` |
| `--out` | No | Output directory for generated files | `output/` |
| `--logging` | No | Enable logging in generated code | (flag only) |
| `--verbose` | No | Enable verbose output | (flag only) |
| `--watch` | No | Watch mode with hot-reload | (flag only) |
| `--di` | No | Enable dependency injection | (flag only) |

### Examples

#### Simple Test
```bash
dotnet run -- --in machine.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll
```

#### With Output Directory and Logging
```bash
dotnet run -- --in machine.cs --logging --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --out output/
```

#### Watch Mode (Hot-Reload)
```bash
dotnet run -- --in machine.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --watch
```

## Use Cases

### 1. Rapid Generator Development

**Scenario**: Developing new generator features without full IDE compilation.

```bash
# Edit generator code
cd ../Generator
dotnet build -c Release

# Test immediately
cd ../GenTest
dotnet run -- --in test_case.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll
```

### 2. Automated Testing by LLM Agents

**Scenario**: AI agents testing generator modifications.

```bash
# Agent creates test file
echo '[StateMachine(typeof(State), typeof(Trigger))]
public partial class TestMachine {}
public enum State { A, B }
public enum Trigger { Next }' > test.cs

# Agent runs test
dotnet run -- --in test.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --out output/

# Agent checks output
ls output/*.cs
cat output/diagnostics.txt
```

### 3. Debugging Generator Issues

**Scenario**: Investigating why certain attributes aren't processed correctly.

```bash
# Run with verbose diagnostics
dotnet run -- --in problematic.cs --verbose --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --out debug_output/

# Check all diagnostics
cat debug_output/diagnostics.txt
```

### 4. Continuous Development with Watch Mode

**Scenario**: Iterative development with automatic regeneration.

```bash
# Start watch mode
dotnet run -- --in developing.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --watch

# In another terminal, rebuild generator after changes
cd ../Generator && dotnet build -c Release

# GenTest automatically reloads and regenerates
```

## Output Structure

GenTest creates the following files in the output directory:

| File | Description | Always Generated |
|------|-------------|------------------|
| `diagnostics.txt` | All diagnostic messages from the generator | Yes |
| `__FastFsm.DiscoveredMachines.g.cs` | Discovery dump of found state machines | Yes |
| `global__<MachineName>.Generated.cs` | Main generated state machine code | When successful |
| `<MachineName>.Log.g.cs` | Logging infrastructure (if --logging enabled) | When logging enabled |

## Lenient Mode

### What is Lenient Mode?

Lenient mode automatically activates when GenTest detects a minimal compilation context where type symbols cannot be fully resolved. This is the default behavior when testing with GenTest.

### Capabilities in Lenient Mode

✅ **Can Process:**
- `[StateMachine(typeof(State), typeof(Trigger))]` attributes
- Enum definitions for states and triggers
- Basic transition definitions
- State callback configurations
- Method signatures for guards and actions

✅ **Generates:**
- State machine interface definitions
- Base class implementations
- Minimal transition skeletons
- State name arrays for logging

### Limitations in Lenient Mode

❌ **Cannot Fully Process:**
- Complex type resolution (custom payload types)
- Member access expressions in attributes (`State.A` may not resolve)
- Type validation for method parameters
- Generic type arguments
- Types from external assemblies

❌ **Results in:**
- Minimal or no transition implementations
- Missing payload type information
- Incomplete HSM parent-child relationships
- Limited async/await detection

### Diagnostic Indicators

```
info FSM998: Using syntax-only parsing for types in limited compilation context
info FSM996: Parsing completed with errors in lenient mode
info FSM994: Enum-only states fallback applied
```

## Constraints and Limitations

### 1. Compilation Context

- **Constraint**: GenTest creates minimal Roslyn compilation contexts
- **Impact**: Full semantic analysis not available
- **Workaround**: Lenient mode enables partial generation

### 2. Type Resolution

- **Constraint**: Cannot resolve types from external assemblies
- **Impact**: Payload types and custom types may not be recognized
- **Workaround**: Use simple types or accept minimal generation

### 3. Single File Processing

- **Constraint**: Processes only one input file at a time
- **Impact**: Cannot test multi-file state machine definitions
- **Workaround**: Combine definitions into single test file

### 4. Reference Collection

- **Constraint**: Automatically collects DLLs from generator directory only
- **Impact**: May miss required dependencies
- **Workaround**: Ensure all dependencies are in generator output directory

### 5. No Project Context

- **Constraint**: Does not read .csproj or solution files
- **Impact**: Project-level configurations not applied
- **Workaround**: Test individual files independently

## Error Messages and Solutions

### FSM004: Attribute requires at least two arguments

**Status**: Fixed in lenient mode
**Solution**: Automatic - lenient mode handles this

### FSM983: InternalTransition requires Action method

**Cause**: Missing Action parameter in InternalTransition attribute
**Solution**: Add `Action = "MethodName"` to attribute

### FSM997: Parser validation failed

**Cause**: Critical errors during parsing in normal mode
**Solution**: Check diagnostics.txt for specific issues

### FSM981: No transitions - generating minimal API

**Cause**: Transitions not successfully parsed from syntax
**Solution**: Verify transition attribute syntax is correct

## Best Practices

### For Human Users

1. **Start Simple**: Test with minimal state machines first
2. **Check Diagnostics**: Always review diagnostics.txt for issues
3. **Use Logging**: Enable --logging to understand generated code behavior
4. **Clean Outputs**: Remove output directories between tests to avoid conflicts

### For LLM Agents

1. **Explicit Paths**: Always use absolute or relative paths from GenTest directory
2. **Check Exit Codes**: Non-zero indicates generation failure
3. **Parse Diagnostics**: Look for "error" or "warning" prefixes in diagnostics.txt
4. **Validate Output**: Confirm expected .cs files exist before processing
5. **Clean State**: Run `rm -rf output*/` between test iterations

## Supported State Machine Types

### Simple FSM
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleMachine 
{
    [Transition(State.A, Trigger.Next, State.B)]
    private void Configure() { }
}
public enum State { A, B }
public enum Trigger { Next }
```
**Support Level**: ✅ Full (basic skeleton generated)

### Hierarchical State Machine (HSM)
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class HsmMachine 
{
    [State(State.On, OnEntry = "EnterOn")]
    [State(State.Idle, Parent = State.On, IsInitial = true)]
    private void ConfigureStates() { }
}
```
**Support Level**: ⚠️ Partial (hierarchy may not be fully resolved)

### State Machine with Payloads
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
[PayloadType(typeof(OrderData))]
public partial class PayloadMachine 
{
    [State(State.Processing, OnEntry = "OnProcess")]
    private void OnProcess(OrderData data) { }
}
```
**Support Level**: ⚠️ Partial (payload types not resolved)

### Async State Machine
```csharp
[StateMachine(typeof(State), typeof(Trigger))]
public partial class AsyncMachine 
{
    [State(State.Running, OnEntry = "OnRunAsync")]
    private async Task OnRunAsync(CancellationToken ct) { }
}
```
**Support Level**: ⚠️ Partial (async detection limited)

## File Structure Example

```
GenTest/
├── Program.cs                    # Main GenTest implementation
├── README.md                     # This file
├── test_simple.cs               # Example test file
├── test_hsm.cs                  # HSM test file
├── test_payload_fsm.cs          # Payload test file
├── output/                      # Generated output directory
│   ├── diagnostics.txt
│   ├── __FastFsm.DiscoveredMachines.g.cs
│   └── global__MachineName.Generated.cs
└── LENIENT_MODE_SUMMARY.md     # Technical details of lenient mode
```

## Troubleshooting

### Generator Not Loading

```bash
# Verify generator path
ls -la ../Generator/bin/Release/netstandard2.0/Generator.dll

# Check generator is built
cd ../Generator && dotnet build -c Release
```

### No Code Generated

```bash
# Check diagnostics for errors
cat output/diagnostics.txt | grep -i error

# Try with minimal test case
echo '[StateMachine(typeof(S), typeof(T))]
public partial class Test {}
public enum S { A }
public enum T { X }' > minimal.cs

dotnet run -- --in minimal.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll
```

### Build Errors from Previous Outputs

```bash
# Clean all outputs
rm -rf output*/
dotnet clean

# Run test
dotnet run -- --in test.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll
```

### Watch Mode Not Detecting Changes

```bash
# Ensure generator is built to same location
cd ../Generator
dotnet build -c Release -o bin/Release/netstandard2.0/

# Verify file timestamp changes
stat ../Generator/bin/Release/netstandard2.0/Generator.dll
```

## Technical Implementation Details

### Assembly Loading

GenTest uses `AssemblyLoadContext` for isolated generator loading:
- Generators are loaded in separate context
- Allows unloading and reloading without restart
- Prevents assembly version conflicts

### Reference Collection

Automatically collects references from generator directory:
- Excludes `Microsoft.CodeAnalysis.*` (provided by GenTest)
- Excludes `System.*` assemblies
- Includes all other DLLs as MetadataReferences

### Compilation Context

Creates minimal Roslyn compilation:
```csharp
CSharpCompilation.Create(
    assemblyName: Path.GetFileNameWithoutExtension(inputFile),
    syntaxTrees: new[] { syntaxTree },
    references: minimalReferences,
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
)
```

## Version Information

- Tool Version: 1.0.0
- Minimum .NET Version: 9.0
- Compatible with FastFSM Generator: 2.0+
- Roslyn Version: 4.0+

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success - generation completed |
| 1 | Command line argument error |
| 2 | Input file not found |
| 3 | Generator loading failed |
| 4 | Compilation/generation error |

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `GENTEST_VERBOSE` | Enable verbose output | `false` |
| `GENTEST_NO_COLOR` | Disable colored console output | `false` |
| `GENTEST_WATCH_INTERVAL` | File watch check interval (ms) | `500` |

## Performance Considerations

- **Startup Time**: ~1-2 seconds for initial load
- **Regeneration Time**: ~100-500ms in watch mode
- **Memory Usage**: ~50-100MB typical
- **File Size Limits**: No hard limit, tested up to 10,000 lines

## Security Notes

- GenTest executes arbitrary code from generator assemblies
- Only load trusted generator DLLs
- Output directory is created with current user permissions
- No network access required or performed

## Support

For issues or questions:
1. Check diagnostics.txt for detailed error information
2. Review LENIENT_MODE_SUMMARY.md for technical details
3. Ensure generator is built in Release configuration
4. Verify input file syntax is valid C#

## License

Part of the FastFSM project. See main project for license details.