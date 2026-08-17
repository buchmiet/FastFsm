using System.Linq;
using Abstractions.Attributes;
using Generator;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Generation;

public class ExtensionGenerationOptInTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    private enum ContractState { Idle }
    private enum ContractTrigger { Start }

    [Fact]
    public void StateMachineAttribute_Defaults_To_NonExtensible()
    {
        var attribute = new StateMachineAttribute(typeof(ContractState), typeof(ContractTrigger));

        Assert.False(attribute.GenerateExtensibleVersion);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(", GenerateExtensibleVersion = true", true)]
    [InlineData(", GenerateExtensibleVersion = false", false)]
    public void AttributeApi_ExtensionGeneration_Is_Explicit_OptIn(string option, bool expectedExtensible)
    {
        var source = $@"
using Abstractions.Attributes;

namespace Sample {{
    public enum State {{ Idle, Active }}
    public enum Trigger {{ Start }}

    [StateMachine(typeof(State), typeof(Trigger){option})]
    public partial class AttributeMachine
    {{
        [Transition(State.Idle, Trigger.Start, State.Active)]
        private void DefineTransitions() {{ }}
    }}
}}";

        AssertExtensionSurface(source, "AttributeMachine", expectedExtensible);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(", GenerateExtensibleVersion = true", true)]
    [InlineData(", GenerateExtensibleVersion = false", false)]
    public void FluentApi_ExtensionGeneration_Is_Explicit_OptIn(string option, bool expectedExtensible)
    {
        var source = $@"
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace Sample {{
    public enum State {{ Idle, Active }}
    public enum Trigger {{ Start }}

    [StateMachine(typeof(State), typeof(Trigger){option})]
    public partial class FluentMachine
    {{
        private void Configure() => FSM
            .State(State.Idle)
                .On(Trigger.Start)
                    .GoTo(State.Active);
    }}
}}";

        AssertExtensionSurface(source, "FluentMachine", expectedExtensible);
    }

    private void AssertExtensionSurface(string source, string className, bool expectedExtensible)
    {
        var (assembly, diagnostics, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
        Assert.NotNull(assembly);

        var generated = generatedSources.Values
            .Single(text => text.Contains($"public partial class {className}"));

        if (expectedExtensible)
        {
            Assert.Contains("IExtensibleStateMachineSync<", generated);
            Assert.Contains("IEnumerable<IStateMachineExtension>? extensions = null", generated);
        }
        else
        {
            Assert.DoesNotContain("IExtensibleStateMachineSync<", generated);
            Assert.DoesNotContain("IEnumerable<IStateMachineExtension>? extensions = null", generated);
        }
    }
}
