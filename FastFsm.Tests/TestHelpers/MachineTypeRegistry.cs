using System;
using System.Collections.Generic;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Features.EdgeCases;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Central registry of enum types for all state machines
    /// This is the single source of truth for which enums each machine uses
    /// </summary>
    public static class MachineTypeRegistry
    {
        /// <summary>
        /// Machine name -> type pair (state/trigger per API)
        /// </summary>
        public static readonly IReadOnlyDictionary<string, EnumTypePair> Types =
            new Dictionary<string, EnumTypePair>(StringComparer.Ordinal)
            {
                // ====== SHARED ENUMS (same type for both APIs) ======
                
                // GuardPermitted - uses local enums from GuardPermittedTriggersTests
                ["GuardPermitted"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Core.State),   // FluentState
                    typeof(FastFsm.Tests.Features.Core.State),   // LegacyState (SAME!)
                    typeof(FastFsm.Tests.Features.Core.Trigger), // FluentTrigger
                    typeof(FastFsm.Tests.Features.Core.Trigger)  // LegacyTrigger (SAME!)
                ),

                // InternalTransition - uses shared StateCallbackTests enums
                ["InternalTransition"] = new EnumTypePair(
                    typeof(StateCallbackTests.InternalState),
                    typeof(StateCallbackTests.InternalState),    // SAME!
                    typeof(StateCallbackTests.InternalTrigger),
                    typeof(StateCallbackTests.InternalTrigger)   // SAME!
                ),
                
                // ExceptionCallback - uses shared StateCallbackTests enums
                ["ExceptionCallback"] = new EnumTypePair(
                    typeof(StateCallbackTests.ExceptionState),
                    typeof(StateCallbackTests.ExceptionState),   // SAME!
                    typeof(StateCallbackTests.ExceptionTrigger),
                    typeof(StateCallbackTests.ExceptionTrigger)  // SAME!
                ),
                
                // CallbackOrder - uses shared StateCallbackTests enums
                ["CallbackOrder"] = new EnumTypePair(
                    typeof(StateCallbackTests.CallbackState),
                    typeof(StateCallbackTests.CallbackState),    // SAME!
                    typeof(StateCallbackTests.CallbackTrigger),
                    typeof(StateCallbackTests.CallbackTrigger)   // SAME!
                ),
                
                // ComplexCallback - uses shared StateCallbackTests enums
                ["ComplexCallback"] = new EnumTypePair(
                    typeof(StateCallbackTests.ComplexCallbackState),
                    typeof(StateCallbackTests.ComplexCallbackState),    // SAME!
                    typeof(StateCallbackTests.ComplexCallbackTrigger),
                    typeof(StateCallbackTests.ComplexCallbackTrigger)   // SAME!
                ),
                
                // GuardedCallback - uses shared StateCallbackTests enums
                ["GuardedCallback"] = new EnumTypePair(
                    typeof(StateCallbackTests.GuardedState),
                    typeof(StateCallbackTests.GuardedState),     // SAME!
                    typeof(StateCallbackTests.GuardedTrigger),
                    typeof(StateCallbackTests.GuardedTrigger)    // SAME!
                ),
                
                // InitialState - uses shared StateCallbackTests enums
                ["InitialState"] = new EnumTypePair(
                    typeof(StateCallbackTests.InitialState),
                    typeof(StateCallbackTests.InitialState),     // SAME!
                    typeof(StateCallbackTests.InitialTrigger),
                    typeof(StateCallbackTests.InitialTrigger)    // SAME!
                ),
                
                // MultipleCallbacks - uses shared StateCallbackTests enums
                ["MultipleCallbacks"] = new EnumTypePair(
                    typeof(StateCallbackTests.MultiState),
                    typeof(StateCallbackTests.MultiState),       // SAME!
                    typeof(StateCallbackTests.MultiTrigger),
                    typeof(StateCallbackTests.MultiTrigger)      // SAME!
                ),
                
                // SelfTransition - uses shared StateCallbackTests enums
                ["SelfTransition"] = new EnumTypePair(
                    typeof(StateCallbackTests.SelfState),
                    typeof(StateCallbackTests.SelfState),        // SAME!
                    typeof(StateCallbackTests.SelfTrigger),
                    typeof(StateCallbackTests.SelfTrigger)       // SAME!
                ),
                
                // ====== DIFFERENT ENUMS (need actual conversion) ======
                
                // CoreBenchmark - different namespaces
                ["CoreBenchmark"] = new EnumTypePair(
                    typeof(BenchmarkTests.BenchmarkState),
                    typeof(BenchmarkTestsLegacy.BenchmarkState), // Different namespace
                    typeof(BenchmarkTests.BenchmarkTrigger),
                    typeof(BenchmarkTestsLegacy.BenchmarkTrigger) // Different namespace
                ),
                
                // BasicBenchmark - different namespaces
                ["BasicBenchmark"] = new EnumTypePair(
                    typeof(BenchmarkTests.BenchmarkState),
                    typeof(BenchmarkTestsLegacy.BenchmarkState),
                    typeof(BenchmarkTests.BenchmarkTrigger),
                    typeof(BenchmarkTestsLegacy.BenchmarkTrigger)
                ),
                
                // NoGuardBenchmark - different namespaces
                ["NoGuardBenchmark"] = new EnumTypePair(
                    typeof(BenchmarkTests.BenchmarkState),
                    typeof(BenchmarkTestsLegacy.BenchmarkState),
                    typeof(BenchmarkTests.BenchmarkTrigger),
                    typeof(BenchmarkTestsLegacy.BenchmarkTrigger)
                ),
                
                // WithGuardBenchmark - different namespaces
                ["WithGuardBenchmark"] = new EnumTypePair(
                    typeof(BenchmarkTests.BenchmarkState),
                    typeof(BenchmarkTestsLegacy.BenchmarkState),
                    typeof(BenchmarkTests.BenchmarkTrigger),
                    typeof(BenchmarkTestsLegacy.BenchmarkTrigger)
                ),
                
                // ====== PAYLOAD MACHINES ======
                
                // PayloadStateMachine - uses TestState/TestTrigger from Machines namespace
                ["PayloadStateMachine"] = new EnumTypePair(
                    typeof(TestState),
                    typeof(TestState),     // SAME!
                    typeof(TestTrigger),
                    typeof(TestTrigger)    // SAME!
                ),
                
                // FullMultiPayload - uses MultiState/MultiTrigger from Payload namespace
                ["FullMultiPayload"] = new EnumTypePair(
                    typeof(MultiState),
                    typeof(MultiState),     // SAME!
                    typeof(MultiTrigger),
                    typeof(MultiTrigger)    // SAME!
                ),
                
                // FullOrder - uses OrderState/OrderTrigger from Machines namespace
                ["FullOrder"] = new EnumTypePair(
                    typeof(Machines.OrderState),
                    typeof(Machines.OrderState),     // SAME!
                    typeof(Machines.OrderTrigger),
                    typeof(Machines.OrderTrigger)    // SAME!
                ),
                
                // ====== EDGE CASES ======
                
                // CaseSensitive
                ["CaseSensitive"] = new EnumTypePair(
                    typeof(NameCollisionTests.CaseSensitiveState),
                    typeof(NameCollisionTests.CaseSensitiveState),  // SAME!
                    typeof(NameCollisionTests.CaseSensitiveTrigger),
                    typeof(NameCollisionTests.CaseSensitiveTrigger) // SAME!
                ),
                
                // ConflictingNames
                ["ConflictingNames"] = new EnumTypePair(
                    typeof(NameCollisionTests.ConflictState),
                    typeof(NameCollisionTests.ConflictState),   // SAME!
                    typeof(NameCollisionTests.ConflictTrigger),
                    typeof(NameCollisionTests.ConflictTrigger)  // SAME!
                ),
                
                // KeywordState
                ["KeywordState"] = new EnumTypePair(
                    typeof(NameCollisionTests.KeywordState),
                    typeof(NameCollisionTests.KeywordState),    // SAME!
                    typeof(NameCollisionTests.KeywordTrigger),
                    typeof(NameCollisionTests.KeywordTrigger)   // SAME!
                ),
                
                // LongName
                ["LongName"] = new EnumTypePair(
                    typeof(NameCollisionTests.LongNameState),
                    typeof(NameCollisionTests.LongNameState),   // SAME!
                    typeof(NameCollisionTests.LongNameTrigger),
                    typeof(NameCollisionTests.LongNameTrigger)  // SAME!
                ),
                
                // Numeric
                ["Numeric"] = new EnumTypePair(
                    typeof(NameCollisionTests.NumericState),
                    typeof(NameCollisionTests.NumericState),    // SAME!
                    typeof(NameCollisionTests.NumericTrigger),
                    typeof(NameCollisionTests.NumericTrigger)   // SAME!
                ),
                
                // Unicode
                ["Unicode"] = new EnumTypePair(
                    typeof(NameCollisionTests.UnicodeState),
                    typeof(NameCollisionTests.UnicodeState),    // SAME!
                    typeof(NameCollisionTests.UnicodeTrigger),
                    typeof(NameCollisionTests.UnicodeTrigger)   // SAME!
                ),
                
                // ====== EMPTY MACHINE TESTS ======
                
                // SingleState
                ["SingleState"] = new EnumTypePair(
                    typeof(EmptyMachineTests.SingleState),
                    typeof(EmptyMachineTests.SingleState),      // SAME!
                    typeof(EmptyMachineTests.SingleTrigger),
                    typeof(EmptyMachineTests.SingleTrigger)     // SAME!
                ),
                
                // InternalOnly
                ["InternalOnly"] = new EnumTypePair(
                    typeof(EmptyMachineTests.InternalOnlyState),
                    typeof(EmptyMachineTests.InternalOnlyState),   // SAME!
                    typeof(EmptyMachineTests.InternalOnlyTrigger),
                    typeof(EmptyMachineTests.InternalOnlyTrigger)  // SAME!
                ),
                
                // Unreachable
                ["Unreachable"] = new EnumTypePair(
                    typeof(EmptyMachineTests.UnreachableState),
                    typeof(EmptyMachineTests.UnreachableState),    // SAME!
                    typeof(EmptyMachineTests.UnreachableTrigger),
                    typeof(EmptyMachineTests.UnreachableTrigger)   // SAME!
                ),
                
                // Note: HSM machines would be added here when ready
                
                // ====== HSM MACHINES (LOCAL ENUMS) ======
                ["SimpleParentChild"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T) // SAME!
                ),

                ["DeepHistory"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.DeepHistoryTestsFluent.T) // SAME!
                ),

                ["ShallowHistory"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.ShallowHistoryTestsFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.ShallowHistoryTestsFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.ShallowHistoryTestsFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.ShallowHistoryTestsFluent.T) // SAME!
                ),

                ["InitialChild"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InitialChildTestsFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InitialChildTestsFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InitialChildTestsFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InitialChildTestsFluent.T) // SAME!
                ),
                
                ["InternalTransitionHsm"] = new EnumTypePair(
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InternalTransitionTestsFluent.S),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InternalTransitionTestsFluent.S),  // SAME!
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InternalTransitionTestsFluent.T),
                    typeof(FastFsm.Tests.Features.Hsm.Runtime.InternalTransitionTestsFluent.T) // SAME!
                ),
            };
            
        /// <summary>
        /// Get the state enum type for a machine and API
        /// </summary>
        public static Type GetStateType(string machineName, Api api)
        {
            if (!Types.TryGetValue(machineName, out var pair))
                throw new ArgumentException($"Unknown machine: {machineName}");
            return pair.For(api, isState: true);
        }
        
        /// <summary>
        /// Get the trigger enum type for a machine and API
        /// </summary>
        public static Type GetTriggerType(string machineName, Api api)
        {
            if (!Types.TryGetValue(machineName, out var pair))
                throw new ArgumentException($"Unknown machine: {machineName}");
            return pair.For(api, isState: false);
        }
        
        /// <summary>
        /// Check if a machine uses the same enums for both APIs
        /// </summary>
        public static bool UsesSameEnums(string machineName)
        {
            if (!Types.TryGetValue(machineName, out var pair))
                return false;
            return pair.UsesSameEnums;
        }
    }
}
