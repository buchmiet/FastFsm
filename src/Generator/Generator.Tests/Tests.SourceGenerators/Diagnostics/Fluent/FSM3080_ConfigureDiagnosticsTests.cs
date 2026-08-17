using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Fluent;

/// <summary>
/// Tests for Configure() method diagnostics FSM3080–FSM3083.
/// </summary>
public class FSM3080_ConfigureDiagnosticsTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    private static string Harness(string configureSection) => $@"
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {{
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {{
        public enum State {{ A, B }}
        public enum Trigger {{ Go }}

{configureSection}
    }}
}}
";

    [Fact]
    public void Emits_FSM3080_For_Multiple_Configure_Like_Methods()
    {
        const string configure = @"        private void Configure() => FSM.State(State.A);
        private void SetupStates() => FSM.State(State.B);";

        var (_, diags, _) = CompileAndRunGenerator([Harness(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.MultipleConfigureMethods);
    }

    [Fact]
    public void Emits_FSM3081a_When_Configure_Not_Private()
    {
        const string configure = "        public void Configure() => FSM.State(State.A);";
        var (_, diags, _) = CompileAndRunGenerator([Harness(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ConfigureMustBePrivate);
    }

    [Fact]
    public void Emits_FSM3081b_When_Configure_Has_Parameters()
    {
        const string configure = "        private void Configure(int value) => FSM.State(State.A);";
        var (_, diags, _) = CompileAndRunGenerator([Harness(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ConfigureMustBeParameterless);
    }

    [Fact]
    public void Emits_FSM3081c_When_Configure_Is_Virtual()
    {
        const string configure = "        private virtual void Configure() => FSM.State(State.A);";
        var (_, diags, _) = CompileAndRunGenerator([Harness(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ConfigureCannotBeVirtual);
    }

    [Fact]
    public void Emits_FSM3081d_When_Configure_Is_Static()
    {
        const string configure = "        private static void Configure() => FSM.State(State.A);";
        var (_, diags, _) = CompileAndRunGenerator([Harness(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ConfigureMustBeInstance);
    }

    [Fact]
    public void Emits_FSM3082_When_Configure_Inherited()
    {
        const string source = @"
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    public class BaseMachine
    {
        protected void Configure() { }
    }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class DerivedMachine : BaseMachine
    {
        public enum State { A, B }
        public enum Trigger { Go }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ConfigureNotDeclaredOnType);
    }

    [Fact]
    public void Emits_FSM3083_When_Configure_Is_Partial_Method()
    {
        const string source = @"
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class PartialConfigureMachine
    {
        public enum State { A, B }
        public enum Trigger { Go }

        partial void Configure();
        partial void Configure()
        {
            FSM.State(State.A);
        }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ConfigureCannotBePartial);
    }
}
