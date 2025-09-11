using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using FastFsm.Tests.TestHelpers;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Features.Parity
{
    /// <summary>
    /// Tests to validate enum name parity between Fluent and Legacy APIs
    /// </summary>
    public class EnumNameParityTests
    {
        private readonly ITestOutputHelper _output;

        public EnumNameParityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetMachinesFromConfig()
        {
            foreach (var entry in MatrixConfig.MatrixEntries)
            {
                yield return new object[] { entry.MachineName };
            }
        }

        /// <summary>
        /// Verifies that all enum values can be mapped between Fluent and Legacy
        /// </summary>
        [Theory]
        [MemberData(nameof(GetMachinesFromConfig))]
        public void AllEnumValues_CanBeMapped_BetweenFluentAndLegacy(string machineName)
        {
            // Skip HSM machines as they use completely different enum structures
            if (machineName == "SimpleParentChild" || machineName == "DeepHistory" || 
                machineName == "ShallowHistory" || machineName == "InitialChild")
            {
                _output.WriteLine($"Skipping {machineName}: HSM machines use different enum structures");
                return;
            }
            
            _output.WriteLine($"=== Testing {machineName} ===");
            
            var enumTypes = GetMachineEnumTypes(machineName);
            if (enumTypes == null)
            {
                _output.WriteLine($"WARNING: Could not determine enum types for {machineName}");
                return;
            }

            // Test State enums
            if (enumTypes.FluentState != null && enumTypes.LegacyState != null)
            {
                _output.WriteLine("STATE ENUMS:");
                TestEnumParity(machineName, enumTypes.FluentState, enumTypes.LegacyState, "State");
            }

            // Test Trigger enums
            if (enumTypes.FluentTrigger != null && enumTypes.LegacyTrigger != null)
            {
                _output.WriteLine("TRIGGER ENUMS:");
                TestEnumParity(machineName, enumTypes.FluentTrigger, enumTypes.LegacyTrigger, "Trigger");
            }
        }

        private void TestEnumParity(string machineName, Type fluentEnum, Type legacyEnum, string enumKind)
        {
            var fluentNames = Enum.GetNames(fluentEnum);
            var legacyNames = Enum.GetNames(legacyEnum);

            _output.WriteLine($"  Fluent {enumKind} ({fluentEnum.Name}): {string.Join(", ", fluentNames)}");
            _output.WriteLine($"  Legacy {enumKind} ({legacyEnum.Name}): {string.Join(", ", legacyNames)}");

            // If same enum type, they're automatically compatible
            if (fluentEnum == legacyEnum)
            {
                _output.WriteLine($"  ✓ Same enum type used for both APIs");
                return;
            }

            var errors = new List<string>();
            var suggestions = new List<string>();

            // Test Fluent -> Legacy conversion
            foreach (var fluentName in fluentNames)
            {
                try
                {
                    // Use reflection to test conversion
                    var testMethod = typeof(EnumConverterV2)
                        .GetMethod(nameof(EnumConverterV2.ToLegacy))!
                        .MakeGenericMethod(legacyEnum);
                    
                    var fluentValue = Enum.Parse(fluentEnum, fluentName);
                    var legacyValue = testMethod.Invoke(null, new object[] { fluentValue, machineName });
                    
                    _output.WriteLine($"  ✓ {fluentName} -> {legacyValue}");
                }
                catch (Exception ex)
                {
                    var innerEx = ex.InnerException ?? ex;
                    errors.Add($"Fluent->Legacy: {fluentName} failed");
                    
                    // Suggest normalized match
                    var normalized = Normalize(fluentName);
                    var possibleMatch = legacyNames.FirstOrDefault(l => 
                        Normalize(l).Equals(normalized, StringComparison.OrdinalIgnoreCase));
                    
                    if (possibleMatch != null)
                    {
                        suggestions.Add($"  Suggestion: Map {fluentName} -> {possibleMatch} (normalized match)");
                    }
                }
            }

            // Test Legacy -> Fluent conversion
            foreach (var legacyName in legacyNames)
            {
                try
                {
                    // Use reflection to test conversion
                    var testMethod = typeof(EnumConverterV2)
                        .GetMethod(nameof(EnumConverterV2.ToFluent))!
                        .MakeGenericMethod(fluentEnum);
                    
                    var legacyValue = Enum.Parse(legacyEnum, legacyName);
                    var fluentValue = testMethod.Invoke(null, new object[] { legacyValue, machineName });
                    
                    _output.WriteLine($"  ✓ {legacyName} <- {fluentValue}");
                }
                catch (Exception ex)
                {
                    var innerEx = ex.InnerException ?? ex;
                    errors.Add($"Legacy->Fluent: {legacyName} failed");
                    
                    // Suggest normalized match
                    var normalized = Normalize(legacyName);
                    var possibleMatch = fluentNames.FirstOrDefault(f => 
                        Normalize(f).Equals(normalized, StringComparison.OrdinalIgnoreCase));
                    
                    if (possibleMatch != null)
                    {
                        suggestions.Add($"  Suggestion: Map {legacyName} -> {possibleMatch} (normalized match)");
                    }
                }
            }

            // Report results
            if (errors.Any())
            {
                _output.WriteLine($"  ❌ FAILURES: {errors.Count}");
                foreach (var error in errors)
                {
                    _output.WriteLine($"    - {error}");
                }
                
                if (suggestions.Any())
                {
                    _output.WriteLine("  SUGGESTED MAPPINGS:");
                    foreach (var suggestion in suggestions)
                    {
                        _output.WriteLine(suggestion);
                    }
                }
                
                // Generate suggested Maps entry
                _output.WriteLine($"  Add to EnumConverterV2.Maps[\"{machineName}\"]:");
                foreach (var suggestion in suggestions)
                {
                    // Parse suggestion to extract mapping
                    if (suggestion.Contains("Map") && suggestion.Contains("->"))
                    {
                        var parts = suggestion.Split(new[] { "Map", "->" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var from = parts[0].Trim().Split(' ').Last();
                            var to = parts[1].Trim().Split(' ').First();
                            _output.WriteLine($"    [\"{from}\"] = \"{to}\",");
                        }
                    }
                }
                
                Assert.True(false, $"{machineName} has {errors.Count} enum conversion failures");
            }
            else
            {
                _output.WriteLine($"  ✓ All conversions successful");
                
                // Show auto-map diagnostics
                var diagnostics = EnumConverterV2.GetAutoMapDiagnostics(machineName, fluentEnum, legacyEnum);
                if (!string.IsNullOrEmpty(diagnostics))
                {
                    _output.WriteLine("  Auto-mappings used:");
                    foreach (var line in diagnostics.Split('\n').Take(5))
                    {
                        _output.WriteLine($"    {line}");
                    }
                }
            }
        }

        private string Normalize(string name)
        {
            return new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        /// <summary>
        /// Test that InitialStateResolver returns valid states for all machines
        /// </summary>
        [Theory]
        [MemberData(nameof(GetMachinesFromConfig))]
        public void InitialStateResolver_ReturnsValidState_ForAllMachines(string machineName)
        {
            var enumTypes = GetMachineEnumTypes(machineName);
            if (enumTypes?.FluentState == null || enumTypes?.LegacyState == null)
            {
                _output.WriteLine($"Skipping {machineName} - enum types not found");
                return;
            }

            // Test Fluent
            var resolveMethod = typeof(InitialStateResolver)
                .GetMethod(nameof(InitialStateResolver.ResolveOrDefault))!
                .MakeGenericMethod(enumTypes.FluentState);
            
            var fluentResolved = (string)resolveMethod.Invoke(null, new object[] { machineName, null })!;
            var fluentStates = Enum.GetNames(enumTypes.FluentState);
            
            Assert.Contains(fluentResolved, fluentStates);
            _output.WriteLine($"{machineName} Fluent: resolved to '{fluentResolved}'");

            // Test Legacy
            resolveMethod = typeof(InitialStateResolver)
                .GetMethod(nameof(InitialStateResolver.ResolveOrDefault))!
                .MakeGenericMethod(enumTypes.LegacyState);
            
            var legacyResolved = (string)resolveMethod.Invoke(null, new object[] { machineName, null })!;
            var legacyStates = Enum.GetNames(enumTypes.LegacyState);
            
            Assert.Contains(legacyResolved, legacyStates);
            _output.WriteLine($"{machineName} Legacy: resolved to '{legacyResolved}'");
        }

        private class MachineEnumTypes
        {
            public Type? FluentState { get; set; }
            public Type? LegacyState { get; set; }
            public Type? FluentTrigger { get; set; }
            public Type? LegacyTrigger { get; set; }
        }

        private MachineEnumTypes? GetMachineEnumTypes(string machineName)
        {
            // Prefer MachineTypeRegistry as the single source of truth
            if (MachineTypeRegistry.Types.TryGetValue(machineName, out var pair))
            {
                return new MachineEnumTypes
                {
                    FluentState = pair.FluentState,
                    LegacyState = pair.LegacyState,
                    FluentTrigger = pair.FluentTrigger,
                    LegacyTrigger = pair.LegacyTrigger
                };
            }

            // Fallback to hardcoded mappings for machines not in registry
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
