using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Fluent;

/// <summary>
/// Tests covering DSL purity diagnostics FSM3071–FSM3077.
/// </summary>
public class FSM3071_FluentDslPurityTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    private static string WrapDsl(string configureBody, string extraMembers = "") => $@"
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace TestNamespace {{
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class TestMachine
    {{
        public enum State {{ Idle, Active }}
        public enum Trigger {{ Go, Stop }}

        private bool _flag = true;
        private bool Allow() => true;
        private bool Disallow() => false;
        private static class Helper {{ public static bool Validate() => true; }}
        private bool PropertyGuard => true;

        private void Configure()
        {{
{configureBody}
        }}

{extraMembers}
    }}
}}
";

    [Fact]
    public void Emits_FSM3071_For_Conditional_Method_Group()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(_flag ? Allow : Disallow)
                    .GoTo(State.Active)
            .State(State.Active);";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ImpureDslExpression);
    }

    [Fact]
    public void Emits_FSM3072_For_Property_Method_Group()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(PropertyGuard)
                    .GoTo(State.Active)
            .State(State.Active);";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.PropertyMethodGroupNotAllowed);
    }

    [Fact]
    public void Emits_FSM3073_For_External_Method_Group()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(Helper.Validate)
                    .GoTo(State.Active)
            .State(State.Active);";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.ExternalMethodGroup);
    }

    [Fact]
    public void Emits_FSM3074_For_Invalid_Guard_Signature()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(BadGuard)
                    .GoTo(State.Active)
            .State(State.Active);";

        const string extraMembers = "        private int BadGuard() => 42;";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure, extraMembers)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.DslSignatureMismatch);
    }

    [Fact]
    public void Emits_FSM3075_For_Lambda_Guard()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(() => true)
                    .GoTo(State.Active)
            .State(State.Active);";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.LambdaExpressionNotAllowed);
    }

    [Fact]
    public void Emits_FSM3076_For_Field_Usage()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(_flag)
                    .GoTo(State.Active)
            .State(State.Active);";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.FieldOrPropertyAccessInDsl);
    }

    [Fact]
    public void Emits_FSM3077_For_Method_Invocation_In_Dsl()
    {
        const string configure = @"        FSM
            .State(State.Idle)
                .On(Trigger.Go)
                    .Guard(InvokeGuard())
                    .GoTo(State.Active)
            .State(State.Active);";

        const string extraMembers = "        private bool InvokeGuard() => true;";

        var (_, diags, _) = CompileAndRunGenerator([WrapDsl(configure, extraMembers)], new StateMachineGenerator());
        Assert.Contains(diags, d => d.Id == RuleIdentifiers.MethodInvocationInDsl);
    }
}
