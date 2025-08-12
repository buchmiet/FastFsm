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
        
        output.WriteLine($"Generated sources: {sources.Count}");
        output.WriteLine($"Total diagnostics: {diags.Length}");
        
        // Check for FSM001
        var fsm001 = diags.Where(d => d.Id == "FSM001").ToList();
        output.WriteLine($"FSM001 (DuplicateTransition): {fsm001.Count}");
        
        // Check for any FSM diagnostics
        var fsmDiags = diags.Where(d => d.Id.StartsWith("FSM")).ToList();
        output.WriteLine($"All FSM diagnostics: {fsmDiags.Count}");
        foreach (var d in fsmDiags)
        {
            output.WriteLine($"  {d.Id}: {d.GetMessage()}");
        }
        
        // Check if code was generated
        var hasCode = sources.Any(s => s.Value.Contains("partial class Machine"));
        output.WriteLine($"Machine class generated: {hasCode}");
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
        
        output.WriteLine($"Generated sources: {sources.Count}");
        output.WriteLine($"Total diagnostics: {diags.Length}");
        
        var fsm001 = diags.Where(d => d.Id == "FSM001").ToList();
        output.WriteLine($"FSM001 count: {fsm001.Count}");
        
        // Show compilation errors if any
        var errors = diags.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        output.WriteLine($"Errors: {errors.Count}");
        foreach (var e in errors.Take(3))
        {
            output.WriteLine($"  {e.Id}: {e.GetMessage()}");
        }
    }
}