using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Fluent;

public class FSM3050_DuplicateOnExceptionHandlerTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3050_When_OnException_Called_Twice_With_Valid_Signatures()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
using FastFsm.Exceptions;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .OnException(nameof(Handle1))
            .OnException(nameof(Handle2))
            .On(Trigger.X).GoTo(State.B);

        private static System.Threading.Tasks.ValueTask<FastFsm.Exceptions.ExceptionDirective> Handle1(
            FastFsm.Exceptions.ExceptionContext<State, Trigger> ctx,
            System.Threading.CancellationToken ct) => new(System.Threading.Tasks.Task.FromResult(FastFsm.Exceptions.ExceptionDirective.Propagate));

        private static System.Threading.Tasks.ValueTask<FastFsm.Exceptions.ExceptionDirective> Handle2(
            FastFsm.Exceptions.ExceptionContext<State, Trigger> ctx,
            System.Threading.CancellationToken ct) => new(System.Threading.Tasks.Task.FromResult(FastFsm.Exceptions.ExceptionDirective.Propagate));
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        foreach (var d in diags) output.WriteLine($"{d.Id}: {d.GetMessage()}");
        var hits = diags.Where(d => d.Id == RuleIdentifiers.DuplicateOnExceptionHandler).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3050 for duplicate OnException handlers.");
    }
}
