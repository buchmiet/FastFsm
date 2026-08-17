using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Fluent;

public class FSM3050_DuplicateOnException_NotEmittedCurrentlyTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3050_For_Duplicate_OnException_Calls()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test.FluentDup {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .OnException(nameof(Handle1))
            .OnException(nameof(Handle2))
            .On(Trigger.X).GoTo(State.B);

        // Handlers intentionally use simple signatures; duplicate should still be emitted
        private static FastFsm.Exceptions.ExceptionDirective Handle1(
            FastFsm.Exceptions.ExceptionContext<State, Trigger> ctx) => FastFsm.Exceptions.ExceptionDirective.Propagate;
        private static FastFsm.Exceptions.ExceptionDirective Handle2(
            FastFsm.Exceptions.ExceptionContext<State, Trigger> ctx) => FastFsm.Exceptions.ExceptionDirective.Propagate;
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var dup = diags.Where(d => d.Id == Generator.Rules.Definitions.RuleIdentifiers.DuplicateOnExceptionHandler).ToList();
        Assert.True(dup.Count >= 1, "Expected FSM3050 for duplicate OnException calls.");
    }
}
