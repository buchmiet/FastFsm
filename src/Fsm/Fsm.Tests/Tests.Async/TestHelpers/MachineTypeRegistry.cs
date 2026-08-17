using System;
using System.Collections.Generic;
using Tests.Async.Features.Hsm.Runtime;
using Tests.Async.Features.Payload;
using Tests.Async.Features.Cancellation;
using Tests.Async.Features.Exceptions;
using Tests.Async.Features.Extensions;
using Tests.Async.Features.Concurrency;
using Tests.Async.Features.Core;

namespace Tests.Async.TestHelpers
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
                ["RcMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Concurrency.RcStates), typeof(Tests.Async.Features.Concurrency.RcStates), typeof(Tests.Async.Features.Concurrency.RcTriggers), typeof(Tests.Async.Features.Concurrency.RcTriggers)),
                ["SimpleAsync"] = new EnumTypePair(typeof(Tests.Async.Features.Core.AsyncStates), typeof(Tests.Async.Features.Core.AsyncStates), typeof(Tests.Async.Features.Core.AsyncTriggers), typeof(Tests.Async.Features.Core.AsyncTriggers)),

                // Additional base-name aliases for full parity
                ["InitialChildMachine"] = new EnumTypePair(typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.T), typeof(AsyncInitialChildTests.T)),
                ["ShallowHistoryMachine"] = new EnumTypePair(typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.T), typeof(AsyncShallowHistoryTests.T)),
                ["DeepHistoryMachine"] = new EnumTypePair(typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.T), typeof(AsyncDeepHistoryTests.T)),
                ["InternalMachine"] = new EnumTypePair(typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.T), typeof(AsyncInternalTransitionTests.T)),
                ["PriorityMachine"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["ChildOverridesMachine"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["SourceOrderTieMachine"] = new EnumTypePair(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), typeof(AsyncResolutionOrderTests.T)),
                ["InheritanceMachine"] = new EnumTypePair(typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.T), typeof(AsyncInheritanceAndIntrospectionTests.T)),

                ["BasicAsyncPayloadMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers)),
                ["OverloadedAsyncMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers)),
                ["ExceptionAsyncPayloadMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers)),
                ["CanFireAsyncPayloadMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers)),
                ["ConcurrentAsyncPayloadMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers)),
                ["InitialOnEntryAsyncPayloadMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadStates), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers), typeof(Tests.Async.Features.Payload.AsyncPayloadTriggers)),
                ["MultiPayloadAsyncMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Payload.MultiPayloadStates), typeof(Tests.Async.Features.Payload.MultiPayloadStates), typeof(Tests.Async.Features.Payload.MultiPayloadTriggers), typeof(Tests.Async.Features.Payload.MultiPayloadTriggers)),

                ["BasicTokenMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger)),
                ["OptionalTokenMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger)),
                ["CancellationMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger)),
                ["MixedTokenMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestState), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger), typeof(Tests.Async.Features.Cancellation.TokenTestTrigger)),

                ["OnEntryContinueMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers)),
                ["ActionPropagateMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers)),
                ["GuardExceptionMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers)),
                ["CancellationPropagationMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers)),
                ["AsyncHandlerMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers)),
                ["ExceptionContextCaptureMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers), typeof(Tests.Async.Features.Exceptions.ExceptionTestTriggers)),

                ["AsyncHookOrderMachineSuccess"] = new EnumTypePair(typeof(Tests.Async.Features.Extensions.AState), typeof(Tests.Async.Features.Extensions.AState), typeof(Tests.Async.Features.Extensions.ATrigger), typeof(Tests.Async.Features.Extensions.ATrigger)),
                ["AsyncHookOrderMachineFail"] = new EnumTypePair(typeof(Tests.Async.Features.Extensions.AState), typeof(Tests.Async.Features.Extensions.AState), typeof(Tests.Async.Features.Extensions.ATrigger), typeof(Tests.Async.Features.Extensions.ATrigger)),
                ["AsyncExtensionsMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Extensions.ExtState), typeof(Tests.Async.Features.Extensions.ExtState), typeof(Tests.Async.Features.Extensions.ExtTrigger), typeof(Tests.Async.Features.Extensions.ExtTrigger)),

                ["SimpleAsyncMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Core.AsyncStates), typeof(Tests.Async.Features.Core.AsyncStates), typeof(Tests.Async.Features.Core.AsyncTriggers), typeof(Tests.Async.Features.Core.AsyncTriggers)),
                ["SimpleCancellationMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.SimpleStates), typeof(Tests.Async.Features.Cancellation.SimpleStates), typeof(Tests.Async.Features.Cancellation.SimpleTriggers), typeof(Tests.Async.Features.Cancellation.SimpleTriggers)),
                ["SpecificationComplianceMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.SpecStates), typeof(Tests.Async.Features.Cancellation.SpecStates), typeof(Tests.Async.Features.Cancellation.SpecTriggers), typeof(Tests.Async.Features.Cancellation.SpecTriggers)),
                ["TokenMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.TokenStates), typeof(Tests.Async.Features.Cancellation.TokenStates), typeof(Tests.Async.Features.Cancellation.TokenTriggers), typeof(Tests.Async.Features.Cancellation.TokenTriggers)),
                ["PayloadMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Cancellation.PayloadStates), typeof(Tests.Async.Features.Cancellation.PayloadStates), typeof(Tests.Async.Features.Cancellation.PayloadTriggers), typeof(Tests.Async.Features.Cancellation.PayloadTriggers)),
                ["TinyAsyncHsm"] = new EnumTypePair(typeof(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), typeof(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), typeof(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T), typeof(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)),
                ["ExceptionAsyncMachine"] = new EnumTypePair(typeof(Tests.Async.Features.Exceptions.ExStates), typeof(Tests.Async.Features.Exceptions.ExStates), typeof(Tests.Async.Features.Exceptions.ExTriggers), typeof(Tests.Async.Features.Exceptions.ExTriggers)),
            };

        public static Type GetStateType(string machine, Api api) => Types[machine].For(api, isState: true);
        public static Type GetTriggerType(string machine, Api api) => Types[machine].For(api, isState: false);
    }
}
