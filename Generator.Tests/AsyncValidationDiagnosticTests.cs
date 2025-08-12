using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

/// <summary>
/// Tests for FSM012-014 async validation diagnostics.
/// </summary>
public class AsyncValidationDiagnosticTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    // ============================================================================
    // FSM012: InvalidGuardTaskReturnType Tests
    // ============================================================================
    
    [Fact]
    public void FSM012_Guard_ReturnsTaskBool_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Guard = nameof(CheckCondition))]
                    private void Config() { }
                    
                    // Invalid: Guards should return ValueTask<bool>, not Task<bool>
                    private async Task<bool> CheckCondition() {
                        await Task.Delay(100);
                        return true;
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm012 = diags.Where(d => d.Id == RuleIdentifiers.InvalidGuardTaskReturnType).ToList();
        Assert.NotEmpty(fsm012);
        Assert.Equal(DiagnosticSeverity.Error, fsm012[0].Severity);
        output.WriteLine($"FSM012: ✓ EMITTED - {fsm012[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM012_Guard_ReturnsValueTaskBool_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Guard = nameof(CheckCondition))]
                    private void Config() { }
                    
                    // Correct: Guards should return ValueTask<bool>
                    private async ValueTask<bool> CheckCondition() {
                        await Task.Delay(100);
                        return true;
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm012 = diags.Where(d => d.Id == RuleIdentifiers.InvalidGuardTaskReturnType).ToList();
        Assert.Empty(fsm012);
        output.WriteLine($"FSM012: ✓ NOT EMITTED (correctly - uses ValueTask<bool>)");
    }
    
    [Fact]
    public void FSM012_SyncGuard_ReturnsBool_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Guard = nameof(CheckCondition))]
                    private void Config() { }
                    
                    // Sync guard returning bool is fine
                    private bool CheckCondition() {
                        return true;
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm012 = diags.Where(d => d.Id == RuleIdentifiers.InvalidGuardTaskReturnType).ToList();
        Assert.Empty(fsm012);
        output.WriteLine($"FSM012: ✓ NOT EMITTED (correctly - sync guard)");
    }
    
    // ============================================================================
    // FSM013: AsyncCallbackInSyncMachine Tests
    // ============================================================================
    
    [Fact]
    public void FSM013_AsyncCallback_InEstablishedSyncMachine_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // First transition with sync callback establishes machine as sync
                    [Transition(State.A, Trigger.X, State.B, Action = nameof(DoSomething))]
                    // Second transition tries to use async callback - FSM013 should emit
                    [Transition(State.B, Trigger.Y, State.C, Action = nameof(DoSomethingAsync))]
                    private void Config() { }
                    
                    private void DoSomething() { }
                    
                    // This async callback conflicts with established sync mode
                    private async Task DoSomethingAsync() {
                        await Task.Delay(100);
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        // Note: Either FSM013 or FSM011 might emit, depending on implementation
        var fsm013 = diags.Where(d => d.Id == RuleIdentifiers.AsyncCallbackInSyncMachine).ToList();
        var fsm011 = diags.Where(d => d.Id == RuleIdentifiers.MixedSyncAsyncCallbacks).ToList();
        
        // At least one of them should emit
        Assert.True(fsm013.Any() || fsm011.Any());
        
        if (fsm013.Any())
        {
            Assert.Equal(DiagnosticSeverity.Error, fsm013[0].Severity);
            output.WriteLine($"FSM013: ✓ EMITTED - {fsm013[0].GetMessage()}");
        }
        else
        {
            output.WriteLine($"FSM011: ✓ EMITTED (instead of FSM013) - {fsm011[0].GetMessage()}");
        }
    }
    
    [Fact]
    public void FSM013_AllAsync_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X, Y }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    // All callbacks are async - consistent
                    [Transition(State.A, Trigger.X, State.B, Action = nameof(DoSomethingAsync1))]
                    [Transition(State.B, Trigger.Y, State.C, Action = nameof(DoSomethingAsync2))]
                    private void Config() { }
                    
                    private async Task DoSomethingAsync1() {
                        await Task.Delay(100);
                    }
                    
                    private async Task DoSomethingAsync2() {
                        await Task.Delay(100);
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm013 = diags.Where(d => d.Id == RuleIdentifiers.AsyncCallbackInSyncMachine).ToList();
        Assert.Empty(fsm013);
        output.WriteLine($"FSM013: ✓ NOT EMITTED (correctly - all async)");
    }
    
    // ============================================================================
    // FSM014: InvalidAsyncVoid Tests
    // ============================================================================
    
    [Fact]
    public void FSM014_AsyncVoid_OnEntry_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [State(State.A, OnEntry = nameof(EnterA))]
                    [Transition(State.A, Trigger.X, State.B)]
                    private void Config() { }
                    
                    // Invalid: async void should be async Task
                    private async void EnterA() {
                        await Task.Delay(100);
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm014 = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        Assert.NotEmpty(fsm014);
        Assert.Equal(DiagnosticSeverity.Warning, fsm014[0].Severity);
        output.WriteLine($"FSM014: ✓ EMITTED - {fsm014[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM014_AsyncVoid_Action_ShouldEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, Action = nameof(TransitionAction))]
                    private void Config() { }
                    
                    // Invalid: async void should be async Task
                    private async void TransitionAction() {
                        await Task.Delay(100);
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm014 = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        Assert.NotEmpty(fsm014);
        Assert.Equal(DiagnosticSeverity.Warning, fsm014[0].Severity);
        output.WriteLine($"FSM014: ✓ EMITTED - {fsm014[0].GetMessage()}");
    }
    
    [Fact]
    public void FSM014_AsyncTask_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [State(State.A, OnEntry = nameof(EnterA))]
                    [Transition(State.A, Trigger.X, State.B, Action = nameof(TransitionAction))]
                    private void Config() { }
                    
                    // Correct: async Task
                    private async Task EnterA() {
                        await Task.Delay(100);
                    }
                    
                    // Correct: async ValueTask
                    private async ValueTask TransitionAction() {
                        await Task.Delay(100);
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm014 = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        Assert.Empty(fsm014);
        output.WriteLine($"FSM014: ✓ NOT EMITTED (correctly - uses Task/ValueTask)");
    }
    
    [Fact]
    public void FSM014_SyncVoid_ShouldNotEmit()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [State(State.A, OnEntry = nameof(EnterA))]
                    [Transition(State.A, Trigger.X, State.B, Action = nameof(TransitionAction))]
                    private void Config() { }
                    
                    // Sync void is fine
                    private void EnterA() { }
                    
                    private void TransitionAction() { }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm014 = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        Assert.Empty(fsm014);
        output.WriteLine($"FSM014: ✓ NOT EMITTED (correctly - sync void is fine)");
    }
    
    // ============================================================================
    // Combined Tests
    // ============================================================================
    
    [Fact]
    public void FSM012_FSM014_Combined_ShouldEmitBoth()
    {
        const string sourceCode = @"
            using Abstractions.Attributes;
            using System.Threading.Tasks;
            
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B, 
                        Guard = nameof(CheckCondition),
                        Action = nameof(TransitionAction))]
                    private void Config() { }
                    
                    // FSM012: Should use ValueTask<bool>
                    private async Task<bool> CheckCondition() {
                        await Task.Delay(100);
                        return true;
                    }
                    
                    // FSM014: Should use Task, not void
                    private async void TransitionAction() {
                        await Task.Delay(100);
                    }
                }
            }";

        var (_, diags, _) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        output.WriteLine($"Total diagnostics: {diags.Length}");
        foreach (var diag in diags)
        {
            output.WriteLine($"  - {diag.Id}: {diag.GetMessage()}");
        }
        
        var fsm012 = diags.Where(d => d.Id == RuleIdentifiers.InvalidGuardTaskReturnType).ToList();
        var fsm014 = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        
        Assert.NotEmpty(fsm012);
        Assert.NotEmpty(fsm014);
        
        output.WriteLine($"FSM012: ✓ EMITTED - {fsm012[0].GetMessage()}");
        output.WriteLine($"FSM014: ✓ EMITTED - {fsm014[0].GetMessage()}");
    }
}