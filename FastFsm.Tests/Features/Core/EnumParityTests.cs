using System;
using System.Collections.Generic;
using System.Linq;
using FastFsm.Tests.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.Core
{
    /// <summary>
    /// Tests to ensure enum parity between Fluent and Legacy APIs for all machines
    /// </summary>
    public class EnumParityTests
    {
        private readonly ITestOutputHelper _output;

        public EnumParityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetMachineData()
        {
            foreach (var machine in MachineRegistry.GetCompleteMachines())
            {
                yield return new object[] { machine.Name, machine };
            }
        }

        [Theory]
        [MemberData(nameof(GetMachineData))]
        public void States_HaveFullParity(string machineName, MachineRegistry.MachineInfo machine)
        {
            if (machine.FluentStateType == null || machine.LegacyStateType == null)
            {
                _output.WriteLine($"Skipping {machineName}: Missing state type definitions");
                return;
            }

            // Use reflection to call ValidateEnumParity with the correct generic types
            var method = typeof(EnumConverterV2).GetMethod(nameof(EnumConverterV2.ValidateEnumParity))!;
            var genericMethod = method.MakeGenericMethod(machine.FluentStateType, machine.LegacyStateType);
            
            var parameters = new object[] { machineName, null! };
            var result = (bool)genericMethod.Invoke(null, parameters)!;
            var report = (string)parameters[1];
            
            _output.WriteLine(report);
            
            if (!result)
            {
                // For CI blocking, we want to fail with a clear message
                Assert.True(result, $"State enum parity check failed for {machineName}. See test output for details.");
            }
        }

        [Theory]
        [MemberData(nameof(GetMachineData))]
        public void Triggers_HaveFullParity(string machineName, MachineRegistry.MachineInfo machine)
        {
            if (machine.FluentTriggerType == null || machine.LegacyTriggerType == null)
            {
                _output.WriteLine($"Skipping {machineName}: Missing trigger type definitions");
                return;
            }

            // Use reflection to call ValidateEnumParity with the correct generic types
            var method = typeof(EnumConverterV2).GetMethod(nameof(EnumConverterV2.ValidateEnumParity))!;
            var genericMethod = method.MakeGenericMethod(machine.FluentTriggerType, machine.LegacyTriggerType);
            
            var parameters = new object[] { machineName, null! };
            var result = (bool)genericMethod.Invoke(null, parameters)!;
            var report = (string)parameters[1];
            
            _output.WriteLine(report);
            
            if (!result)
            {
                // For CI blocking, we want to fail with a clear message
                Assert.True(result, $"Trigger enum parity check failed for {machineName}. See test output for details.");
            }
        }

        [Fact]
        public void AllMachines_AreRegistered()
        {
            var allMachines = MachineRegistry.GetAllMachines().ToList();
            var completeMachines = MachineRegistry.GetCompleteMachines().ToList();
            var incompleteMachines = MachineRegistry.GetIncompleteMachines().ToList();
            
            _output.WriteLine($"Total machines registered: {allMachines.Count}");
            _output.WriteLine($"Complete machines: {completeMachines.Count}");
            _output.WriteLine($"Incomplete machines: {incompleteMachines.Count}");
            
            if (incompleteMachines.Any())
            {
                _output.WriteLine("");
                _output.WriteLine("Incomplete machine registrations (TODO):");
                foreach (var machine in incompleteMachines)
                {
                    var issues = new List<string>();
                    if (machine.FluentStateType == null) issues.Add("FluentStateType");
                    if (machine.LegacyStateType == null) issues.Add("LegacyStateType");
                    if (machine.FluentTriggerType == null) issues.Add("FluentTriggerType");
                    if (machine.LegacyTriggerType == null) issues.Add("LegacyTriggerType");
                    if (machine.WrapperFactory == null) issues.Add("WrapperFactory");
                    
                    _output.WriteLine($"  - {machine.Name}: Missing {string.Join(", ", issues)}");
                }
            }
            
            // This should eventually be Assert.Empty(incompleteMachines) when all are complete
            Assert.True(completeMachines.Count > 0, "At least some machines should be completely registered");
        }

        [Fact]
        public void EnumConverter_HandlesCommonMappings()
        {
            // Test some common mapping scenarios
            
            // Test 1: Same name mapping (CoreBenchmark states should match)
            var fluentStateA = Features.Performance.BenchmarkTests.BenchmarkState.A;
            var legacyStateA = EnumConverterV2.ToLegacy<Features.Performance.BenchmarkTestsLegacy.BenchmarkState>(
                fluentStateA, "CoreBenchmark");
            Assert.Equal("A", legacyStateA.ToString());
            
            // Test 2: Reverse mapping
            var backToFluent = EnumConverterV2.ToFluent<Features.Performance.BenchmarkTests.BenchmarkState>(
                legacyStateA, "CoreBenchmark");
            Assert.Equal(fluentStateA, backToFluent);
            
            // Test 3: TryConvert methods
            var success = EnumConverterV2.TryToLegacy<Features.Performance.BenchmarkTestsLegacy.BenchmarkTrigger>(
                Features.Performance.BenchmarkTests.BenchmarkTrigger.Next, "CoreBenchmark", out var legacyTrigger);
            Assert.True(success);
            Assert.Equal("Next", legacyTrigger.ToString());
        }

        [Fact]
        public void EnumConverter_ThrowsForInvalidMappings()
        {
            // Create a fake enum value that doesn't exist
            var invalidEnum = (Features.Performance.BenchmarkTests.BenchmarkState)999;
            
            var ex = Assert.Throws<InvalidOperationException>(() =>
                EnumConverterV2.ToLegacy<Features.Performance.BenchmarkTestsLegacy.BenchmarkState>(
                    invalidEnum, "CoreBenchmark"));
            
            Assert.Contains("Enum mapping failed", ex.Message);
            Assert.Contains("CoreBenchmark", ex.Message);
            Assert.Contains("Hint:", ex.Message);
        }
    }
}