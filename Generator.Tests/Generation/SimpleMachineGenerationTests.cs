using System.Linq;
using Generator;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Generation;

public class SimpleMachineGenerationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Generates_Code_For_Simple_StateMachine()
    {
        const string source = @"
using Abstractions.Attributes;
namespace Sample {
    public enum State { Idle, Active }
    public enum Trigger { Start, Stop }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine
    {
        [Transition(State.Idle, Trigger.Start, State.Active)]
        [Transition(State.Active, Trigger.Stop, State.Idle)]
        private void Configure() { }
    }
}";

        var (assembly, diagnostics, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
        Assert.NotNull(assembly);
        const string hintName = "global__Sample.Machine.Generated.cs";
        Assert.Contains(hintName, generatedSources.Keys);

        var generated = generatedSources[hintName];
        Assert.Contains("public interface IMachine", generated);
        Assert.Contains("public partial class Machine", generated);

        var discoverySource = generatedSources["__FastFsm.DiscoveredMachines.g.cs"];
        Assert.Contains("Sample.Machine", discoverySource);
    }
}
