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
        /// Creates a PayloadStateMachine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreatePayloadStateMachineWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new PayloadStateMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new PayloadStateMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates a FullMultiPayload machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateFullMultiPayloadWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new MultiPayloadMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new MultiPayloadMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates an InternalTransition machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateInternalTransitionWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new InternalTransitionMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new InternalTransitionMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates an ExceptionCallback machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateExceptionCallbackWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new ExceptionCallbackMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new ExceptionCallbackMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates a SimpleParentChild machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateSimpleParentChildWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new SimpleParentChildMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new SimpleParentChildMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates a DeepHistory machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateDeepHistoryWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new DeepHistoryMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new DeepHistoryMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates a ShallowHistory machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateShallowHistoryWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new ShallowHistoryMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new ShallowHistoryMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates an InitialChild machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateInitialChildWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new InitialChildMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new InitialChildMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        /// <summary>
        /// Creates an InternalTransitionHsm machine wrapper
        /// </summary>
        private static IStateMachineTestWrapper CreateInternalTransitionHsmWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new InternalTransitionHsmMachineFluentWrapper(initialStateName),
                ApiType.Legacy => new InternalTransitionHsmMachineLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        // Callback machine wrappers
        private static IStateMachineTestWrapper CreateMultipleCallbacksWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new MultipleCallbacksFluentWrapper(initialStateName),
                ApiType.Legacy => new MultipleCallbacksLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateInitialStateWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new InitialStateFluentWrapper(initialStateName),
                ApiType.Legacy => new InitialStateLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateCallbackOrderWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new CallbackOrderFluentWrapper(initialStateName),
                ApiType.Legacy => new CallbackOrderLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateComplexCallbackWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new ComplexCallbackFluentWrapper(initialStateName),
                ApiType.Legacy => new ComplexCallbackLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateGuardedCallbackWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new GuardedCallbackFluentWrapper(initialStateName),
                ApiType.Legacy => new GuardedCallbackLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateSelfTransitionWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new SelfTransitionFluentWrapper(initialStateName),
                ApiType.Legacy => new SelfTransitionLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        // Edge case machine wrappers
        private static IStateMachineTestWrapper CreateCaseSensitiveWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new CaseSensitiveFluentWrapper(initialStateName),
                ApiType.Legacy => new CaseSensitiveLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateConflictingNamesWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new ConflictingNamesFluentWrapper(initialStateName),
                ApiType.Legacy => new ConflictingNamesLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateLongNameWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new LongNameFluentWrapper(initialStateName),
                ApiType.Legacy => new LongNameLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateInternalOnlyWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new InternalOnlyFluentWrapper(initialStateName),
                ApiType.Legacy => new InternalOnlyLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateUnreachableWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new UnreachableFluentWrapper(initialStateName),
                ApiType.Legacy => new UnreachableLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateSingleStateWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new SingleStateFluentWrapper(initialStateName),
                ApiType.Legacy => new SingleStateLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateFullOrderWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new FullOrderFluentWrapper(initialStateName),
                ApiType.Legacy => new FullOrderLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateUnicodeWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new UnicodeFluentWrapper(initialStateName),
                ApiType.Legacy => new UnicodeLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateNumericWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new NumericFluentWrapper(initialStateName),
                ApiType.Legacy => new NumericLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
        private static IStateMachineTestWrapper CreateKeywordStateWrapper(ApiType apiType, string initialStateName)
        {
            return apiType switch
            {
                ApiType.Fluent => new KeywordStateFluentWrapper(initialStateName),
                ApiType.Legacy => new KeywordStateLegacyWrapper(initialStateName),
                _ => throw new ArgumentException($"Unknown API type: {apiType}")
            };
        }
        
    }
}