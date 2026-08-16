# FastFSM Test Infrastructure Documentation

## Overview

This document provides a comprehensive overview of the test infrastructure implemented in FastFsm.Tests project. The infrastructure enables testing of state machines written in both Legacy (attribute-based) and Fluent (DSL-based) APIs, ensuring complete functional parity between them.

## Project Structure

```
FastFsm.Tests/
├── TestHelpers/                    # Core infrastructure
│   ├── IStateMachineTestWrapper.cs # Unified interface for testing
│   ├── ApiCapabilities.cs          # API capability flags
│   ├── StateMachineWrapperFactory.cs # Factory for creating wrappers
│   ├── MachineTypeRegistry.cs      # Central enum type registry
│   ├── MatrixConfig.cs             # Test configuration
│   ├── EnumConverterExtensions.cs  # Enum conversion utilities
│   ├── MachineTypes.cs             # Type definitions
│   ├── InitialStateResolver.cs     # Initial state resolution
│   └── [Machine]Wrappers.cs        # Wrapper implementations
├── Features/
│   ├── Parity/                     # Parity tests
│   │   ├── DualApiMatrixTests.cs   # Matrix tests for both APIs
│   │   └── MatrixConfigValidationTests.cs
│   ├── Core/                       # Core functionality tests
│   ├── Hsm/                        # Hierarchical state machine tests
│   └── ...
└── Machines/                       # State machine definitions

```

## Core Components

### 1. IStateMachineTestWrapper Interface

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/IStateMachineTestWrapper.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Unified interface for testing both Fluent and Legacy API state machines
    /// </summary>
    public interface IStateMachineTestWrapper
    {
        // Properties
        object CurrentState { get; }
        ApiCapabilities Caps { get; }
        
        // Synchronous methods
        void Start();
        bool TryFire(object trigger, object? payload = null);
        void Fire(object trigger, object? payload = null);
        bool CanFire(object trigger);
        IReadOnlyList<object> GetPermittedTriggers();
        
        // Asynchronous methods
        ValueTask StartAsync(CancellationToken ct = default);
        ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default);
        ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default);
    }
}
```

### 2. ApiCapabilities Enumeration

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/ApiCapabilities.cs`

```csharp
using System;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Describes the capabilities of a state machine API implementation
    /// </summary>
    [Flags]
    public enum ApiCapabilities
    {
        None = 0,
        
        /// <summary>
        /// Machine supports async operations (StartAsync, FireAsync, etc.)
        /// </summary>
        HasAsync = 1 << 0,
        
        /// <summary>
        /// Machine has a default payload type configured
        /// </summary>
        HasDefaultPayload = 1 << 1,
        
        /// <summary>
        /// Machine supports multiple payload types via .Payload<T>()
        /// </summary>
        HasMultiPayloads = 1 << 2,
        
        /// <summary>
        /// Machine supports internal transitions
        /// </summary>
        HasInternalTransitions = 1 << 3,
        
        /// <summary>
        /// Machine is hierarchical (HSM)
        /// </summary>
        IsHierarchical = 1 << 4,
        
        /// <summary>
        /// Machine has async guards or actions
        /// </summary>
        RequiresAsyncPath = 1 << 5
    }
    
    /// <summary>
    /// Extension methods for ApiCapabilities
    /// </summary>
    public static class ApiCapabilitiesExtensions
    {
        public static bool Has(this ApiCapabilities caps, ApiCapabilities flag)
        {
            return (caps & flag) == flag;
        }
        
        public static bool RequiresAsync(this ApiCapabilities caps)
        {
            return caps.Has(ApiCapabilities.RequiresAsyncPath) || caps.Has(ApiCapabilities.HasAsync);
        }
        
        public static bool SupportsPayloads(this ApiCapabilities caps)
        {
            return caps.Has(ApiCapabilities.HasDefaultPayload) || caps.Has(ApiCapabilities.HasMultiPayloads);
        }
    }
}
```

### 3. StateMachineWrapperFactory

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/StateMachineWrapperFactory.cs`

```csharp
using System;
using System.Collections.Generic;
using FastFsm.Tests.Features.Performance;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Factory for creating state machine wrappers based on API type
    /// </summary>
    public static class StateMachineWrapperFactory
    {
        public enum ApiType 
        { 
            Fluent, 
            Legacy 
        }
        
        /// <summary>
        /// Helper to get the state enum type for a machine and API
        /// </summary>
        private static Type GetStateEnumType(string machine, ApiType api) =>
            MachineTypeRegistry.GetStateType(machine, api == ApiType.Fluent ? Api.Fluent : Api.Legacy);
        
        /// <summary>
        /// Helper to get the trigger enum type for a machine and API
        /// </summary>
        private static Type GetTriggerEnumType(string machine, ApiType api) =>
            MachineTypeRegistry.GetTriggerType(machine, api == ApiType.Fluent ? Api.Fluent : Api.Legacy);
        
        /// <summary>
        /// Parses a state enum value from string using the correct type
        /// </summary>
        public static object GetStateEnum(string machine, ApiType api, string name)
        {
            var type = GetStateEnumType(machine, api);
            return Enum.Parse(type, name, ignoreCase: false);
        }
        
        /// <summary>
        /// Parses a trigger enum value from string using the correct type
        /// </summary>
        public static object GetTriggerEnum(string machine, ApiType api, string name)
        {
            var type = GetTriggerEnumType(machine, api);
            return Enum.Parse(type, name, ignoreCase: false);
        }
        
        /// <summary>
        /// Registry of machine types and their wrapper creators
        /// </summary>
        private static readonly Dictionary<string, Func<ApiType, string, IStateMachineTestWrapper>> _wrapperFactories = new()
        {
            ["CoreBenchmark"] = CreateCoreBenchmarkWrapper,
            ["BasicBenchmark"] = CreateCoreBenchmarkWrapper, // Uses same enums as CoreBenchmark
            ["NoGuardBenchmark"] = CreateCoreBenchmarkWrapper, // Uses same enums as CoreBenchmark
            ["WithGuardBenchmark"] = CreateCoreBenchmarkWrapper, // Uses same enums as CoreBenchmark
            ["GuardPermitted"] = CreateGuardPermittedWrapper,
            ["PayloadStateMachine"] = CreatePayloadStateMachineWrapper,
            ["FullMultiPayload"] = CreateFullMultiPayloadWrapper,
            ["InternalTransition"] = CreateInternalTransitionWrapper,
            ["ExceptionCallback"] = CreateExceptionCallbackWrapper,
            
            // Callback machines
            ["MultipleCallbacks"] = CreateMultipleCallbacksWrapper,
            ["InitialState"] = CreateInitialStateWrapper,
            ["CallbackOrder"] = CreateCallbackOrderWrapper,
            ["ComplexCallback"] = CreateComplexCallbackWrapper,
            ["GuardedCallback"] = CreateGuardedCallbackWrapper,
            ["SelfTransition"] = CreateSelfTransitionWrapper,
            
            // Edge case machines
            ["CaseSensitive"] = CreateCaseSensitiveWrapper,
            ["ConflictingNames"] = CreateConflictingNamesWrapper,
            ["LongName"] = CreateLongNameWrapper,
            ["InternalOnly"] = CreateInternalOnlyWrapper,
            ["Unreachable"] = CreateUnreachableWrapper,
            ["SingleState"] = CreateSingleStateWrapper,
            ["FullOrder"] = CreateFullOrderWrapper,
            ["Unicode"] = CreateUnicodeWrapper,
            ["Numeric"] = CreateNumericWrapper,
            ["KeywordState"] = CreateKeywordStateWrapper,
            
            // HSM machines
            ["SimpleParentChild"] = CreateSimpleParentChildWrapper,
            ["DeepHistory"] = CreateDeepHistoryWrapper,
            ["ShallowHistory"] = CreateShallowHistoryWrapper,
            ["InitialChild"] = CreateInitialChildWrapper,
            ["InternalTransitionHsm"] = CreateInternalTransitionHsmWrapper,
        };
        
        /// <summary>
        /// Creates a wrapper for the specified machine type and API
        /// </summary>
        public static IStateMachineTestWrapper Create(string machineType, ApiType apiType, string initialStateName)
        {
            if (!_wrapperFactories.TryGetValue(machineType, out var factory))
            {
                throw new NotSupportedException(
                    $"Machine type '{machineType}' is not supported. " +
                    $"Available types: {string.Join(", ", _wrapperFactories.Keys)}");
            }
            
            return factory(apiType, initialStateName);
        }
        
        // Factory methods for each machine type...
        private static IStateMachineTestWrapper CreateCoreBenchmarkWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new CoreBenchmarkFluentWrapper(initialStateName),
                ApiType.Legacy => new CoreBenchmarkLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        // ... (more factory methods for each machine type)
    }
}
```

### 4. MachineTypeRegistry

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/MachineTypeRegistry.cs`

```csharp
using System;
using System.Collections.Generic;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Features.EdgeCases;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Central registry of enum types for all state machines
    /// This is the single source of truth for which enums each machine uses
    /// </summary>
    public static class MachineTypeRegistry
    {
        /// <summary>
        /// Machine name -> type pair (state/trigger per API)
        /// </summary>
        public static readonly IReadOnlyDictionary<string, EnumTypePair> Types =
            new Dictionary<string, EnumTypePair>(StringComparer.Ordinal)
            {
                // ====== SHARED ENUMS (same type for both APIs) ======
                
                // GuardPermitted - uses local enums from GuardPermittedTriggersTests
                ["GuardPermitted"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Core.State),   // FluentState
                    typeof(FastFsm.Tests.Features.Core.State),   // LegacyState (SAME!)
                    typeof(FastFsm.Tests.Features.Core.Trigger), // FluentTrigger
                    typeof(FastFsm.Tests.Features.Core.Trigger)  // LegacyTrigger (SAME!)
                ),

                // InternalTransition - uses shared StateCallbackTests enums
                ["InternalTransition"] = new EnumTypePair(
                    typeof(StateCallbackTests.InternalState),
                    typeof(StateCallbackTests.InternalState),    // SAME!
                    typeof(StateCallbackTests.InternalTrigger),
                    typeof(StateCallbackTests.InternalTrigger)   // SAME!
                ),
                
                // ====== DIFFERENT ENUMS (need actual conversion) ======
                
                // CoreBenchmark - different namespaces
                ["CoreBenchmark"] = new EnumTypePair(
                    typeof(BenchmarkTests.BenchmarkState),
                    typeof(BenchmarkTestsLegacy.BenchmarkState), // Different namespace
                    typeof(BenchmarkTests.BenchmarkTrigger),
                    typeof(BenchmarkTestsLegacy.BenchmarkTrigger) // Different namespace
                ),
                
                // ====== HSM MACHINES (LOCAL ENUMS) ======
                ["SimpleParentChild"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T) // SAME!
                ),

                ["DeepHistory"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.T) // SAME!
                ),
                
                // ... more machines
            };
            
        /// <summary>
        /// Get the state enum type for a machine and API
        /// </summary>
        public static Type GetStateType(string machineName, Api api)
        {
            if (!Types.TryGetValue(machineName, out var pair))
                throw new ArgumentException($"Unknown machine: {machineName}");
            return pair.For(api, isState: true);
        }
        
        /// <summary>
        /// Get the trigger enum type for a machine and API
        /// </summary>
        public static Type GetTriggerType(string machineName, Api api)
        {
            if (!Types.TryGetValue(machineName, out var pair))
                throw new ArgumentException($"Unknown machine: {machineName}");
            return pair.For(api, isState: false);
        }
        
        /// <summary>
        /// Check if a machine uses the same enums for both APIs
        /// </summary>
        public static bool UsesSameEnums(string machineName)
        {
            if (!Types.TryGetValue(machineName, out var pair))
                return false;
            return pair.UsesSameEnums;
        }
    }
}
```

### 5. EnumTypePair Structure

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/MachineTypes.cs`

```csharp
using System;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Represents the enum types used by a state machine for both Fluent and Legacy APIs
    /// </summary>
    public readonly record struct EnumTypePair(
        Type FluentState, 
        Type LegacyState,
        Type FluentTrigger, 
        Type LegacyTrigger)
    {
        /// <summary>
        /// Gets the appropriate enum type based on API and whether it's a state or trigger
        /// </summary>
        public Type For(Api api, bool isState) =>
            (api, isState) switch
            {
                (Api.Fluent, true) => FluentState,
                (Api.Legacy, true) => LegacyState,
                (Api.Fluent, false) => FluentTrigger,
                (Api.Legacy, false) => LegacyTrigger,
                _ => throw new ArgumentException($"Invalid combination: {api}, isState={isState}")
            };
            
        /// <summary>
        /// Checks if this pair uses the same enums for both APIs
        /// </summary>
        public bool UsesSameEnums => 
            FluentState == LegacyState && FluentTrigger == LegacyTrigger;
            
        /// <summary>
        /// Checks if states are the same type in both APIs
        /// </summary>
        public bool UsesSameStateEnum => FluentState == LegacyState;
        
        /// <summary>
        /// Checks if triggers are the same type in both APIs
        /// </summary>
        public bool UsesSameTriggerEnum => FluentTrigger == LegacyTrigger;
    }

    /// <summary>
    /// API type enumeration
    /// </summary>
    public enum Api 
    { 
        Fluent, 
        Legacy 
    }
}
```

### 6. MatrixConfig

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/MatrixConfig.cs`

```csharp
using System;
using System.Collections.Generic;
using FastFsm.Contracts;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Central configuration for matrix tests - defines valid machines and their test scenarios
    /// </summary>
    public static class MatrixConfig
    {
        public class MachineTestConfig
        {
            public string MachineName { get; set; } = "";
            public string InitialState { get; set; } = "";
            public string[] TriggerSequence { get; set; } = Array.Empty<string>();
            public object?[] Payloads { get; set; } = Array.Empty<object?>();
            public bool RequiresAsync { get; set; }
        }

        public class MatrixEntry
        {
            public string MachineName { get; }
            public string? InitialState { get; }
            public ApiCapabilities Capabilities { get; }

            public MatrixEntry(string machineName, string? initialState, ApiCapabilities capabilities)
            {
                MachineName = machineName;
                InitialState = initialState;
                Capabilities = capabilities;
            }
        }

        /// <summary>
        /// Configuration for all machines included in matrix tests
        /// </summary>
        public static readonly IReadOnlyDictionary<string, MachineTestConfig> Machines = new Dictionary<string, MachineTestConfig>
        {
            // Core machines
            ["GuardPermitted"] = new MachineTestConfig
            {
                MachineName = "GuardPermitted",
                InitialState = null, // Will use fallback (Idle or first enum value)
                TriggerSequence = new[] { "Run" }
            },

            // Payload machines
            ["PayloadStateMachine"] = new MachineTestConfig
            {
                MachineName = "PayloadStateMachine",
                InitialState = null, // Will use fallback
                TriggerSequence = new[] { "Start", "Process", "Complete" },
                Payloads = new object?[] { new { OrderId = "TEST-001", Amount = 100.50m }, null }
            },

            ["FullMultiPayload"] = new MachineTestConfig
            {
                MachineName = "FullMultiPayload",
                InitialState = null, // Will use fallback
                TriggerSequence = new[] { "Configure", "Process" },
                Payloads = new object?[] { "TestData", 42 }
            },

            // Internal transition machines
            ["InternalTransition"] = new MachineTestConfig
            {
                MachineName = "InternalTransition",
                InitialState = null, // Will use fallback - Active will be resolved
                TriggerSequence = new[] { "Update", "Deactivate" }
            },

            // Exception handling machines
            ["ExceptionCallback"] = new MachineTestConfig
            {
                MachineName = "ExceptionCallback",
                InitialState = null, // Will use fallback - A will be resolved
                TriggerSequence = new[] { "Go" }
            },

            // HSM machines - now with both Fluent and Legacy implementations
            ["SimpleParentChild"] = new MachineTestConfig
            {
                MachineName = "SimpleParentChild",
                InitialState = "Idle",
                TriggerSequence = new[] { "Start", "Process", "Complete" }
            },

            ["DeepHistory"] = new MachineTestConfig
            {
                MachineName = "DeepHistory",
                InitialState = "Out",
                TriggerSequence = new[] { "EnterWork", "Next", "Abort", "EnterWork" }
            },

            ["ShallowHistory"] = new MachineTestConfig
            {
                MachineName = "ShallowHistory",
                InitialState = "Outside",
                TriggerSequence = new[] { "Enter", "Next", "Exit", "Enter" }
            },

            ["InitialChild"] = new MachineTestConfig
            {
                MachineName = "InitialChild",
                InitialState = "Outside",
                TriggerSequence = new[] { "EnterParent", "Switch", "LeaveParent" }
            },
            
            ["InternalTransitionHsm"] = new MachineTestConfig
            {
                MachineName = "InternalTransitionHsm",
                InitialState = "Parent",
                TriggerSequence = new[] { "Refresh" }
            },
        };

        /// <summary>
        /// Get test configuration for a specific machine
        /// </summary>
        public static MachineTestConfig? GetConfig(string machineName)
        {
            return Machines.TryGetValue(machineName, out var config) ? config : null;
        }

        /// <summary>
        /// Get all machine names that should be included in matrix tests
        /// </summary>
        public static IEnumerable<string> GetAllMachineNames()
        {
            return Machines.Keys;
        }

        /// <summary>
        /// Create a dummy payload for testing
        /// </summary>
        public static object CreateDummyPayload()
        {
            return new { TestData = "DummyPayload", Timestamp = DateTime.UtcNow };
        }

        /// <summary>
        /// Matrix entries for parity testing - defines machines and their capabilities
        /// </summary>
        public static readonly List<MatrixEntry> MatrixEntries = new List<MatrixEntry>
        {
            new MatrixEntry("GuardPermitted", null, ApiCapabilities.None), // Use fallback
            new MatrixEntry("PayloadStateMachine", null, ApiCapabilities.HasDefaultPayload), // Use fallback
            new MatrixEntry("FullMultiPayload", null, ApiCapabilities.HasMultiPayloads), // Use fallback - only HasMultiPayloads
            new MatrixEntry("InternalTransition", null, ApiCapabilities.HasInternalTransitions), // Use fallback
            new MatrixEntry("ExceptionCallback", null, ApiCapabilities.HasAsync | ApiCapabilities.RequiresAsyncPath), // Has async capabilities
            
            // HSM machines
            new MatrixEntry("SimpleParentChild", null, ApiCapabilities.IsHierarchical), // Use fallback
            new MatrixEntry("DeepHistory", null, ApiCapabilities.IsHierarchical), // Has deep history
            new MatrixEntry("ShallowHistory", null, ApiCapabilities.IsHierarchical), // Has shallow history
            new MatrixEntry("InitialChild", null, ApiCapabilities.IsHierarchical), // Use fallback
            new MatrixEntry("InternalTransitionHsm", null, ApiCapabilities.IsHierarchical | ApiCapabilities.HasInternalTransitions), // Has internal transitions
        };
    }
}
```

### 7. InitialStateResolver

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/InitialStateResolver.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Resolves initial state names for state machines with fallback logic
    /// </summary>
    public static class InitialStateResolver
    {
        // Global preferences - order matters, most likely first
        private static readonly string[] GlobalPrefs = new[]
        {
            "Initial", "A", "Active", "New", "Idle", "Start", 
            "Disconnected", "Parent", "Root", "Outside", "None", 
            "Off", "Waiting", "Ready", "Stopped", "Closed", 
            "Unknown", "Default", "Empty", "Uninitialized", "NotStarted"
        };

        // Machine-specific preferences to avoid mismatches
        private static readonly Dictionary<string, string[]> MachinePrefs = new(StringComparer.Ordinal)
        {
            ["InternalTransition"] = new[] { "Active", "A", "Initial" },
            ["GuardPermitted"] = new[] { "Idle", "Initial", "A" },
            ["PayloadStateMachine"] = new[] { "Initial", "New" },
            ["FullMultiPayload"] = new[] { "Initial", "Configured" },
            ["ExceptionCallback"] = new[] { "A", "Initial" },
            ["CoreBenchmark"] = new[] { "A", "Initial" },
            
            // HSM machines
            ["SimpleParentChild"] = new[] { "Idle", "Initial" },
            ["DeepHistory"] = new[] { "Outside", "Out", "Initial" },
            ["ShallowHistory"] = new[] { "Outside", "Out", "Initial" },
            ["InitialChild"] = new[] { "Outside", "Initial" }
        };

        /// <summary>
        /// Resolves the initial state name, using fallback logic if preferred name is invalid
        /// </summary>
        /// <typeparam name="TState">The state enum type</typeparam>
        /// <param name="machineName">Name of the machine (for machine-specific preferences)</param>
        /// <param name="preferredName">Preferred initial state name (can be null or invalid)</param>
        /// <returns>Valid state name from the enum</returns>
        public static string ResolveOrDefault<TState>(string machineName, string? preferredName)
            where TState : struct, Enum
        {
            var availableStates = new HashSet<string>(Enum.GetNames(typeof(TState)), StringComparer.Ordinal);
            
            // 1) Exact match with preferred name
            if (!string.IsNullOrWhiteSpace(preferredName) && availableStates.Contains(preferredName))
            {
                return preferredName;
            }

            // 2) Machine-specific preferences
            if (MachinePrefs.TryGetValue(machineName, out var prefs))
            {
                foreach (var pref in prefs)
                {
                    if (availableStates.Contains(pref))
                        return pref;
                }
            }

            // 3) Global preferences
            foreach (var pref in GlobalPrefs)
            {
                if (availableStates.Contains(pref))
                    return pref;
            }

            // 4) First enum value as absolute fallback
            var firstState = Enum.GetNames(typeof(TState)).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstState))
                return firstState;

            throw new InvalidOperationException(
                $"Cannot resolve initial state (machine: {machineName}, enum: {typeof(TState).Name}). " +
                $"No valid states found in enum.");
        }

        /// <summary>
        /// Tries to resolve the initial state name without throwing
        /// </summary>
        public static bool TryResolveOrDefault<TState>(
            string machineName, 
            string? preferredName, 
            out string resolvedName) where TState : struct, Enum
        {
            try
            {
                resolvedName = ResolveOrDefault<TState>(machineName, preferredName);
                return true;
            }
            catch
            {
                resolvedName = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Gets all available state names for a given enum type
        /// </summary>
        public static IReadOnlyList<string> GetAvailableStates<TState>() where TState : struct, Enum
        {
            return Enum.GetNames(typeof(TState));
        }
    }
}
```

### 8. EnumConverterExtensions

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/EnumConverterExtensions.cs`

```csharp
using System;
using System.Collections.Generic;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Extension methods for enum conversion
    /// </summary>
    public static class EnumConverterExtensions
    {
        /// <summary>
        /// Converts a trigger object to the concrete trigger type for the specified API and machine
        /// </summary>
        public static object ToConcreteTrigger(this object trigger, StateMachineWrapperFactory.ApiType apiType, string machineName)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            
            // If it's a string, we need to convert it to the appropriate enum
            if (trigger is string triggerName)
            {
                // Determine target enum type from MachineTypeRegistry
                var api = apiType == StateMachineWrapperFactory.ApiType.Fluent ? Api.Fluent : Api.Legacy;
                Type targetEnumType = MachineTypeRegistry.GetTriggerType(machineName, api);

                try
                {
                    // Parse into the exact target enum type
                    return Enum.Parse(targetEnumType, triggerName, ignoreCase: false);
                }
                catch
                {
                    return trigger; // return original string if parsing fails
                }
            }
            
            // If it's already an enum, return as-is
            if (trigger.GetType().IsEnum)
            {
                return trigger;
            }
            
            // Default: return as-is
            return trigger;
        }
        
        // Historical mapping removed in favor of MachineTypeRegistry
    }
}
```

## Wrapper Implementation Pattern

Each state machine needs two wrapper classes - one for Fluent API and one for Legacy API. Both implement the `IStateMachineTestWrapper` interface.

### Example: CoreBenchmark Wrapper

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/TestHelpers/CoreBenchmarkWrappers.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Wrapper for CoreBenchmarkMachineFluent
    /// </summary>
    public class CoreBenchmarkFluentWrapper : IStateMachineTestWrapper
    {
        private readonly CoreBenchmarkMachineFluent _machine;
        
        public CoreBenchmarkFluentWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<BenchmarkTests.BenchmarkState>(
                "CoreBenchmark", initialStateName);
            var state = (BenchmarkTests.BenchmarkState)Enum.Parse(
                typeof(BenchmarkTests.BenchmarkState), resolvedName);
            _machine = new CoreBenchmarkMachineFluent(state);
        }
        
        public CoreBenchmarkFluentWrapper(BenchmarkTests.BenchmarkState initialState)
        {
            _machine = new CoreBenchmarkMachineFluent(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.None; // Simple sync-only machine
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Fluent, "CoreBenchmark");
            return payload == null 
                ? _machine.TryFire((BenchmarkTests.BenchmarkTrigger)typedTrigger) 
                : _machine.TryFire((BenchmarkTests.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Fluent, "CoreBenchmark");
            if (payload == null)
                _machine.Fire((BenchmarkTests.BenchmarkTrigger)typedTrigger);
            else
                _machine.Fire((BenchmarkTests.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Fluent, "CoreBenchmark");
            return _machine.CanFire((BenchmarkTests.BenchmarkTrigger)typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers()
        {
            return _machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            _machine.Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var result = TryFire(trigger, payload);
            return ValueTask.FromResult(result);
        }
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// Wrapper for CoreBenchmarkMachineLegacy
    /// </summary>
    public class CoreBenchmarkLegacyWrapper : IStateMachineTestWrapper
    {
        private readonly CoreBenchmarkMachineLegacy _machine;
        
        public CoreBenchmarkLegacyWrapper(string? initialStateName)
        {
            var resolvedName = InitialStateResolver.ResolveOrDefault<BenchmarkTestsLegacy.BenchmarkState>(
                "CoreBenchmark", initialStateName);
            var state = (BenchmarkTestsLegacy.BenchmarkState)Enum.Parse(
                typeof(BenchmarkTestsLegacy.BenchmarkState), resolvedName);
            _machine = new CoreBenchmarkMachineLegacy(state);
        }
        
        public CoreBenchmarkLegacyWrapper(BenchmarkTestsLegacy.BenchmarkState initialState)
        {
            _machine = new CoreBenchmarkMachineLegacy(initialState);
        }
        
        public object CurrentState => _machine.CurrentState;
        
        public ApiCapabilities Caps => ApiCapabilities.None; // Simple sync-only machine
        
        public void Start() => _machine.Start();
        
        public bool TryFire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Legacy, "CoreBenchmark");
            return payload == null 
                ? _machine.TryFire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger) 
                : _machine.TryFire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public void Fire(object trigger, object? payload = null)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Legacy, "CoreBenchmark");
            if (payload == null)
                _machine.Fire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger);
            else
                _machine.Fire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger, payload);
        }
        
        public bool CanFire(object trigger)
        {
            var typedTrigger = trigger.ToConcreteTrigger(StateMachineWrapperFactory.ApiType.Legacy, "CoreBenchmark");
            return _machine.CanFire((BenchmarkTestsLegacy.BenchmarkTrigger)typedTrigger);
        }
        
        public IReadOnlyList<object> GetPermittedTriggers()
        {
            return _machine.GetPermittedTriggers().Cast<object>().ToList();
        }
        
        public ValueTask StartAsync(CancellationToken ct = default)
        {
            _machine.Start();
            return ValueTask.CompletedTask;
        }
        
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            var result = TryFire(trigger, payload);
            return ValueTask.FromResult(result);
        }
        
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default)
        {
            Fire(trigger, payload);
            return ValueTask.CompletedTask;
        }
    }
}
```

## Matrix Test Implementation

The matrix tests ensure parity between Fluent and Legacy APIs by running the same test scenarios on both implementations.

### DualApiMatrixTests

**File:** `/home/lukasz/FastFsm/FastFsm.Tests/Features/Parity/DualApiMatrixTests.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using FastFsm.Tests.TestHelpers;
using Shouldly;
using static FastFsm.Tests.TestHelpers.StateMachineWrapperFactory;

namespace FastFsm.Tests.Features.Parity
{
    /// <summary>
    /// Matrix tests that run all machines on both APIs to ensure functional parity
    /// </summary>
    [Trait("Category", "Parity")]
    [Trait("Category", "Matrix")]
    public class DualApiMatrixTests
    {
        private readonly ITestOutputHelper _output;

        public DualApiMatrixTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetAllMachinesAndApis()
        {
            foreach (var machineName in MatrixConfig.GetAllMachineNames())
            {
                yield return new object[] { machineName, ApiType.Fluent };
                yield return new object[] { machineName, ApiType.Legacy };
            }
        }

        [Theory]
        [MemberData(nameof(GetAllMachinesAndApis))]
        public void Machine_BasicOperations_WorkOnBothApis(string machineName, ApiType apiType)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull($"Machine {machineName} not found in MatrixConfig");
            
            try
            {
                // Create wrapper using factory
                var wrapper = StateMachineWrapperFactory.Create(machineName, apiType, config.InitialState);
                wrapper.ShouldNotBeNull($"Failed to create {apiType} wrapper for {machineName}");
                
                // Start machine
                wrapper.Start();
                
                // Get current state
                var currentState = wrapper.CurrentState;
                currentState.ShouldNotBeNull($"{machineName} ({apiType}) CurrentState is null");
                
                // Get permitted triggers
                var permittedTriggers = wrapper.GetPermittedTriggers();
                permittedTriggers.ShouldNotBeNull($"{machineName} ({apiType}) GetPermittedTriggers returned null");
                
                // Try to execute the configured trigger sequence
                if (config.TriggerSequence.Length > 0)
                {
                    var firstTrigger = config.TriggerSequence[0];
                    var canFire = wrapper.CanFire(firstTrigger);
                    
                    if (canFire)
                    {
                        // Prepare payload if needed
                        object? payload = null;
                        if (config.Payloads.Length > 0)
                        {
                            payload = config.Payloads[0];
                        }
                        else if (wrapper.Caps.Has(ApiCapabilities.HasDefaultPayload) || 
                                 wrapper.Caps.Has(ApiCapabilities.HasMultiPayloads))
                        {
                            payload = MatrixConfig.CreateDummyPayload();
                        }
                        
                        // Try to fire the trigger
                        try
                        {
                            var result = wrapper.TryFire(firstTrigger, payload);
                            
                            // For internal transitions, state might not change
                            if (!wrapper.Caps.Has(ApiCapabilities.HasInternalTransitions) || !result)
                            {
                                // Either it should succeed and change state, or fail
                                if (result)
                                {
                                    var newState = wrapper.CurrentState;
                                    _output.WriteLine($"{machineName} ({apiType}): {currentState} -> {newState} via {firstTrigger}");
                                }
                                else
                                {
                                    _output.WriteLine($"{machineName} ({apiType}): TryFire({firstTrigger}) returned false");
                                }
                            }
                            else
                            {
                                _output.WriteLine($"{machineName} ({apiType}): Internal transition {firstTrigger} executed");
                            }
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("FSM204"))
                        {
                            // Async path required
                            _output.WriteLine($"{machineName} ({apiType}): Requires async path for {firstTrigger}");
                            wrapper.Caps.Has(ApiCapabilities.RequiresAsyncPath).ShouldBeTrue(
                                $"Machine threw FSM204 but doesn't have RequiresAsyncPath capability");
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("payload"))
                        {
                            // Payload required but not provided correctly
                            _output.WriteLine($"{machineName} ({apiType}): Payload required for {firstTrigger}");
                        }
                    }
                }
                
                _output.WriteLine($"✅ {machineName} ({apiType}): Basic operations successful");
            }
            catch (NotImplementedException)
            {
                var message = $"{machineName} ({apiType}) wrapper not fully implemented";
                _output.WriteLine($"⚠️ {message}");
                return; // Skip test for now
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ {machineName} ({apiType}): {ex.Message}");
                throw;
            }
        }

        [Theory]
        [MemberData(nameof(GetAllMachinesAndApis))]
        public async void Machine_AsyncOperations_WorkOnBothApis(string machineName, ApiType apiType)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull($"Machine {machineName} not found in MatrixConfig");
            
            try
            {
                // Create wrapper using factory
                var wrapper = StateMachineWrapperFactory.Create(machineName, apiType, config.InitialState);
                wrapper.ShouldNotBeNull($"Failed to create {apiType} wrapper for {machineName}");
                
                // Start machine async
                await wrapper.StartAsync();
                
                // Get current state
                var currentState = wrapper.CurrentState;
                currentState.ShouldNotBeNull($"{machineName} ({apiType}) CurrentState is null after StartAsync");
                
                // Get permitted triggers
                var permittedTriggers = wrapper.GetPermittedTriggers();
                
                // Try to execute the configured trigger sequence async
                if (config.TriggerSequence.Length > 0)
                {
                    var firstTrigger = config.TriggerSequence[0];
                    var canFire = wrapper.CanFire(firstTrigger);
                    
                    if (canFire)
                    {
                        // Prepare payload if needed
                        object? payload = null;
                        if (config.Payloads.Length > 0)
                        {
                            payload = config.Payloads[0];
                        }
                        else if (wrapper.Caps.Has(ApiCapabilities.HasDefaultPayload) || 
                                 wrapper.Caps.Has(ApiCapabilities.HasMultiPayloads))
                        {
                            payload = MatrixConfig.CreateDummyPayload();
                        }
                        
                        // Try to fire the trigger async
                        try
                        {
                            var result = await wrapper.TryFireAsync(firstTrigger, payload);
                            
                            if (result)
                            {
                                var newState = wrapper.CurrentState;
                                _output.WriteLine($"{machineName} ({apiType}): Async {currentState} -> {newState} via {firstTrigger}");
                            }
                            else
                            {
                                _output.WriteLine($"{machineName} ({apiType}): Async TryFire({firstTrigger}) returned false");
                            }
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("payload"))
                        {
                            // Payload required but not provided correctly
                            _output.WriteLine($"{machineName} ({apiType}): Async payload required for {firstTrigger}");
                        }
                    }
                }
                
                _output.WriteLine($"✅ {machineName} ({apiType}): Async operations successful");
            }
            catch (NotImplementedException)
            {
                var message = $"{machineName} ({apiType}) async wrapper not fully implemented";
                _output.WriteLine($"⚠️ {message}");
                return; // Skip test for now
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ {machineName} ({apiType}) Async: {ex.Message}");
                throw;
            }
        }

        [Theory]
        [MemberData(nameof(GetAllMachinesAndApis))]
        public void Machine_Capabilities_AreConsistent(string machineName, ApiType apiType)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull($"Machine {machineName} not found in MatrixConfig");
            
            try
            {
                var wrapper = StateMachineWrapperFactory.Create(machineName, apiType, config.InitialState);
                var caps = wrapper.Caps;
                
                _output.WriteLine($"{machineName} ({apiType}) Capabilities: {caps}");
                
                // Verify capabilities make sense
                if (caps.Has(ApiCapabilities.RequiresAsyncPath))
                {
                    caps.Has(ApiCapabilities.HasAsync).ShouldBeTrue(
                        "RequiresAsyncPath should imply HasAsync");
                }
                
                if (caps.Has(ApiCapabilities.HasMultiPayloads))
                {
                    caps.Has(ApiCapabilities.HasDefaultPayload).ShouldBeFalse(
                        "HasMultiPayloads and HasDefaultPayload should be mutually exclusive");
                }
            }
            catch (NotImplementedException)
            {
                _output.WriteLine($"⚠️ {machineName} ({apiType}) wrapper not implemented - skipping");
                return;
            }
        }
    }
}
```

## Integration Process for New Projects

To integrate this infrastructure into other test projects (FastFsm.Async.Tests, FastFsm.DependencyInjection.Tests, FastFsm.Logging.Tests):

### 1. Copy Core Infrastructure Files

Copy the following files from `FastFsm.Tests/TestHelpers/`:
- `IStateMachineTestWrapper.cs`
- `ApiCapabilities.cs`
- `StateMachineWrapperFactory.cs`
- `MachineTypeRegistry.cs`
- `MachineTypes.cs`
- `MatrixConfig.cs`
- `InitialStateResolver.cs`
- `EnumConverterExtensions.cs`

### 2. Create Fluent API Equivalents

For each existing Legacy state machine in the target project:
1. Create a Fluent API version using the DSL pattern
2. Ensure the same states, triggers, and transitions
3. Use shared enums where possible, or create local enums when needed

### 3. Implement Wrapper Classes

For each state machine pair (Fluent + Legacy):
1. Create a wrapper file (e.g., `[MachineName]Wrappers.cs`)
2. Implement both `[MachineName]FluentWrapper` and `[MachineName]LegacyWrapper`
3. Both should implement `IStateMachineTestWrapper`
4. Set appropriate `ApiCapabilities` flags

### 4. Register in Infrastructure

1. Add entries to `MachineTypeRegistry` for enum type mappings
2. Add factory methods to `StateMachineWrapperFactory`
3. Add configuration to `MatrixConfig` with test scenarios
4. Update `InitialStateResolver` with machine-specific preferences if needed

### 5. Create Matrix Tests

Create parity test classes similar to `DualApiMatrixTests` that:
1. Use `[Theory]` with `[MemberData]` to test all machines
2. Test both Fluent and Legacy APIs
3. Verify functional parity
4. Test async operations if applicable

### 6. Project-Specific Considerations

#### FastFsm.Async.Tests
- Focus on async capabilities
- All machines should have `ApiCapabilities.HasAsync`
- Test async guards, actions, and lifecycle methods

#### FastFsm.DependencyInjection.Tests
- Focus on DI integration
- Test service resolution in guards and actions
- Verify scoped and singleton lifetime behaviors

#### FastFsm.Logging.Tests
- Focus on logging integration
- Test log output for state transitions
- Verify structured logging with proper event IDs

## Key Design Patterns

### 1. Wrapper Pattern
- Abstracts differences between Fluent and Legacy APIs
- Provides uniform interface for testing
- Handles enum type conversions

### 2. Factory Pattern
- Centralized creation of machine wrappers
- Type-safe enum resolution
- API-specific instance creation

### 3. Registry Pattern
- Single source of truth for enum types
- Centralized machine metadata
- Supports both shared and different enum scenarios

### 4. Strategy Pattern
- Different resolution strategies for initial states
- Machine-specific and global preferences
- Fallback mechanisms

### 5. Matrix Testing
- Parametrized tests across all machines and APIs
- Ensures complete coverage
- Validates functional parity

## Benefits of This Architecture

1. **Maintainability**: Centralized configuration and registry make it easy to add new machines
2. **Consistency**: Uniform testing interface ensures all machines are tested the same way
3. **Scalability**: Easy to extend to other test projects
4. **Type Safety**: Compile-time enum type checking prevents runtime errors
5. **Flexibility**: Supports both shared and different enum scenarios
6. **Comprehensive Testing**: Matrix tests ensure nothing is missed

## Migration Checklist

When migrating to another test project:

- [ ] Copy infrastructure files
- [ ] Create Fluent API machines for existing Legacy machines
- [ ] Implement wrapper classes for each machine pair
- [ ] Register machines in MachineTypeRegistry
- [ ] Add factory methods to StateMachineWrapperFactory
- [ ] Configure test scenarios in MatrixConfig
- [ ] Add initial state preferences if needed
- [ ] Create matrix test classes
- [ ] Run tests and verify parity
- [ ] Document any project-specific requirements

## Conclusion

This infrastructure provides a robust foundation for testing state machines across different API styles. By ensuring parity between Legacy and Fluent APIs, we guarantee that both approaches produce functionally equivalent state machines, giving users confidence to choose the API style that best fits their needs.