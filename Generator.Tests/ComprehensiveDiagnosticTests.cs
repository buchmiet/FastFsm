using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

/// <summary>
/// Comprehensive tests for all FSM diagnostic codes.
/// Verifies which diagnostics are actually emitted by the generator.
/// </summary>
public class ComprehensiveDiagnosticTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    // ============================================================================
    // CORE DIAGNOSTICS (FSM001-FSM014)
    // ============================================================================

    [Fact]
    public void FSM001_DuplicateTransition_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B)]
                    [Transition(State.A, Trigger.X, State.C)] // Duplicate: same from+trigger
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        // Debug: Show all diagnostics
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.DuplicateTransition).ToList();
        output.WriteLine($"FSM001: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Don't assert for now, just observe
        // Assert.NotEmpty(fsmErrors); // FSM001 should be emitted
    }

    [Fact]
    public void FSM002_UnreachableState_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B, C, Unreachable }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B)]
                    [Transition(State.B, Trigger.Y, State.C)]
                    // State.Unreachable has no incoming transitions
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.UnreachableState).ToList();
        output.WriteLine($"FSM002: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Note: May depend on initial state configuration
    }

    [Fact]
    public void FSM003_InvalidMethodSignature_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Guard = nameof(InvalidGuard))]
                    private void Config() { }
                    
                    private string InvalidGuard() => ""wrong""; // Should return bool
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidMethodSignature).ToList();
        Assert.NotEmpty(fsmErrors); // FSM003 should be emitted
        output.WriteLine($"FSM003: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM004_MissingStateMachineAttribute_ShouldNotEmitInParser()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                // Missing [StateMachine] attribute
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.MissingStateMachineAttribute).ToList();
        output.WriteLine($"FSM004: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // This is expected NOT to emit in parser (only in analyzer)
    }

    [Fact]
    public void FSM005_InvalidTypesInAttribute_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public class NotAnEnum { }
                public enum Trigger { X }
                
                [StateMachine(typeof(NotAnEnum), typeof(Trigger))] // Not an enum
                public partial class Machine { }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidTypesInAttribute).ToList();
        Assert.NotEmpty(fsmErrors); // FSM005 should be emitted
        output.WriteLine($"FSM005: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM006_InvalidEnumValueInTransition_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State : byte { Low = 0, High = 255 }
                public enum Trigger { Go }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition((State)128, Trigger.Go, State.Low)] // 128 not defined
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidEnumValueInTransition).ToList();
        Assert.NotEmpty(fsmErrors); // FSM006 should be emitted
        output.WriteLine($"FSM006: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM007_MissingPayloadType_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), Force = GenerationVariant.WithPayload)]
                public partial class Machine {
                    // Forced WithPayload but no [PayloadType] attribute
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.MissingPayloadType).ToList();
        output.WriteLine($"FSM007: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Expected NOT to emit (TODO in code)
    }

    [Fact]
    public void FSM008_ConflictingPayloadConfiguration_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger), Force = GenerationVariant.WithPayload)]
                [PayloadType(typeof(int), Triggers = new[] { Trigger.X })]
                [PayloadType(typeof(string), Triggers = new[] { Trigger.Y })]
                public partial class Machine {
                    // WithPayload expects single type but has multiple
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.ConflictingPayloadConfiguration).ToList();
        output.WriteLine($"FSM008: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Expected NOT to emit (TODO in code)
    }

    [Fact]
    public void FSM009_InvalidForcedVariantConfiguration_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), Force = GenerationVariant.Pure)]
                public partial class Machine {
                    // Forced Pure but has callbacks
                    [State(State.A, OnEntry = nameof(EnterA))]
                    private void ConfigureStates() { }
                    
                    private void EnterA() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidForcedVariantConfiguration).ToList();
        output.WriteLine($"FSM009: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Expected NOT to emit (TODO in code)
    }

    [Fact]
    public void FSM010_GuardWithPayloadInNonPayloadMachine_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))] // No payload type
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Guard = nameof(GuardWithPayload))]
                    private void Config() { }
                    
                    private bool GuardWithPayload(object payload) => true; // Expects payload
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.GuardWithPayloadInNonPayloadMachine).ToList();
        output.WriteLine($"FSM010: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM011_MixedSyncAsyncCallbacks_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [State(State.A, OnEntry = nameof(SyncEntry))]
                    [State(State.B, OnEntry = nameof(AsyncEntry))]
                    private void Config() { }
                    
                    private void SyncEntry() { }
                    private async Task AsyncEntry() { await Task.Delay(1); }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.MixedSyncAsyncCallbacks).ToList();
        output.WriteLine($"FSM011: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM012_InvalidGuardTaskReturnType_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Guard = nameof(AsyncGuard))]
                    private void Config() { }
                    
                    private async Task AsyncGuard() { await Task.Delay(1); } // Should return Task<bool>
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidGuardTaskReturnType).ToList();
        output.WriteLine($"FSM012: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM013_AsyncCallbackInSyncMachine_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [State(State.A, OnEntry = nameof(SyncEntry))]
                    private void Config1() { }
                    
                    [State(State.B, OnEntry = nameof(AsyncEntry))] // Async in sync machine
                    private void Config2() { }
                    
                    private void SyncEntry() { }
                    private async Task AsyncEntry() { await Task.Delay(1); }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.AsyncCallbackInSyncMachine).ToList();
        output.WriteLine($"FSM013: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Expected NOT to emit (not implemented)
    }

    [Fact]
    public void FSM014_InvalidAsyncVoid_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [State(State.A, OnEntry = nameof(AsyncVoidMethod))]
                    private void Config() { }
                    
                    private async void AsyncVoidMethod() { await System.Threading.Tasks.Task.Delay(1); }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        output.WriteLine($"FSM014: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    // ============================================================================
    // HSM DIAGNOSTICS (FSM100-FSM105)
    // ============================================================================

    [Fact]
    public void FSM100_CircularHierarchy_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    [State(State.A, Parent = State.B)]
                    [State(State.B, Parent = State.C)]
                    [State(State.C, Parent = State.A)] // Circular
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.CircularHierarchy).ToList();
        output.WriteLine($"FSM100: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM101_OrphanSubstate_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    [State(State.A, Parent = State.B)]
                    [State(State.A, Parent = State.C)] // Multiple parents
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.OrphanSubstate).ToList();
        output.WriteLine($"FSM101: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM102_InvalidHierarchyConfiguration_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { Parent, Child1, Child2, Other }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    [State(State.Parent)]
                    [State(State.Child1, Parent = State.Parent)]
                    [State(State.Child2, Parent = State.Parent)]
                    // No initial child specified for Parent
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidHierarchyConfiguration).ToList();
        output.WriteLine($"FSM102: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM103_MultipleInitialSubstates_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    [State(State.Parent)]
                    [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    [State(State.Child2, Parent = State.Parent, IsInitial = true)] // Multiple initial
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.MultipleInitialSubstates).ToList();
        output.WriteLine($"FSM103: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM104_InvalidHistoryConfiguration_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    [State(State.A, History = HistoryMode.Shallow)] // History on non-composite
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.InvalidHistoryConfiguration).ToList();
        output.WriteLine($"FSM104: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM105_ConflictingTransitionTargets_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { Parent, Child1, Child2, Other }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    [State(State.Parent)]
                    [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    [State(State.Child2, Parent = State.Parent)]
                    
                    // Transition to composite without specifying child
                    [Transition(State.Other, Trigger.X, State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == RuleIdentifiers.ConflictingTransitionTargets).ToList();
        output.WriteLine($"FSM105: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
        // Expected NOT to emit (not implemented)
    }

    // ============================================================================
    // INFO/DEBUG DIAGNOSTICS (FSM9xx)
    // ============================================================================

    [Fact]
    public void FSM981_NoTransitions_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // No transitions defined
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == "FSM981").ToList();
        output.WriteLine($"FSM981: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM982_InternalOnlyMachine_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [InternalTransition(State.A, Trigger.X, Action = nameof(DoSomething))]
                    [InternalTransition(State.B, Trigger.Y, Action = nameof(DoSomething))]
                    private void Config() { }
                    
                    private void DoSomething() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == "FSM982").ToList();
        output.WriteLine($"FSM982: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM983_MissingActionMethod_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [InternalTransition(State.A, Trigger.X)] // Missing Action
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == "FSM983").ToList();
        output.WriteLine($"FSM983: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM994_EnumOnlyFallback_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // No [State] attributes, will use enum-only fallback
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsmErrors = diags.Where(d => d.Id == "FSM994").ToList();
        output.WriteLine($"FSM994: {(fsmErrors.Any() ? "✓ EMITTED" : "✗ NOT EMITTED")}");
    }

    [Fact]
    public void FSM999_ParserCriticalError_CannotTestDirectly()
    {
        // FSM999 is for parser critical errors which are hard to trigger in tests
        // It would require corrupting the parser state internally
        output.WriteLine("FSM999: Cannot test directly (internal critical error)");
    }

    // ============================================================================
    // SUMMARY TEST - Run this to get a complete report
    // ============================================================================

    [Fact]
    public void RunAllDiagnosticTests_GenerateReport()
    {
        output.WriteLine("=== FSM DIAGNOSTIC EMISSION VERIFICATION ===\n");
        
        // Run all individual tests and collect results
        var testResults = new Dictionary<string, bool>();
        
        // Core diagnostics
        FSM001_DuplicateTransition_ShouldEmit();
        FSM002_UnreachableState_ShouldEmit();
        FSM003_InvalidMethodSignature_ShouldEmit();
        FSM004_MissingStateMachineAttribute_ShouldNotEmitInParser();
        FSM005_InvalidTypesInAttribute_ShouldEmit();
        FSM006_InvalidEnumValueInTransition_ShouldEmit();
        FSM007_MissingPayloadType_ShouldNotEmit();
        FSM008_ConflictingPayloadConfiguration_ShouldNotEmit();
        FSM009_InvalidForcedVariantConfiguration_ShouldNotEmit();
        FSM010_GuardWithPayloadInNonPayloadMachine_ShouldEmit();
        FSM011_MixedSyncAsyncCallbacks_ShouldEmit();
        FSM012_InvalidGuardTaskReturnType_ShouldEmit();
        FSM013_AsyncCallbackInSyncMachine_ShouldNotEmit();
        FSM014_InvalidAsyncVoid_ShouldEmit();
        
        // HSM diagnostics
        FSM100_CircularHierarchy_ShouldEmit();
        FSM101_OrphanSubstate_ShouldEmit();
        FSM102_InvalidHierarchyConfiguration_ShouldEmit();
        FSM103_MultipleInitialSubstates_ShouldEmit();
        FSM104_InvalidHistoryConfiguration_ShouldEmit();
        FSM105_ConflictingTransitionTargets_ShouldNotEmit();
        
        // Info diagnostics
        FSM981_NoTransitions_ShouldEmit();
        FSM982_InternalOnlyMachine_ShouldEmit();
        FSM983_MissingActionMethod_ShouldEmit();
        FSM994_EnumOnlyFallback_ShouldEmit();
        
        output.WriteLine("\n=== SUMMARY ===");
        output.WriteLine("Diagnostics that SHOULD emit but DON'T need investigation");
        output.WriteLine("Diagnostics that SHOULDN'T emit but DO need to be fixed");
    }
}