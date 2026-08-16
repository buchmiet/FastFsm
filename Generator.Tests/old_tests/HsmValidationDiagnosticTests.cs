using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

/// <summary>
/// Tests for FSM100-105 HSM (Hierarchical State Machine) validation diagnostics.
/// </summary>
public class HsmValidationDiagnosticTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    // ============================================================================
    // FSM100: CircularHierarchy Tests
    // ============================================================================
    
    [Fact]
    public void FSM100_CircularHierarchy_DirectCycle_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // A is parent of B, B is parent of A - circular!
                    [State(State.A, Parent = State.B)]
                    [State(State.B, Parent = State.A)]
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm100 = diags.Where(d => d.Id == RuleIdentifiers.CircularHierarchy).ToList();
        Assert.NotEmpty(fsm100);
        Assert.Equal(DiagnosticSeverity.Error, fsm100[0].Severity);
        output.WriteLine($"FSM100: ✓ EMITTED - {fsm100[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM100_CircularHierarchy_IndirectCycle_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // A -> B -> C -> A (circular)
                    [State(State.A, Parent = State.C)]
                    [State(State.B, Parent = State.A)]
                    [State(State.C, Parent = State.B)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm100 = diags.Where(d => d.Id == RuleIdentifiers.CircularHierarchy).ToList();
        Assert.NotEmpty(fsm100);
        output.WriteLine($"FSM100: ✓ EMITTED - {fsm100[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM100_ValidHierarchy_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { Root, Parent, Child }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Valid hierarchy: Root -> Parent -> Child
                    [State(State.Parent, Parent = State.Root)]
                    [State(State.Child, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm100 = diags.Where(d => d.Id == RuleIdentifiers.CircularHierarchy).ToList();
        Assert.Empty(fsm100);
        output.WriteLine($"FSM100: ✓ NOT EMITTED (correctly - valid hierarchy)");
    }
    
    // ============================================================================
    // FSM101: OrphanSubstate Tests
    // ============================================================================
    
    [Fact]
    public void FSM101_OrphanSubstate_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // B references non-existent parent 'NonExistent'
                    [State(State.B, Parent = ""NonExistent"")]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm101 = diags.Where(d => d.Id == RuleIdentifiers.OrphanSubstate).ToList();
        Assert.NotEmpty(fsm101);
        Assert.Equal(DiagnosticSeverity.Error, fsm101[0].Severity);
        output.WriteLine($"FSM101: ✓ EMITTED - {fsm101[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM101_ValidParent_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { Parent, Child }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Valid parent reference
                    [State(State.Child, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm101 = diags.Where(d => d.Id == RuleIdentifiers.OrphanSubstate).ToList();
        Assert.Empty(fsm101);
        output.WriteLine($"FSM101: ✓ NOT EMITTED (correctly - valid parent)");
    }
    
    // ============================================================================
    // FSM102: InvalidHierarchyConfiguration Tests
    // ============================================================================
    
    [Fact]
    public void FSM102_CompositeWithoutInitial_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Parent has children but no initial substate
                    [State(State.Child1, Parent = State.Parent)]
                    [State(State.Child2, Parent = State.Parent)]
                    // Missing: [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm102 = diags.Where(d => d.Id == RuleIdentifiers.InvalidHierarchyConfiguration).ToList();
        Assert.NotEmpty(fsm102);
        Assert.Equal(DiagnosticSeverity.Error, fsm102[0].Severity);
        output.WriteLine($"FSM102: ✓ EMITTED - {fsm102[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM102_CompositeWithInitial_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Parent has children and an initial substate
                    [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    [State(State.Child2, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm102 = diags.Where(d => d.Id == RuleIdentifiers.InvalidHierarchyConfiguration).ToList();
        Assert.Empty(fsm102);
        output.WriteLine($"FSM102: ✓ NOT EMITTED (correctly - has initial substate)");
    }
    
    [Fact]
    public void FSM102_CompositeWithHistory_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            
            namespace Test {
                public enum State { Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Parent has history mode, so initial substate not required
                    [State(State.Parent, History = HistoryMode.Shallow)]
                    [State(State.Child1, Parent = State.Parent)]
                    [State(State.Child2, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm102 = diags.Where(d => d.Id == RuleIdentifiers.InvalidHierarchyConfiguration).ToList();
        Assert.Empty(fsm102);
        output.WriteLine($"FSM102: ✓ NOT EMITTED (correctly - has history mode)");
    }
    
    // ============================================================================
    // FSM103: MultipleInitialSubstates Tests
    // ============================================================================
    
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
                    // Both children marked as initial - error!
                    [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    [State(State.Child2, Parent = State.Parent, IsInitial = true)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm103 = diags.Where(d => d.Id == RuleIdentifiers.MultipleInitialSubstates).ToList();
        Assert.NotEmpty(fsm103);
        Assert.Equal(DiagnosticSeverity.Error, fsm103[0].Severity);
        output.WriteLine($"FSM103: ✓ EMITTED - {fsm103[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM103_SingleInitialSubstate_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Only one child marked as initial - correct
                    [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    [State(State.Child2, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm103 = diags.Where(d => d.Id == RuleIdentifiers.MultipleInitialSubstates).ToList();
        Assert.Empty(fsm103);
        output.WriteLine($"FSM103: ✓ NOT EMITTED (correctly - single initial substate)");
    }
    
    // ============================================================================
    // FSM104: InvalidHistoryConfiguration Tests
    // ============================================================================
    
    [Fact]
    public void FSM104_HistoryOnNonComposite_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            
            namespace Test {
                public enum State { Simple, Other }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Simple has no children but has history mode - warning!
                    [State(State.Simple, History = HistoryMode.Shallow)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm104 = diags.Where(d => d.Id == RuleIdentifiers.InvalidHistoryConfiguration).ToList();
        Assert.NotEmpty(fsm104);
        Assert.Equal(DiagnosticSeverity.Warning, fsm104[0].Severity);
        output.WriteLine($"FSM104: ✓ EMITTED - {fsm104[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM104_HistoryOnComposite_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            
            namespace Test {
                public enum State { Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Parent has children and history mode - correct
                    [State(State.Parent, History = HistoryMode.Shallow)]
                    [State(State.Child1, Parent = State.Parent)]
                    [State(State.Child2, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm104 = diags.Where(d => d.Id == RuleIdentifiers.InvalidHistoryConfiguration).ToList();
        Assert.Empty(fsm104);
        output.WriteLine($"FSM104: ✓ NOT EMITTED (correctly - history on composite)");
    }
    
    // ============================================================================
    // FSM105: ConflictingTransitionTargets Tests
    // Note: FSM105 is informational and may not be implemented yet
    // ============================================================================
    
    [Fact]
    public void FSM105_TransitionToComposite_MightEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { Source, Parent, Child1, Child2 }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // Transition to composite state without specifying child
                    [Transition(State.Source, Trigger.X, State.Parent)]
                    [State(State.Child1, Parent = State.Parent, IsInitial = true)]
                    [State(State.Child2, Parent = State.Parent)]
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm105 = diags.Where(d => d.Id == RuleIdentifiers.ConflictingTransitionTargets).ToList();
        
        // FSM105 might not be implemented yet - just log the result
        if (fsm105.Any())
        {
            Assert.Equal(DiagnosticSeverity.Info, fsm105[0].Severity);
            output.WriteLine($"FSM105: ✓ EMITTED - {fsm105[0].GetMessage()}");
        }
        else
        {
            output.WriteLine($"FSM105: NOT EMITTED (might not be implemented yet)");
        }
    }
    
    // ============================================================================
    // Combined Tests
    // ============================================================================
    
    [Fact]
    public void HSM_Multiple_Issues_ShouldEmitMultiple()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using Generator.Model;
            
            namespace Test {
                public enum State { A, B, C, D }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
                public partial class Machine {
                    // FSM100: Circular hierarchy A -> B -> A
                    [State(State.A, Parent = State.B)]
                    [State(State.B, Parent = State.A)]
                    
                    // FSM101: Orphan substate
                    [State(State.C, Parent = ""NonExistent"")]
                    
                    // FSM104: History on non-composite
                    [State(State.D, History = HistoryMode.Deep)]
                    
                    private void Config() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm100 = diags.Where(d => d.Id == RuleIdentifiers.CircularHierarchy).ToList();
        var fsm101 = diags.Where(d => d.Id == RuleIdentifiers.OrphanSubstate).ToList();
        var fsm104 = diags.Where(d => d.Id == RuleIdentifiers.InvalidHistoryConfiguration).ToList();
        
        Assert.NotEmpty(fsm100);
        Assert.NotEmpty(fsm101);
        Assert.NotEmpty(fsm104);
        
        output.WriteLine($"FSM100: ✓ EMITTED");
        output.WriteLine($"FSM101: ✓ EMITTED");
        output.WriteLine($"FSM104: ✓ EMITTED");
    }
}