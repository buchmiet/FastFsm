# FastFSM.Net Installation Guide

## Table of Contents
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Complete Example](#complete-example)

## Prerequisites

### Required Software
- **.NET SDK 9.0** or later ([Download](https://dotnet.microsoft.com/download))
- **IDE** with C# support (Visual Studio 2022, Visual Studio Code, JetBrains Rider)
- **NuGet Package Manager** (included with .NET SDK)

### Verify Prerequisites
```bash
# Check .NET SDK version
dotnet --version
# Should output: 9.0.x or higher

# Check NuGet sources
dotnet nuget list source
# Should include: https://api.nuget.org/v3/index.json
```

## Installation

### Step 1: Create a New Project

```bash
# Create a new console application
dotnet new console -n MyStateMachine -f net9.0

# Navigate to project directory
cd MyStateMachine
```

### Step 2: Install FastFSM.Net Package

```bash
# Install the latest stable version
dotnet add package FastFsm.Net --version 0.6.9.2

# Or install the latest pre-release
dotnet add package FastFsm.Net --prerelease
```

### Alternative: PackageReference in .csproj

You can also add the package reference directly to your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FastFsm.Net" Version="0.6.9.2" />
  </ItemGroup>
</Project>
```

Then restore packages:
```bash
dotnet restore
```

## Quick Start

### Simple State Machine Example

Create a file `Program.cs` with the following content:

```csharp
using System;
using Abstractions.Attributes;

// 1. Define states and triggers as enums
public enum LightState { Off, On }
public enum LightTrigger { TurnOn, TurnOff }

// 2. Create state machine class with attributes
[StateMachine(typeof(LightState), typeof(LightTrigger))]
public partial class LightSwitch
{
    // 3. Define transitions
    [Transition(LightState.Off, LightTrigger.TurnOn, LightState.On)]
    [Transition(LightState.On, LightTrigger.TurnOff, LightState.Off)]
    private void ConfigureTransitions() { }
    
    // 4. Optional: Add state callbacks
    [State(LightState.On, OnEntry = nameof(OnLightOn))]
    private void ConfigureOnState() { }
    
    private void OnLightOn() => Console.WriteLine("Light is ON!");
}

// 5. Use the state machine
class Program
{
    static void Main()
    {
        var light = new LightSwitch(LightState.Off);
        light.Start(); // Initialize the state machine
        
        Console.WriteLine($"Initial: {light.CurrentState}");
        
        light.Fire(LightTrigger.TurnOn);
        Console.WriteLine($"After TurnOn: {light.CurrentState}");
    }
}
```

### Build and Run

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

Expected output:
```
Initial: Off
Light is ON!
After TurnOn: On
```

## Configuration

### Project Configuration

FastFSM.Net uses **Roslyn Source Generators** to generate optimized code at compile time. No additional configuration is required for basic usage.

#### Optional Compile-Time Flags

For advanced scenarios, you can define compile-time constants in your `.csproj`:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <!-- Enable safe guards and actions in Debug mode -->
  <DefineConstants>$(DefineConstants);FASTFSM_SAFE_ACTIONS;FASTFSM_SAFE_GUARDS</DefineConstants>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <!-- Maximum performance in Release mode -->
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

### Generator Configuration

The source generator automatically detects and processes classes marked with `[StateMachine]` attribute. Generated code is placed in:
- `obj/Debug/net9.0/Generator/Generator.StateMachineGenerator/`

### IDE Configuration

#### Visual Studio 2022
- Source generators are fully supported
- IntelliSense will show generated methods after first build
- Generated files visible in Solution Explorer under Dependencies → Analyzers

#### Visual Studio Code
- Install C# extension
- Enable "omnisharp.enableRoslynAnalyzers": true in settings

#### JetBrains Rider
- Full support for source generators
- Generated code visible in "Generated Files" folder

## Verification

### Verify Installation

1. **Check Package Installation**
```bash
dotnet list package
# Should show: FastFsm.Net 0.6.9.2
```

2. **Verify Source Generation**
After building, check for generated files:
```bash
# Windows
dir obj\Debug\net9.0\Generator\Generator.StateMachineGenerator\

# Linux/macOS
ls obj/Debug/net9.0/Generator/Generator.StateMachineGenerator/
```

3. **Test Compilation**
```bash
dotnet build --verbosity normal
# Should complete without errors
# Warnings FSM002/FSM004 are often false positives
```

### Common Warnings (Can Be Ignored)

- **FSM002**: "State might be unreachable" - Often false positive for hierarchical states
- **FSM004**: "Class missing [StateMachine] attribute" - False positive when attribute is present
- **CS0108**: "Member hides inherited member" - Normal for HSM, will be fixed in future versions

## Troubleshooting

### Issue: Package Not Found

```bash
error: Unable to find package FastFsm.Net
```

**Solution**: Ensure NuGet.org is in your package sources:
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Issue: Namespace 'Abstractions.Attributes' Not Found

```csharp
error CS0234: The type or namespace name 'Abstractions' does not exist
```

**Solution**: 
1. Ensure package is installed: `dotnet add package FastFsm.Net`
2. Clean and rebuild: `dotnet clean && dotnet build`

### Issue: Partial Class Required

```csharp
error: State machine class must be declared as 'partial'
```

**Solution**: Add `partial` keyword to your class:
```csharp
public partial class MyStateMachine // ← Note 'partial'
```

### Issue: Generated Methods Not Found

**Solution**:
1. Build the project first: `dotnet build`
2. Restart your IDE/OmniSharp
3. Check generated files exist in `obj/` directory

## Complete Example

### Full Working Example with FSM and HSM

```csharp
using System;
using Abstractions.Attributes;

namespace StateMachineDemo;

// ==========================================
// EXAMPLE 1: Traffic Light (Simple FSM)
// ==========================================

public enum TrafficState { Red, Yellow, Green }
public enum TrafficTrigger { Next }

[StateMachine(typeof(TrafficState), typeof(TrafficTrigger))]
public partial class TrafficLight
{
    [State(TrafficState.Red, OnEntry = nameof(ShowRed))]
    [State(TrafficState.Yellow, OnEntry = nameof(ShowYellow))]
    [State(TrafficState.Green, OnEntry = nameof(ShowGreen))]
    private void ConfigureStates() { }
    
    [Transition(TrafficState.Red, TrafficTrigger.Next, TrafficState.Green)]
    [Transition(TrafficState.Green, TrafficTrigger.Next, TrafficState.Yellow)]
    [Transition(TrafficState.Yellow, TrafficTrigger.Next, TrafficState.Red)]
    private void ConfigureTransitions() { }
    
    private void ShowRed() => Console.WriteLine("🔴 RED - Stop!");
    private void ShowYellow() => Console.WriteLine("🟡 YELLOW - Caution!");
    private void ShowGreen() => Console.WriteLine("🟢 GREEN - Go!");
}

// ==========================================
// EXAMPLE 2: Media Player (Hierarchical FSM)
// ==========================================

public enum PlayerState 
{ 
    PowerOff,
    PowerOn,              // Parent
    PowerOn_Stopped,      // Child
    PowerOn_Playing,      // Child  
    PowerOn_Paused        // Child
}

public enum PlayerTrigger 
{ 
    PowerButton, 
    Play, 
    Pause, 
    Stop 
}

[StateMachine(typeof(PlayerState), typeof(PlayerTrigger), EnableHierarchy = true)]
public partial class MediaPlayer
{
    private string _currentTrack = "Song.mp3";
    
    [State(PlayerState.PowerOn, History = HistoryMode.Shallow)]
    private void ConfigurePowerOn() { }
    
    [State(PlayerState.PowerOn_Stopped, Parent = PlayerState.PowerOn, IsInitial = true)]
    private void ConfigureStopped() { }
    
    [State(PlayerState.PowerOn_Playing, Parent = PlayerState.PowerOn, 
        OnEntry = nameof(StartPlayback), OnExit = nameof(StopPlayback))]
    private void ConfigurePlaying() { }
    
    [State(PlayerState.PowerOn_Paused, Parent = PlayerState.PowerOn)]
    private void ConfigurePaused() { }
    
    [Transition(PlayerState.PowerOff, PlayerTrigger.PowerButton, PlayerState.PowerOn)]
    [Transition(PlayerState.PowerOn, PlayerTrigger.PowerButton, PlayerState.PowerOff)]
    [Transition(PlayerState.PowerOn_Stopped, PlayerTrigger.Play, PlayerState.PowerOn_Playing)]
    [Transition(PlayerState.PowerOn_Playing, PlayerTrigger.Pause, PlayerState.PowerOn_Paused)]
    [Transition(PlayerState.PowerOn_Playing, PlayerTrigger.Stop, PlayerState.PowerOn_Stopped)]
    [Transition(PlayerState.PowerOn_Paused, PlayerTrigger.Play, PlayerState.PowerOn_Playing)]
    [Transition(PlayerState.PowerOn_Paused, PlayerTrigger.Stop, PlayerState.PowerOn_Stopped)]
    private void ConfigureTransitions() { }
    
    private void StartPlayback() => Console.WriteLine($"▶️ Playing: {_currentTrack}");
    private void StopPlayback() => Console.WriteLine("⏸️ Playback stopped");
}

// ==========================================
// MAIN PROGRAM
// ==========================================

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Traffic Light Demo ===");
        var traffic = new TrafficLight(TrafficState.Red);
        traffic.Start();
        
        for (int i = 0; i < 4; i++)
        {
            traffic.Fire(TrafficTrigger.Next);
        }
        
        Console.WriteLine("\n=== Media Player Demo ===");
        var player = new MediaPlayer(PlayerState.PowerOff);
        player.Start();
        
        Console.WriteLine($"State: {player.CurrentState}");
        
        player.Fire(PlayerTrigger.PowerButton);
        Console.WriteLine($"State: {player.CurrentState}");
        
        player.Fire(PlayerTrigger.Play);
        player.Fire(PlayerTrigger.Pause);
        player.Fire(PlayerTrigger.PowerButton); // Power off
        player.Fire(PlayerTrigger.PowerButton); // Power on - should restore to Paused
        Console.WriteLine($"Restored state: {player.CurrentState}");
    }
}
```

### Project Structure After Installation

```
MyStateMachine/
├── MyStateMachine.csproj
├── Program.cs
├── bin/
│   └── Debug/
│       └── net9.0/
│           ├── MyStateMachine.dll
│           ├── MyStateMachine.exe
│           ├── Abstractions.dll
│           └── FastFsm.dll
└── obj/
    └── Debug/
        └── net9.0/
            └── Generator/
                └── Generator.StateMachineGenerator/
                    ├── global__YourNamespace.YourStateMachine.Generated.cs
                    └── [other generated files]
```

## Additional Resources

- **Official Documentation**: [FastFSM GitHub Repository](https://github.com/buchmiet/FastFsm)
- **NuGet Package**: [FastFsm.Net on NuGet.org](https://www.nuget.org/packages/FastFsm.Net)
- **Examples**: See the `Example/` folder in the repository
- **Issues/Support**: [GitHub Issues](https://github.com/buchmiet/FastFsm/issues)

## Version Compatibility

| FastFSM.Net Version | .NET Version | Status |
|-------------------|--------------|---------|
| 0.6.9.2          | .NET 9.0+    | Current |
| 0.6.9-pre-release | .NET 9.0+    | Deprecated |
| 0.6.x            | .NET 8.0+    | Supported |

## License

FastFSM.Net is licensed under the MIT License. See the [LICENSE](https://github.com/buchmiet/FastFsm/blob/main/LICENSE) file for details.