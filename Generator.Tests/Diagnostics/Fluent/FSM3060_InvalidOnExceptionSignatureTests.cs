using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Fluent;

public class FSM3060_InvalidOnExceptionSignatureTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3060_For_Invalid_OnException_Signature()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .OnException(nameof(BadHandler))
            .On(Trigger.X).GoTo(State.B);

        // Invalid: wrong parameter type (no ExceptionContext)
        private static FastFsm.Exceptions.ExceptionDirective BadHandler(int notContext) =>
            FastFsm.Exceptions.ExceptionDirective.Propagate;
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        foreach (var d in diags) output.WriteLine($"{d.Id}: {d.GetMessage()}");
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidOnExceptionSignature).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3060 for invalid OnException signature.");
    }
}
