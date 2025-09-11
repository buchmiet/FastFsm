using System;
using System.Linq;
using Xunit;
using FastFsm.Tests.TestHelpers;

namespace FastFsm.Tests.Features.Parity
{
    /// <summary>
    /// Tests to validate MatrixConfig entries are correctly configured
    /// </summary>
    public class MatrixConfigValidationTests
    {
        [Fact]
        public void AllMatrixEntries_HaveValidFactories()
        {
            foreach (var entry in MatrixConfig.MatrixEntries)
            {
                // Try to create both Fluent and Legacy wrappers
                var fluentWrapper = StateMachineWrapperFactory.Create(
                    entry.MachineName,
                    StateMachineWrapperFactory.ApiType.Fluent,
                    entry.InitialState);
                
                var legacyWrapper = StateMachineWrapperFactory.Create(
                    entry.MachineName,
                    StateMachineWrapperFactory.ApiType.Legacy,
                    entry.InitialState);
                
                // Verify wrappers are created
                Assert.NotNull(fluentWrapper);
                Assert.NotNull(legacyWrapper);
                
                // Verify they implement the right interface
                Assert.IsAssignableFrom<IStateMachineTestWrapper>(fluentWrapper);
                Assert.IsAssignableFrom<IStateMachineTestWrapper>(legacyWrapper);
            }
        }
        
        [Fact]
        public void AllMatrixEntries_HaveValidCapabilities()
        {
            foreach (var entry in MatrixConfig.MatrixEntries)
            {
                var fluentWrapper = StateMachineWrapperFactory.Create(
                    entry.MachineName,
                    StateMachineWrapperFactory.ApiType.Fluent,
                    entry.InitialState);
                
                var legacyWrapper = StateMachineWrapperFactory.Create(
                    entry.MachineName,
                    StateMachineWrapperFactory.ApiType.Legacy,
                    entry.InitialState);
                
                // Verify capabilities match
                Assert.Equal(fluentWrapper.Caps, legacyWrapper.Caps);
                
                // Verify capabilities match the entry
                Assert.True(entry.Capabilities == fluentWrapper.Caps, 
                    $"Machine {entry.MachineName} Fluent: Expected {entry.Capabilities}, got {fluentWrapper.Caps}");
                Assert.True(entry.Capabilities == legacyWrapper.Caps,
                    $"Machine {entry.MachineName} Legacy: Expected {entry.Capabilities}, got {legacyWrapper.Caps}");
            }
        }
        
        [Fact]
        public void AllMatrixEntries_CanBeStarted()
        {
            foreach (var entry in MatrixConfig.MatrixEntries)
            {
                var fluentWrapper = StateMachineWrapperFactory.Create(
                    entry.MachineName,
                    StateMachineWrapperFactory.ApiType.Fluent,
                    entry.InitialState);
                
                var legacyWrapper = StateMachineWrapperFactory.Create(
                    entry.MachineName,
                    StateMachineWrapperFactory.ApiType.Legacy,
                    entry.InitialState);
                
                // Verify both can be started without throwing
                fluentWrapper.Start();
                legacyWrapper.Start();
                
                // Verify CurrentState is not null after start
                Assert.NotNull(fluentWrapper.CurrentState);
                Assert.NotNull(legacyWrapper.CurrentState);
            }
        }
        
        [Fact]
        public void MatrixConfig_HasNoDuplicateMachineNames()
        {
            var machineNames = MatrixConfig.MatrixEntries.Select(e => e.MachineName).ToList();
            var uniqueNames = machineNames.Distinct().ToList();
            
            Assert.Equal(uniqueNames.Count, machineNames.Count);
        }
        
        [Fact]
        public void MatrixConfig_ExcludesPerformanceMachines()
        {
            // Performance/benchmark machines should not be in the matrix
            var performanceKeywords = new[] { "Benchmark", "Performance", "Perf" };
            
            foreach (var entry in MatrixConfig.MatrixEntries)
            {
                foreach (var keyword in performanceKeywords)
                {
                    Assert.DoesNotContain(keyword, entry.MachineName, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        
        [Fact]
        public void MatrixConfig_ContainsExpectedMachines()
        {
            // Verify key machines are present
            var expectedMachines = new[]
            {
                // "CoreBenchmark", - excluded as it's a performance test machine
                "GuardPermitted",
                "PayloadStateMachine",
                "FullMultiPayload",
                "InternalTransition",
                "ExceptionCallback",
                "SimpleParentChild",
                "DeepHistory",
                "ShallowHistory",
                "InitialChild"
            };
            
            var actualMachines = MatrixConfig.MatrixEntries.Select(e => e.MachineName).ToHashSet();
            
            foreach (var expected in expectedMachines)
            {
                Assert.Contains(expected, actualMachines);
            }
        }
    }
}