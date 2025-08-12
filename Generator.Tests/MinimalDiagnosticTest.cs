using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class MinimalDiagnosticTest(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Minimal_FullyQualifiedAttributes()
    {
        // Use fully qualified names to avoid namespace issues
        const string sourceCode = @"
            namespace Test {
                public enum State { A, B, C }
                public enum Trigger { X }
                
                [Abstractions.Attributes.StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Abstractions.Attributes.Transition(State.A, Trigger.X, State.B)]
                    [Abstractions.Attributes.Transition(State.A, Trigger.X, State.C)]
                    private void Config() { }
                }
            }";

        var (_, diags, sources) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        // Check for FSM001
        var fsm001 = diags.Where(d => d.Id == "FSM001").ToList();
        
        // FSM001 should be emitted for duplicate transitions
        Assert.True(fsm001.Count > 0, "Expected FSM001 diagnostic for duplicate transition");
        
        // Also check FSM002 for unreachable state
        var fsm002 = diags.Where(d => d.Id == "FSM002").ToList();
        Assert.True(fsm002.Count > 0, "Expected FSM002 diagnostic for unreachable state C");
        
        // Verify code was still generated
        Assert.True(sources.Count > 0, "Expected generated source files");
        var hasCode = sources.Any(s => s.Value.Contains("partial class Machine"));
        Assert.True(hasCode, "Expected generated Machine class");
    }
    
    [Fact]
    public void Minimal_WithUsingDirective()
    {
        // Test with using directive as in original tests
        const string sourceCode = @"
            using Abstractions.Attributes;
            namespace Test {
                public enum State { A, B }
                public enum Trigger { X }
                
                [StateMachine(typeof(State), typeof(Trigger))]
                public partial class Machine {
                    [Transition(State.A, Trigger.X, State.B)]
                    [Transition(State.A, Trigger.X, State.B)] // Exact duplicate
                    private void Config() { }
                }
            }";

        var (_, diags, sources) = CompileAndRunGenerator([sourceCode], new StateMachineGenerator());
        
        var fsm001 = diags.Where(d => d.Id == "FSM001").ToList();
        
        // FSM001 should be emitted for exact duplicate
        Assert.True(fsm001.Count > 0, "Expected FSM001 diagnostic for duplicate transition");
        
        // Verify code was still generated
        Assert.True(sources.Count > 0, "Expected generated source files");
    }
}