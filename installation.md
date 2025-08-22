# FastFSM Installation Guide

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Complete Examples](#complete-examples)
- [Additional Resources](#additional-resources)

## Prerequisites

- **.NET SDK 9.0** or later ([Download](https://dotnet.microsoft.com/download))
- Any IDE with C# support (Visual Studio 2022, VS Code, Rider)

Verify installation:
```bash
dotnet --version  # Should output: 9.0.x or higher
```

## Installation

### Create a New Project

```bash
dotnet new console -n MyStateMachine -f net9.0
cd MyStateMachine
```

### Install Package

```bash
# Install latest stable version
dotnet add package FastFsm.Net

# Or specific version
dotnet add package FastFsm.Net --version 0.6.9.13
```

Alternative via `.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="FastFsm.Net" Version="0.6.9.13" />
</ItemGroup>
```

## Quick Start

Create `Program.cs`:

```csharp
using Abstractions.Attributes;

// Define states and triggers
public enum LightState { Off, On }
public enum LightTrigger { Toggle }

// Create state machine class
[StateMachine(typeof(LightState), typeof(LightTrigger))]
public partial class LightSwitch
{
    [Transition(LightState.Off, LightTrigger.Toggle, LightState.On)]
    [Transition(LightState.On, LightTrigger.Toggle, LightState.Off)]
    private void ConfigureTransitions() { }
    
    [State(LightState.On, OnEntry = nameof(OnLightOn))]
    private void ConfigureOnState() { }
    
    private void OnLightOn() => Console.WriteLine("Light is ON!");
}

// Usage
var light = new LightSwitch(LightState.Off);
light.Start();  // Required: initialize the state machine

Console.WriteLine($"State: {light.CurrentState}");  // Off
light.Fire(LightTrigger.Toggle);                    // Light is ON!
Console.WriteLine($"State: {light.CurrentState}");  // On
```

Build and run:
```bash
dotnet build
dotnet run
```

## Configuration

### Compile-Time Safety (Optional)

Add to `.csproj` for debug builds:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <DefineConstants>$(DefineConstants);FASTFSM_SAFE_ACTIONS;FASTFSM_SAFE_GUARDS</DefineConstants>
</PropertyGroup>
```

This wraps guards and actions in try/catch blocks during development.

### Source Generator

FastFSM uses Roslyn source generators to create optimized code at compile time. Generated files are located in:
```
obj/Debug/net9.0/generated/Generator.StateMachineGenerator/
```

No configuration required – the generator automatically processes classes with `[StateMachine]` attribute.

## Verification

```bash
# Check package installation
dotnet list package
# Should show: FastFsm.Net

# Build with warnings as errors
dotnet build -warnaserror
# Should succeed with 0 warnings, 0 errors

# Verify generated code exists
ls obj/Debug/net9.0/generated/*/*.cs
```

## Troubleshooting

### Common Issues

**Package not found:**
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

**Namespace 'Abstractions' not found:**
```bash
dotnet clean && dotnet build
```

**Class must be partial:**
```csharp
public partial class MyStateMachine  // ← Required 'partial' keyword
```

**Generated methods not visible in IDE:**
1. Build first: `dotnet build`
2. Restart IDE/language server
3. IntelliSense will recognize generated members after initial build

## Complete Examples

### Simple FSM – Traffic Light

```csharp
using Abstractions.Attributes;

public enum TrafficState { Red, Yellow, Green }
public enum TrafficTrigger { Next }

[StateMachine(typeof(TrafficState), typeof(TrafficTrigger))]
public partial class TrafficLight
{
    [Transition(TrafficState.Red, TrafficTrigger.Next, TrafficState.Green)]
    [Transition(TrafficState.Green, TrafficTrigger.Next, TrafficState.Yellow)]
    [Transition(TrafficState.Yellow, TrafficTrigger.Next, TrafficState.Red)]
    private void ConfigureTransitions() { }
    
    [State(TrafficState.Red, OnEntry = nameof(ShowRed))]
    [State(TrafficState.Yellow, OnEntry = nameof(ShowYellow))]
    [State(TrafficState.Green, OnEntry = nameof(ShowGreen))]
    private void ConfigureStates() { }
    
    private void ShowRed() => Console.WriteLine("RED - Stop!");
    private void ShowYellow() => Console.WriteLine("YELLOW - Caution!");
    private void ShowGreen() => Console.WriteLine("GREEN - Go!");
}
```

### Hierarchical FSM – Media Player

```csharp
public enum PlayerState 
{ 
    PowerOff,
    PowerOn,              // Parent
    PowerOn_Stopped,      // Child
    PowerOn_Playing,      // Child  
    PowerOn_Paused        // Child
}

public enum PlayerTrigger { PowerButton, Play, Pause, Stop }

[StateMachine(typeof(PlayerState), typeof(PlayerTrigger), EnableHierarchy = true)]
public partial class MediaPlayer
{
    [State(PlayerState.PowerOn, History = HistoryMode.Shallow)]
    private void ConfigurePowerOn() { }
    
    [State(PlayerState.PowerOn_Stopped, Parent = PlayerState.PowerOn, IsInitial = true)]
    [State(PlayerState.PowerOn_Playing, Parent = PlayerState.PowerOn, 
        OnEntry = nameof(StartPlayback), OnExit = nameof(StopPlayback))]
    [State(PlayerState.PowerOn_Paused, Parent = PlayerState.PowerOn)]
    private void ConfigureStates() { }
    
    [Transition(PlayerState.PowerOff, PlayerTrigger.PowerButton, PlayerState.PowerOn)]
    [Transition(PlayerState.PowerOn, PlayerTrigger.PowerButton, PlayerState.PowerOff)]
    [Transition(PlayerState.PowerOn_Stopped, PlayerTrigger.Play, PlayerState.PowerOn_Playing)]
    [Transition(PlayerState.PowerOn_Playing, PlayerTrigger.Pause, PlayerState.PowerOn_Paused)]
    [Transition(PlayerState.PowerOn_Playing, PlayerTrigger.Stop, PlayerState.PowerOn_Stopped)]
    [Transition(PlayerState.PowerOn_Paused, PlayerTrigger.Play, PlayerState.PowerOn_Playing)]
    [Transition(PlayerState.PowerOn_Paused, PlayerTrigger.Stop, PlayerState.PowerOn_Stopped)]
    private void ConfigureTransitions() { }
    
    private void StartPlayback() => Console.WriteLine("Playing...");
    private void StopPlayback() => Console.WriteLine("Stopped");
}

// Usage
var player = new MediaPlayer(PlayerState.PowerOff);
player.Start();

player.Fire(PlayerTrigger.PowerButton);  // → PowerOn_Stopped (initial child)
player.Fire(PlayerTrigger.Play);         // → PowerOn_Playing
player.Fire(PlayerTrigger.PowerButton);  // → PowerOff
player.Fire(PlayerTrigger.PowerButton);  // → PowerOn_Playing (history restored)
```

## Project Structure

After installation, generated code is located in:
```
MyStateMachine/
├── MyStateMachine.csproj
├── Program.cs
├── bin/Debug/net9.0/
│   ├── Abstractions.dll
│   └── FastFsm.dll
└── obj/Debug/net9.0/generated/
    └── Generator.StateMachineGenerator/
        └── *.Generated.cs
```

## Version Compatibility

| FastFSM.Net | .NET | Notes |
|-------------|------|-------|
| 0.6.9.13+   | 9.0+ | Current stable, HSM support |
| 0.6.9.x     | 9.0+ | All 0.6.9 versions stable |
| 0.6.x       | 8.0+ | Basic FSM only |

## Additional Resources

- [GitHub Repository](https://github.com/buchmiet/FastFsm)
- [NuGet Package](https://www.nuget.org/packages/FastFsm.Net)
- [Issues & Support](https://github.com/buchmiet/FastFsm/issues)

## License

MIT License. See [LICENSE](https://github.com/buchmiet/FastFsm/blob/main/LICENSE).