using System;
using System.Collections.Generic;
using FastFsm.Async.Tests.Features.Hsm.Runtime;
using FastFsm.Async.Tests.Features.Payload;
using FastFsm.Async.Tests.Features.Cancellation;
using FastFsm.Async.Tests.Features.Exceptions;
using FastFsm.Async.Tests.Features.Extensions;
using FastFsm.Async.Tests.Features.Concurrency;
using FastFsm.Async.Tests.Features.Core;

namespace FastFsm.Async.Tests.TestHelpers
{
    public static class MachineTypeRegistry
    {
        public enum Api { Fluent, Legacy }

        public sealed class EnumTypePair
        {
            public Type FluentState { get; }
            public Type LegacyState { get; }
            public Type FluentTrigger { get; }
            public Type LegacyTrigger { get; }

            public EnumTypePair(Type fluentState, Type legacyState, Type fluentTrigger, Type legacyTrigger)
            { FluentState = fluentState; LegacyState = legacyState; FluentTrigger = fluentTrigger; LegacyTrigger = legacyTrigger; }

            public Type For(Api api, bool isState) => (api, isState) switch
            {
                (Api.Fluent, true) => FluentState,
                (Api.Legacy, true) => LegacyState,
                (Api.Fluent, false) => FluentTrigger,
                (Api.Legacy, false) => LegacyTrigger,
                _ => throw new ArgumentException("Invalid combination")
            };

            public bool UsesSameEnums => FluentState == LegacyState && FluentTrigger == LegacyTrigger;
        }

        // HSM Runtime machines — both APIs use the same enums defined in the test containers
        public static readonly IReadOnlyDictionary<string, EnumTypePair> Types =
            new Dictionary<string, EnumTypePair>(StringComparer.Ordinal)
            {
                ["InitialChild"] = new EnumTypePair(typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.T), typeof(AsyncInitialChildTests.T)),
                ["ShallowHistory"] = new EnumTypePair(typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.T), typeof(AsyncShallowHistoryTests.T)),
                ["DeepHistory"] = new EnumTypePair(typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.T), typeof(AsyncDeepHistoryTests.T)),
                ["Internal"] = new EnumTypePair(typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.T), typeof(AsyncInternalTransitionTests.T)),
                ["Priority"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["ChildOverrides"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["SourceOrderTie"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["Inheritance"] = new EnumTypePair(typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.T), typeof(AsyncInheritanceAndIntrospectionTests.T)),

                // Payload machines (attribute-only for now)
                ["BasicPayload"] = new EnumTypePair(typeof(AsyncPayloadStates), typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers), typeof(AsyncPayloadTriggers)),
                ["OverloadedPayload"] = new EnumTypePair(typeof(AsyncPayloadStates), typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers), typeof(AsyncPayloadTriggers)),
                ["ExceptionPayload"] = new EnumTypePair(typeof(AsyncPayloadStates), typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers), typeof(AsyncPayloadTriggers)),
                ["CanFirePayload"] = new EnumTypePair(typeof(AsyncPayloadStates), typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers), typeof(AsyncPayloadTriggers)),
                ["ConcurrentPayload"] = new EnumTypePair(typeof(AsyncPayloadStates), typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers), typeof(AsyncPayloadTriggers)),
                ["InitialOnEntryPayload"] = new EnumTypePair(typeof(AsyncPayloadStates), typeof(AsyncPayloadStates), typeof(AsyncPayloadTriggers), typeof(AsyncPayloadTriggers)),
                ["MultiPayload"] = new EnumTypePair(typeof(MultiPayloadStates), typeof(MultiPayloadStates), typeof(MultiPayloadTriggers), typeof(MultiPayloadTriggers)),

                // Cancellation machines
                ["BasicToken"] = new EnumTypePair(typeof(TokenTestState), typeof(TokenTestState), typeof(TokenTestTrigger), typeof(TokenTestTrigger)),
                ["OptionalToken"] = new EnumTypePair(typeof(TokenTestState), typeof(TokenTestState), typeof(TokenTestTrigger), typeof(TokenTestTrigger)),
                ["Cancellation"] = new EnumTypePair(typeof(TokenTestState), typeof(TokenTestState), typeof(TokenTestTrigger), typeof(TokenTestTrigger)),
                ["MixedToken"] = new EnumTypePair(typeof(TokenTestState), typeof(TokenTestState), typeof(TokenTestTrigger), typeof(TokenTestTrigger)),

                // Exceptions machines
                ["OnEntryContinue"] = new EnumTypePair(typeof(ExceptionTestStates), typeof(ExceptionTestStates), typeof(ExceptionTestTriggers), typeof(ExceptionTestTriggers)),
                ["ActionPropagate"] = new EnumTypePair(typeof(ExceptionTestStates), typeof(ExceptionTestStates), typeof(ExceptionTestTriggers), typeof(ExceptionTestTriggers)),
                ["GuardException"] = new EnumTypePair(typeof(ExceptionTestStates), typeof(ExceptionTestStates), typeof(ExceptionTestTriggers), typeof(ExceptionTestTriggers)),
                ["CancellationPropagation"] = new EnumTypePair(typeof(ExceptionTestStates), typeof(ExceptionTestStates), typeof(ExceptionTestTriggers), typeof(ExceptionTestTriggers)),
                ["AsyncHandler"] = new EnumTypePair(typeof(ExceptionTestStates), typeof(ExceptionTestStates), typeof(ExceptionTestTriggers), typeof(ExceptionTestTriggers)),
                ["ExceptionContextCapture"] = new EnumTypePair(typeof(ExceptionTestStates), typeof(ExceptionTestStates), typeof(ExceptionTestTriggers), typeof(ExceptionTestTriggers)),

                // Extensions machines
                ["ExtensionsSuccess"] = new EnumTypePair(typeof(AState), typeof(AState), typeof(ATrigger), typeof(ATrigger)),
                ["ExtensionsFail"] = new EnumTypePair(typeof(AState), typeof(AState), typeof(ATrigger), typeof(ATrigger)),

                // Concurrency/Core
                ["RcMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Concurrency.RcStates), typeof(FastFsm.Async.Tests.Features.Concurrency.RcStates), typeof(FastFsm.Async.Tests.Features.Concurrency.RcTriggers), typeof(FastFsm.Async.Tests.Features.Concurrency.RcTriggers)),
                ["SimpleAsync"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Core.AsyncStates), typeof(FastFsm.Async.Tests.Features.Core.AsyncStates), typeof(FastFsm.Async.Tests.Features.Core.AsyncTriggers), typeof(FastFsm.Async.Tests.Features.Core.AsyncTriggers)),

                // Additional base-name aliases for full parity
                ["InitialChildMachine"] = new EnumTypePair(typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.T), typeof(AsyncInitialChildTests.T)),
                ["ShallowHistoryMachine"] = new EnumTypePair(typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.T), typeof(AsyncShallowHistoryTests.T)),
                ["DeepHistoryMachine"] = new EnumTypePair(typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.T), typeof(AsyncDeepHistoryTests.T)),
                ["InternalMachine"] = new EnumTypePair(typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.T), typeof(AsyncInternalTransitionTests.T)),
                ["PriorityMachine"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["ChildOverridesMachine"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["SourceOrderTieMachine"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["InheritanceMachine"] = new EnumTypePair(typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.T), typeof(AsyncInheritanceAndIntrospectionTests.T)),

                ["BasicAsyncPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)),
                ["OverloadedAsyncMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)),
                ["ExceptionAsyncPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)),
                ["CanFireAsyncPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)),
                ["ConcurrentAsyncPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)),
                ["InitialOnEntryAsyncPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)),
                ["MultiPayloadAsyncMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Payload.MultiPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.MultiPayloadStates), typeof(FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers), typeof(FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)),

                ["BasicTokenMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)),
                ["OptionalTokenMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)),
                ["CancellationMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)),
                ["MixedTokenMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)),

                ["OnEntryContinueMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)),
                ["ActionPropagateMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)),
                ["GuardExceptionMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)),
                ["CancellationPropagationMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)),
                ["AsyncHandlerMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)),
                ["ExceptionContextCaptureMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)),

                ["AsyncHookOrderMachineSuccess"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Extensions.AState), typeof(FastFsm.Async.Tests.Features.Extensions.AState), typeof(FastFsm.Async.Tests.Features.Extensions.ATrigger), typeof(FastFsm.Async.Tests.Features.Extensions.ATrigger)),
                ["AsyncHookOrderMachineFail"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Extensions.AState), typeof(FastFsm.Async.Tests.Features.Extensions.AState), typeof(FastFsm.Async.Tests.Features.Extensions.ATrigger), typeof(FastFsm.Async.Tests.Features.Extensions.ATrigger)),
                ["AsyncExtensionsMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Extensions.ExtState), typeof(FastFsm.Async.Tests.Features.Extensions.ExtState), typeof(FastFsm.Async.Tests.Features.Extensions.ExtTrigger), typeof(FastFsm.Async.Tests.Features.Extensions.ExtTrigger)),

                ["SimpleAsyncMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Core.AsyncStates), typeof(FastFsm.Async.Tests.Features.Core.AsyncStates), typeof(FastFsm.Async.Tests.Features.Core.AsyncTriggers), typeof(FastFsm.Async.Tests.Features.Core.AsyncTriggers)),
                ["SimpleCancellationMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.SimpleStates), typeof(FastFsm.Async.Tests.Features.Cancellation.SimpleStates), typeof(FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers), typeof(FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)),
                ["SpecificationComplianceMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.SpecStates), typeof(FastFsm.Async.Tests.Features.Cancellation.SpecStates), typeof(FastFsm.Async.Tests.Features.Cancellation.SpecTriggers), typeof(FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)),
                ["TokenMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenStates), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenStates), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTriggers), typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)),
                ["PayloadMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Cancellation.PayloadStates), typeof(FastFsm.Async.Tests.Features.Cancellation.PayloadStates), typeof(FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers), typeof(FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)),
                ["TinyAsyncHsm"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), typeof(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), typeof(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T), typeof(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)),
                ["ExceptionAsyncMachine"] = new EnumTypePair(typeof(FastFsm.Async.Tests.Features.Exceptions.ExStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExStates), typeof(FastFsm.Async.Tests.Features.Exceptions.ExTriggers), typeof(FastFsm.Async.Tests.Features.Exceptions.ExTriggers)),
            };

        public static Type GetStateType(string machine, Api api) => Types[machine].For(api, isState: true);
        public static Type GetTriggerType(string machine, Api api) => Types[machine].For(api, isState: false);
    }
}
