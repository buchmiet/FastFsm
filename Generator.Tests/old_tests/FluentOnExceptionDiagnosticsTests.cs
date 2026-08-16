using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests;

public class FluentOnExceptionDiagnosticsTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void DuplicateOnException_ShouldEmit_FSM208()
    {
        const string source = @"
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;

public enum S { A, B }
public enum T { Go }

[StateMachine(typeof(S), typeof(T))]
public partial class M
{
    private static void Configure() => FSM
        .OnException<S>(nameof(Handle))
        .OnException<S>(nameof(Handle)) // duplicate
        .State(S.A)
            .On(T.Go).GoTo(S.B)
        .State(S.B);

    private ExceptionDirective Handle(ExceptionContext<S, T> ctx) => ExceptionDirective.Continue;
}
";

        var (_, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());
        var fsm208 = diags.Where(d => d.Id == RuleIdentifiers.DuplicateOnExceptionHandler).ToList();
        Assert.NotEmpty(fsm208);
    }

    [Fact]
    public void InvalidSignature_OnException_TaskReturn_ShouldEmit_FSM209()
    {
        const string source = @"
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;

public enum S2 { A, B }
public enum T2 { Go }

[StateMachine(typeof(S2), typeof(T2))]
public partial class M2
{
    private static void Configure() => FSM
        .OnException<S2>(nameof(HandleAsyncTask))
        .State(S2.A)
            .On(T2.Go).GoTo(S2.B)
        .State(S2.B);

    // Invalid: Task<ExceptionDirective> is not allowed
    private Task<ExceptionDirective> HandleAsyncTask(ExceptionContext<S2, T2> ctx) => Task.FromResult(ExceptionDirective.Continue);
}
";

        var (_, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());
        var fsm209 = diags.Where(d => d.Id == RuleIdentifiers.InvalidOnExceptionSignature).ToList();
        Assert.NotEmpty(fsm209);
    }

    [Fact]
    public void InvalidSignature_OnException_VoidReturn_ShouldEmit_FSM209()
    {
        const string source = @"
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;

public enum S3 { A, B }
public enum T3 { Go }

[StateMachine(typeof(S3), typeof(T3))]
public partial class M3
{
    private static void Configure() => FSM
        .OnException<S3>(nameof(HandleVoid))
        .State(S3.A)
            .On(T3.Go).GoTo(S3.B)
        .State(S3.B);

    // Invalid: void is not allowed
    private void HandleVoid(ExceptionContext<S3, T3> ctx) { }
}
";

        var (_, diags, _) = CompileAndRunGenerator([source], new StateMachineGenerator());
        var fsm209 = diags.Where(d => d.Id == RuleIdentifiers.InvalidOnExceptionSignature).ToList();
        Assert.NotEmpty(fsm209);
    }
}
