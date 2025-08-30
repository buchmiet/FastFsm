# GenTest - Fast Roslyn Source Generator Test Runner with Hot-Reload

A high-performance tool for testing Roslyn source generators with **hot-reload support**. GenTest allows you to rapidly iterate on generator development without rebuilding the test harness.

## Features

- ⚡ **Hot-reload**: Automatically detects and reloads generator changes
- 🔄 **No rebuild required**: Test new generator versions without rebuilding GenTest
- 📝 **Full diagnostics**: See all generator diagnostics and warnings
- 💾 **File output**: Save generated files for inspection
- 🎯 **Isolated execution**: Uses AssemblyLoadContext for clean generator loading/unloading
- 🔍 **Auto-discovery**: Automatically finds Generator.dll if not specified

## Installation

Build the project once:
```bash
cd GenTest
dotnet build -c Release
```

That's it! GenTest is ready to use.

## Quick Start

```bash
# Basic usage - test your generator on a file
dotnet run -- --in test.cs

# Watch mode - auto-reload when generator changes
dotnet run -- --in test.cs --watch

# Save output to files
dotnet run -- --in test.cs --out output/
```

## Usage

### Basic Usage

Test a source generator on a single file:
```bash
dotnet run -- --in test.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll
```

If `--generator` is omitted, GenTest automatically searches for Generator.dll in standard locations.

### Watch Mode (Hot-Reload) 🔥

This is the killer feature! Monitor generator DLL for changes and automatically re-run:

```bash
dotnet run -- --in test.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll --watch
```

Now in another terminal:
```bash
# Edit your generator code...
cd ../Generator
dotnet build -c Release
# GenTest automatically reloads and shows new results!
```

Press `Ctrl+C` to exit watch mode.

### Save Output to Files

Save generated files and diagnostics to a directory:
```bash
dotnet run -- --in test.cs --out output/
```

This creates:
- `output/diagnostics.txt` - All generator diagnostics
- `output/*.g.cs` - All generated source files

### Enable Generator Features

Test generators with logging support:
```bash
dotnet run -- --in test.cs --logging
```

Test generators with dependency injection:
```bash
dotnet run -- --in test.cs --di
```

## Command-Line Options

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--in <file>` | Path to input .cs file to process | ✅ Yes | - |
| `--generator <dll>` | Path to generator DLL | ❌ No | Auto-detect |
| `--out <dir>` | Output directory for generated files | ❌ No | Console output |
| `--watch` | Watch generator DLL for changes | ❌ No | false |
| `--logging` | Enable logging support (FsmGenerateLogging) | ❌ No | false |
| `--di` | Enable dependency injection (FsmGenerateDI) | ❌ No | false |

## How Paths Work

### Understanding Path Resolution

GenTest executes from `bin/Debug/net9.0/` when using `dotnet run`. This is crucial for understanding paths:

```
Project structure:
FastFsm/
├── GenTest/
│   ├── test.cs                    <- Your test file
│   ├── bin/Debug/net9.0/          <- GenTest runs from here!
│   └── output/                    <- Generated output
└── Generator/
    └── bin/Release/netstandard2.0/
        └── Generator.dll           <- Your generator
```

### Path Resolution Rules

1. **Input file (`--in`)**: Relative to where you run the command
   ```bash
   cd GenTest
   dotnet run -- --in test.cs           # ✅ Finds GenTest/test.cs
   dotnet run -- --in ./tests/test.cs   # ✅ Finds GenTest/tests/test.cs
   ```

2. **Generator (`--generator`)**: Relative to current directory OR auto-detected
   ```bash
   # Explicit relative path
   dotnet run -- --in test.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll
   
   # Auto-detection (searches these paths):
   dotnet run -- --in test.cs
   # Searches: ../Generator/bin/Release/netstandard2.0/Generator.dll
   #          ../Generator/bin/Debug/netstandard2.0/Generator.dll
   ```

3. **Output (`--out`)**: Relative to current directory
   ```bash
   dotnet run -- --in test.cs --out output/      # Creates GenTest/output/
   dotnet run -- --in test.cs --out /tmp/gen/    # Absolute path
   ```

### Path Examples

```bash
# From GenTest directory - most common usage:
cd GenTest
dotnet run -- --in test.cs --generator ../Generator/bin/Release/netstandard2.0/Generator.dll

# Let GenTest auto-detect the generator:
dotnet run -- --in test.cs

# With absolute paths (always works):
dotnet run -- \
  --in /home/user/tests/test.cs \
  --generator /home/user/FastFsm/Generator/bin/Release/netstandard2.0/Generator.dll \
  --out /tmp/output/

# From repository root:
cd FastFsm
dotnet run --project GenTest -- \
  --in GenTest/test.cs \
  --generator Generator/bin/Release/netstandard2.0/Generator.dll
```

## Hot-Reload Workflow

The most efficient workflow for generator development:

1. **Terminal 1 - Start watch mode:**
   ```bash
   cd GenTest
   dotnet run -- --in test.cs --watch
   ```
   GenTest is now monitoring the generator DLL.

2. **Terminal 2 - Edit and rebuild generator:**
   ```bash
   cd Generator
   # Edit your generator code...
   dotnet build -c Release
   ```

3. **See instant results!** GenTest automatically:
   - Detects the file change
   - Unloads the old generator
   - Loads the new version
   - Reruns generation
   - Shows updated output

4. **Repeat** steps 2-3 for rapid iteration

5. Press `Ctrl+C` in Terminal 1 to exit

## Common Errors and Solutions

### Error: "No generators found in [path]"

**Cause**: The DLL doesn't contain a valid generator.

**Solution**: Ensure your generator:
```csharp
[Generator]  // Required attribute
public class MyGenerator : IIncrementalGenerator  // Or ISourceGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Implementation
    }
}
```

### Error: "Object of type X cannot be converted to type IIncrementalGenerator"

**Cause**: Version mismatch between Microsoft.CodeAnalysis assemblies.

**Solution**: 
1. Ensure GenTest and Generator use same Microsoft.CodeAnalysis.CSharp version
2. Check Generator output doesn't contain Microsoft.CodeAnalysis*.dll files:
   ```bash
   ls ../Generator/bin/Release/netstandard2.0/Microsoft.CodeAnalysis*.dll
   # These should NOT exist - delete if present
   ```

### Warning: "No [StateMachine] attribute" or "Parser validation failed"

**Cause**: Missing references or incorrect attribute usage.

**Solution**: 
1. Ensure Abstractions.dll is built:
   ```bash
   cd ../Abstractions
   dotnet build -c Release
   ```

2. Check your test file has correct using and attributes:
   ```csharp
   using Abstractions.Attributes;  // Required
   
   [StateMachine(typeof(StateEnum), typeof(TriggerEnum))]  // Two arguments required
   public partial class MyStateMachine { }
   ```

### Error: "Input file not found"

**Cause**: Incorrect relative path.

**Solution**: Remember you're in the GenTest directory:
```bash
# Wrong:
dotnet run -- --in ../GenTest/test.cs  # Looking in parent directory

# Correct:
dotnet run -- --in test.cs             # File is in GenTest/
```

### Watch mode not detecting changes

**Cause**: File system delays or editor behavior.

**Solutions**:
1. Ensure you save the file (Ctrl+S)
2. Some editors use temp files - make sure Generator.dll is actually updated
3. On WSL, add a small delay after save
4. Try touching the file: `touch ../Generator/bin/Release/netstandard2.0/Generator.dll`

### Generator runs but no code generated

**Cause**: Generator found no valid input or hit validation errors.

**Check**:
1. Read diagnostics section carefully
2. Look for FSM* diagnostic codes
3. Ensure test file has valid StateMachine attributes
4. Check "Valid: X, Skipped: Y" in discovery dump

## Output Format

### Console Output
```
[LOAD] Loading generators from: /path/to/Generator.dll
  ✓ Loaded IIncrementalGenerator: Generator.StateMachineGenerator
[LOAD] Loaded 1 generator(s) successfully

[RUN #1] Executing generators...
  Added reference: Abstractions.dll
============= DIAGNOSTICS =============
Total: 2 (Errors: 0, Warnings: 1)
  [Warning] FSM001: State 'Open' has no exit action
  [Info] FSM995: Generated 1 state machine

============= GENERATED FILES =============
--- DoorController.g.cs ---
// <auto-generated/>
// Generator Build: 2025-08-30 10:15:23 UTC
#nullable enable

namespace Test
{
    public partial class DoorController
    {
        // Generated state machine implementation...
    }
}
```

### File Output (with `--out`)
```
output/
├── diagnostics.txt                        # All diagnostics
├── DoorController.g.cs                    # Generated state machine
└── __FastFsm.DiscoveredMachines.g.cs     # Discovery summary
```

## Example Test File

Create `test_light.cs`:
```csharp
using Abstractions.Attributes;
using System;

namespace TestFsm
{
    public enum LightState { Off, On, Blinking }
    public enum LightTrigger { TurnOn, TurnOff, StartBlink, StopBlink }

    [StateMachine(typeof(LightState), typeof(LightTrigger))]
    public partial class LightController
    {
        [Transition(LightState.Off, LightTrigger.TurnOn, LightState.On)]
        [Transition(LightState.On, LightTrigger.TurnOff, LightState.Off)]
        [Transition(LightState.On, LightTrigger.StartBlink, LightState.Blinking)]
        [Transition(LightState.Blinking, LightTrigger.StopBlink, LightState.On)]
        [Transition(LightState.Blinking, LightTrigger.TurnOff, LightState.Off)]
        private void ConfigureTransitions() { }

        [State(LightState.On, OnEntry = nameof(OnLightOn))]
        private void ConfigureOnState() { }
        
        private void OnLightOn() => Console.WriteLine("Light is ON!");
    }
}
```

Test it:
```bash
# Basic test
dotnet run -- --in test_light.cs

# With output
dotnet run -- --in test_light.cs --out output/

# Watch mode for development
dotnet run -- --in test_light.cs --watch
```

## Tips for Effective Testing

1. **Always use watch mode** during active development
2. **Save output** (`--out`) to inspect in your editor with syntax highlighting
3. **Start simple** - test with minimal state machine first
4. **Read diagnostics first** when generation fails
5. **Use absolute paths** if you have path issues
6. **Check discovery dump** to see what the generator found
7. **Build in Release mode** for faster generator execution

## Advanced Usage

### Testing Multiple Configurations

Create a test script:
```bash
#!/bin/bash
# test_all.sh
for config in "" "--logging" "--di" "--logging --di"; do
    echo "Testing with: $config"
    dotnet run -- --in test.cs $config --out "output_$config/"
done
```

### CI/CD Integration

```yaml
# .github/workflows/test-generator.yml
- name: Test Generator
  run: |
    cd GenTest
    dotnet build
    dotnet run -- --in test_door.cs --out test_output/
    if grep -q "Error" test_output/diagnostics.txt; then
      echo "Generator produced errors!"
      exit 1
    fi
```

### Debugging Generator Issues

1. **Enable verbose output** by modifying Program.cs
2. **Check loaded types**:
   ```bash
   dotnet run -- --in test.cs 2>&1 | grep "Checking type"
   ```
3. **Verify references**:
   ```bash
   dotnet run -- --in test.cs 2>&1 | grep "Added reference"
   ```

## Technical Details

### How It Works

1. **Dynamic Loading**: Uses `AssemblyLoadContext` to load generators
2. **Type Identity**: Prevents loading Microsoft.CodeAnalysis in isolated context
3. **Hot-Reload**: `FileSystemWatcher` monitors generator DLL
4. **Clean Unload**: Properly disposes and unloads assemblies
5. **Compilation**: Creates minimal Roslyn compilation with required references

### Key Components

- **HotRunner**: Manages generator loading/unloading
- **CollectibleAlc**: Custom AssemblyLoadContext for isolation
- **CSharpGeneratorDriver**: Executes the generator
- **FileSystemWatcher**: Monitors for DLL changes

### Requirements

- .NET 9.0 SDK
- Generator targeting netstandard2.0
- Microsoft.CodeAnalysis.CSharp 4.12.0 (must match generator)

## Troubleshooting WSL

For WSL users:
- File watching may have 100-200ms delay
- Use `/mnt/c/...` paths for Windows files
- Ensure execute permissions: `chmod +x Generator.dll`
- If watch doesn't work, try `inotify-tools` package

## License

Part of the FastFsm project.