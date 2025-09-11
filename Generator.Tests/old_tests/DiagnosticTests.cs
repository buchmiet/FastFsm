using Generator.Rules.Definitions;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class DiagnosticTests(ITestOutputHelper output):GeneratorBaseClass(output)
{


    [Fact]
    public void EnumValueCasting_ValidatedCorrectly()
    {
        const string sourceCode = @"
                using Abstractions.Attributes;
                namespace Test {
                    public enum State : byte { Low = 0, High = 255 }
                    public enum Trigger { Go }
                    
                    [StateMachine(typeof(State), typeof(Trigger))]
                    public partial class Machine {
                        [Transition((State)0, Trigger.Go, (State)255)] // Valid
                        [Transition((State)128, Trigger.Go, State.Low)] // Invalid - 128 not defined
                        private void Config() { }
                    }
                }";

        var (asm1, diags1, generatedSources1) =
            CompileAndRunGenerator([sourceCode], new StateMachineGenerator());

        // Should have one FSM006 for invalid value
        var fsmErrors = diags1.Where(d => d.Id == RuleIdentifiers.InvalidEnumValueInTransition).ToList();
        Assert.Single(fsmErrors);
        Assert.Contains("128", fsmErrors[0].GetMessage());
    }


  
}