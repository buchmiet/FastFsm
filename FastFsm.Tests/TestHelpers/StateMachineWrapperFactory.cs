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
        /// Registry of machine types and their wrapper creators
        /// </summary>
        private static readonly Dictionary<string, Func<ApiType, string, IStateMachineTestWrapper>> _wrapperFactories = new()
        {
            ["CoreBenchmark"] = CreateCoreBenchmarkWrapper,
            ["GuardPermitted"] = CreateGuardPermittedWrapper,
            // Add more machine types as needed
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
        
        /// <summary>
        /// Creates a CoreBenchmark machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateCoreBenchmarkWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new CoreBenchmarkFluentWrapper(initialStateName),
                ApiType.Legacy => new CoreBenchmarkLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates a GuardPermitted machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateGuardPermittedWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new GuardPermittedFluentWrapper(initialStateName),
                ApiType.Legacy => new GuardPermittedLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Helper method to get state enum value for a specific API type
        /// </summary>
        public static object GetStateEnum(string machineType, ApiType apiType, string stateName)
        {
            return machineType switch
            {
                "CoreBenchmark" => apiType == ApiType.Fluent
                    ? EnumConverter.ConvertEnum(typeof(BenchmarkTests.BenchmarkState), stateName)
                    : EnumConverter.ConvertEnum(typeof(BenchmarkTestsLegacy.BenchmarkState), stateName),
                    
                "GuardPermitted" => EnumConverter.ConvertEnum(typeof(Features.Core.State), stateName),
                
                _ => throw new NotSupportedException($"Machine type '{machineType}' is not supported")
            };
        }
        
        /// <summary>
        /// Helper method to get trigger enum value for a specific API type
        /// </summary>
        public static object GetTriggerEnum(string machineType, ApiType apiType, string triggerName)
        {
            return machineType switch
            {
                "CoreBenchmark" => apiType == ApiType.Fluent
                    ? EnumConverter.ConvertEnum(typeof(BenchmarkTests.BenchmarkTrigger), triggerName)
                    : EnumConverter.ConvertEnum(typeof(BenchmarkTestsLegacy.BenchmarkTrigger), triggerName),
                    
                "GuardPermitted" => EnumConverter.ConvertEnum(typeof(Features.Core.Trigger), triggerName),
                
                _ => throw new NotSupportedException($"Machine type '{machineType}' is not supported")
            };
        }
    }
}