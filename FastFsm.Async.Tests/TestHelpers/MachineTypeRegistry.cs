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
            };

        public static Type GetStateType(string machine, Api api) => Types[machine].For(api, isState: true);
        public static Type GetTriggerType(string machine, Api api) => Types[machine].For(api, isState: false);
    }
}
