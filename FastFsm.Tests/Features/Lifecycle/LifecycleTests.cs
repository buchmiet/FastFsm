using FastFsm.Tests.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FastFsm.Tests.Features.Performance;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;

namespace FastFsm.Tests.Features.Lifecycle
{
    public class LifecycleTests
    {
        public enum ApiType { Fluent, Legacy }

        private object CreateMachine(ApiType apiType, object initialState)
        {
            if (apiType == ApiType.Fluent)
                return new CoreBenchmarkMachineFluent((BenchmarkState)initialState);
            else
            {
                // Convert to Legacy enum
                var legacyState = (BenchmarkTestsLegacy.BenchmarkState)Enum.Parse(
                    typeof(BenchmarkTestsLegacy.BenchmarkState), 
                    initialState.ToString());
                return new CoreBenchmarkMachineLegacy(legacyState);
            }
        }

        private object GetBenchmarkState(ApiType apiType, string stateName)
        {
            return apiType == ApiType.Fluent
                ? Enum.Parse(typeof(BenchmarkState), stateName)
                : Enum.Parse(typeof(BenchmarkTestsLegacy.BenchmarkState), stateName);
        }

        private object GetBenchmarkTrigger(ApiType apiType, string triggerName)
        {
            return apiType == ApiType.Fluent
                ? Enum.Parse(typeof(BenchmarkTrigger), triggerName)
                : Enum.Parse(typeof(BenchmarkTestsLegacy.BenchmarkTrigger), triggerName);
        }
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Machine_Throws_Before_Start(ApiType apiType)
        {
            var stateA = GetBenchmarkState(apiType, "A");
            var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            
            dynamic machine = CreateMachine(apiType, stateA);

            // TryFire without Start() should throw
            Assert.Throws<InvalidOperationException>(
                () => machine.TryFire(triggerNext));
        }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Machine_Works_After_Start(ApiType apiType)
        {
            var stateA = GetBenchmarkState(apiType, "A");
            var stateB = GetBenchmarkState(apiType, "B");
            var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            
            dynamic machine = CreateMachine(apiType, stateA);
            machine.Start();

            Assert.True(machine.TryFire(triggerNext));
            Assert.Equal(stateB, machine.CurrentState);
        }
    }
}
