using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using FastFsm.Tests.TestHelpers;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Diagnostics
{
    /// <summary>
    /// Diagnostic tests for enum conversion issues between Fluent and Legacy APIs
    /// </summary>
    public class EnumConversionDiagnosticsTests
    {
        private readonly ITestOutputHelper _output;

        public EnumConversionDiagnosticsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetMachinesFromConfig()
        {
            foreach (var machineName in MatrixConfig.GetAllMachineNames())
            {
                yield return new object[] { machineName };
            }
        }

        /// <summary>
        /// Test that initial state can be resolved and converted for both APIs
        /// </summary>
        [Theory]
        [MemberData(nameof(GetMachinesFromConfig))]
        public void CanResolveInitialState_FluentAndLegacy(string machineName)
        {
            _output.WriteLine($"=== Testing {machineName} ===");
            
            var config = MatrixConfig.GetConfig(machineName);
            if (config == null)
            {
                _output.WriteLine($"WARNING: No config found for {machineName}");
                return;
            }

            // Get the enum types for this machine
            var enumTypes = GetEnumTypesForMachine(machineName);
            if (enumTypes == null)
            {
                _output.WriteLine($"WARNING: Could not determine enum types for {machineName}");
                return;
            }

            _output.WriteLine($"State Enums: Fluent={enumTypes.FluentState?.Name}, Legacy={enumTypes.LegacyState?.Name}");
            
            // Test InitialStateResolver
            if (enumTypes.FluentState != null)
            {
                var resolvedFluent = ResolveInitialState(enumTypes.FluentState, machineName, config.InitialState);
                _output.WriteLine($"Fluent Initial: Config='{config.InitialState}' -> Resolved='{resolvedFluent}'");
                
                // Verify it exists in enum
                var fluentValues = Enum.GetNames(enumTypes.FluentState);
                _output.WriteLine($"Fluent States: {string.Join(", ", fluentValues)}");
                
                if (!fluentValues.Contains(resolvedFluent))
                {
                    _output.WriteLine($"ERROR: Resolved state '{resolvedFluent}' not in Fluent enum!");
                }
            }

            if (enumTypes.LegacyState != null)
            {
                var resolvedLegacy = ResolveInitialState(enumTypes.LegacyState, machineName, config.InitialState);
                _output.WriteLine($"Legacy Initial: Config='{config.InitialState}' -> Resolved='{resolvedLegacy}'");
                
                // Verify it exists in enum
                var legacyValues = Enum.GetNames(enumTypes.LegacyState);
                _output.WriteLine($"Legacy States: {string.Join(", ", legacyValues)}");
                
                if (!legacyValues.Contains(resolvedLegacy))
                {
                    _output.WriteLine($"ERROR: Resolved state '{resolvedLegacy}' not in Legacy enum!");
                }
            }

            // Check for aliases in EnumConverterV2
            if (EnumConverterV2.Maps.TryGetValue(machineName, out var machineMap))
            {
                _output.WriteLine($"Aliases defined: {machineMap.Count} entries");
                foreach (var kvp in machineMap)
                {
                    _output.WriteLine($"  {kvp.Key} -> {kvp.Value}");
                }
            }
            else
            {
                _output.WriteLine("No aliases defined in EnumConverterV2.Maps");
            }
            
            _output.WriteLine("");
        }

        /// <summary>
        /// Test conversion of common trigger names
        /// </summary>
        [Theory]
        [MemberData(nameof(GetMachinesFromConfig))]
        public void CanConvertKnownTriggers_FluentAndLegacy(string machineName)
        {
            _output.WriteLine($"=== Testing Triggers for {machineName} ===");
            
            var config = MatrixConfig.GetConfig(machineName);
            if (config == null) return;

            var enumTypes = GetEnumTypesForMachine(machineName);
            if (enumTypes?.FluentTrigger == null || enumTypes?.LegacyTrigger == null)
            {
                _output.WriteLine($"Skipping - trigger enums not found");
                return;
            }

            var commonTriggers = new[] { "Start", "Next", "Process", "Ship", "Update", "Toggle", "Reset", "Complete" };
            var fluentTriggers = Enum.GetNames(enumTypes.FluentTrigger);
            var legacyTriggers = Enum.GetNames(enumTypes.LegacyTrigger);

            _output.WriteLine($"Fluent Triggers: {string.Join(", ", fluentTriggers)}");
            _output.WriteLine($"Legacy Triggers: {string.Join(", ", legacyTriggers)}");

            foreach (var trigger in commonTriggers)
            {
                var existsInFluent = fluentTriggers.Contains(trigger);
                var existsInLegacy = legacyTriggers.Contains(trigger);
                
                if (existsInFluent || existsInLegacy)
                {
                    _output.WriteLine($"  {trigger}: Fluent={existsInFluent}, Legacy={existsInLegacy}");
                    
                    if (existsInFluent != existsInLegacy)
                    {
                        _output.WriteLine($"    WARNING: Trigger parity mismatch!");
                    }
                }
            }
            
            // Test configured trigger sequence
            if (config.TriggerSequence?.Any() == true)
            {
                _output.WriteLine($"Configured sequence: {string.Join(" -> ", config.TriggerSequence)}");
                foreach (var trigger in config.TriggerSequence)
                {
                    var inFluent = fluentTriggers.Contains(trigger);
                    var inLegacy = legacyTriggers.Contains(trigger);
                    
                    if (!inFluent || !inLegacy)
                    {
                        _output.WriteLine($"  ERROR: '{trigger}' missing in Fluent={!inFluent}, Legacy={!inLegacy}");
                    }
                }
            }
            
            _output.WriteLine("");
        }

        /// <summary>
        /// Test wrapper instantiation and basic operations
        /// </summary>
        [Theory]
        [MemberData(nameof(GetMachinesFromConfig))]
        public void WrapperStartAndTryFire_MinimalHappyPath(string machineName)
        {
            _output.WriteLine($"=== Testing Wrapper Operations for {machineName} ===");
            
            try
            {
                // Try Fluent wrapper
                _output.WriteLine("Creating Fluent wrapper...");
                var fluentWrapper = StateMachineWrapperFactory.Create(
                    machineName, 
                    StateMachineWrapperFactory.ApiType.Fluent, 
                    null); // Use fallback
                
                _output.WriteLine($"Fluent wrapper created, CurrentState before Start: {fluentWrapper.CurrentState}");
                fluentWrapper.Start();
                _output.WriteLine($"Started, CurrentState: {fluentWrapper.CurrentState}");
                
                // Try Legacy wrapper
                _output.WriteLine("Creating Legacy wrapper...");
                var legacyWrapper = StateMachineWrapperFactory.Create(
                    machineName, 
                    StateMachineWrapperFactory.ApiType.Legacy, 
                    null); // Use fallback
                
                _output.WriteLine($"Legacy wrapper created, CurrentState before Start: {legacyWrapper.CurrentState}");
                legacyWrapper.Start();
                _output.WriteLine($"Started, CurrentState: {legacyWrapper.CurrentState}");
                
                // Compare initial states
                var fluentState = fluentWrapper.CurrentState?.ToString();
                var legacyState = legacyWrapper.CurrentState?.ToString();
                
                if (fluentState != legacyState)
                {
                    _output.WriteLine($"WARNING: Initial state mismatch! Fluent='{fluentState}', Legacy='{legacyState}'");
                }
                
                // Try firing a trigger if configured
                var config = MatrixConfig.GetConfig(machineName);
                if (config?.TriggerSequence?.Any() == true)
                {
                    var firstTrigger = config.TriggerSequence[0];
                    _output.WriteLine($"Attempting to fire trigger: {firstTrigger}");
                    
                    try
                    {
                        var fluentCanFire = fluentWrapper.CanFire(firstTrigger);
                        var legacyCanFire = legacyWrapper.CanFire(firstTrigger);
                        _output.WriteLine($"  CanFire: Fluent={fluentCanFire}, Legacy={legacyCanFire}");
                        
                        if (fluentCanFire)
                        {
                            fluentWrapper.Fire(firstTrigger);
                            _output.WriteLine($"  Fluent fired, new state: {fluentWrapper.CurrentState}");
                        }
                        
                        if (legacyCanFire)
                        {
                            legacyWrapper.Fire(firstTrigger);
                            _output.WriteLine($"  Legacy fired, new state: {legacyWrapper.CurrentState}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  ERROR firing trigger: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                
                _output.WriteLine("SUCCESS: Both wrappers instantiated and started");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"FAILURE: {ex.GetType().Name}: {ex.Message}");
                _output.WriteLine($"Stack: {ex.StackTrace?.Split('\n')[0]}");
            }
            
            _output.WriteLine("");
        }

        /// <summary>
        /// Detailed enum comparison for a machine
        /// </summary>
        [Theory]
        [MemberData(nameof(GetMachinesFromConfig))]
        public void CompareEnumValues_DetailedReport(string machineName)
        {
            _output.WriteLine($"=== Enum Comparison for {machineName} ===");
            
            var enumTypes = GetEnumTypesForMachine(machineName);
            if (enumTypes == null) 
            {
                _output.WriteLine("Could not determine enum types");
                return;
            }

            // Compare States
            if (enumTypes.FluentState != null && enumTypes.LegacyState != null)
            {
                _output.WriteLine("STATE ENUMS:");
                CompareEnums(enumTypes.FluentState, enumTypes.LegacyState);
            }
            
            // Compare Triggers
            if (enumTypes.FluentTrigger != null && enumTypes.LegacyTrigger != null)
            {
                _output.WriteLine("TRIGGER ENUMS:");
                CompareEnums(enumTypes.FluentTrigger, enumTypes.LegacyTrigger);
            }
            
            _output.WriteLine("");
        }

        private void CompareEnums(Type fluentEnum, Type legacyEnum)
        {
            var fluentValues = Enum.GetNames(fluentEnum);
            var legacyValues = Enum.GetNames(legacyEnum);
            
            _output.WriteLine($"  Fluent ({fluentEnum.Name}): {string.Join(", ", fluentValues)}");
            _output.WriteLine($"  Legacy ({legacyEnum.Name}): {string.Join(", ", legacyValues)}");
            
            var onlyInFluent = fluentValues.Except(legacyValues).ToList();
            var onlyInLegacy = legacyValues.Except(fluentValues).ToList();
            
            if (onlyInFluent.Any())
            {
                _output.WriteLine($"  Only in Fluent: {string.Join(", ", onlyInFluent)}");
            }
            
            if (onlyInLegacy.Any())
            {
                _output.WriteLine($"  Only in Legacy: {string.Join(", ", onlyInLegacy)}");
            }
            
            if (!onlyInFluent.Any() && !onlyInLegacy.Any())
            {
                _output.WriteLine("  ✓ Enums are identical");
            }
        }

        private string ResolveInitialState(Type stateEnumType, string machineName, string? preferredName)
        {
            // Use reflection to call InitialStateResolver.ResolveOrDefault<T>
            var method = typeof(InitialStateResolver).GetMethod(
                nameof(InitialStateResolver.ResolveOrDefault),
                BindingFlags.Public | BindingFlags.Static);
            
            var genericMethod = method!.MakeGenericMethod(stateEnumType);
            var result = genericMethod.Invoke(null, new object?[] { machineName, preferredName });
            
            return result?.ToString() ?? "null";
        }

        private class MachineEnumTypes
        {
            public Type? FluentState { get; set; }
            public Type? LegacyState { get; set; }
            public Type? FluentTrigger { get; set; }
            public Type? LegacyTrigger { get; set; }
        }

        private MachineEnumTypes? GetEnumTypesForMachine(string machineName)
        {
            // This is a simplified mapping - in real implementation, 
            // this should use MachineRegistry or similar
            return machineName switch
            {
                "CoreBenchmark" => new MachineEnumTypes
                {
                    FluentState = typeof(Features.Performance.BenchmarkTests.BenchmarkState),
                    LegacyState = typeof(Features.Performance.BenchmarkTestsLegacy.BenchmarkState),
                    FluentTrigger = typeof(Features.Performance.BenchmarkTests.BenchmarkTrigger),
                    LegacyTrigger = typeof(Features.Performance.BenchmarkTestsLegacy.BenchmarkTrigger)
                },
                "GuardPermitted" => new MachineEnumTypes
                {
                    FluentState = typeof(State),
                    LegacyState = typeof(State),
                    FluentTrigger = typeof(Trigger),
                    LegacyTrigger = typeof(Trigger)
                },
                "InternalTransition" => new MachineEnumTypes
                {
                    FluentState = typeof(StateCallbackTests.InternalState),
                    LegacyState = typeof(StateCallbackTests.InternalState),
                    FluentTrigger = typeof(StateCallbackTests.InternalTrigger),
                    LegacyTrigger = typeof(StateCallbackTests.InternalTrigger)
                },
                "ExceptionCallback" => new MachineEnumTypes
                {
                    FluentState = typeof(StateCallbackTests.ExceptionState),
                    LegacyState = typeof(StateCallbackTests.ExceptionState),
                    FluentTrigger = typeof(StateCallbackTests.ExceptionTrigger),
                    LegacyTrigger = typeof(StateCallbackTests.ExceptionTrigger)
                },
                "PayloadStateMachine" => new MachineEnumTypes
                {
                    FluentState = typeof(TestState),
                    LegacyState = typeof(TestState),
                    FluentTrigger = typeof(TestTrigger),
                    LegacyTrigger = typeof(TestTrigger)
                },
                "FullMultiPayload" => new MachineEnumTypes
                {
                    FluentState = typeof(MultiState),
                    LegacyState = typeof(MultiState),
                    FluentTrigger = typeof(MultiTrigger),
                    LegacyTrigger = typeof(MultiTrigger)
                },
                _ => null
            };
        }
    }
}