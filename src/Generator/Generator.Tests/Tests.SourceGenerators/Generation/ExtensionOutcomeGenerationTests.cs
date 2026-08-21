using System.Linq;
using Generator;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Generation;

public sealed class ExtensionOutcomeGenerationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Extensible_sync_machine_emits_fault_and_cancellation_results()
    {
        const string source = """
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;

namespace Sample;

public enum State { A, B }
public enum Trigger { Go }

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class Machine
{
    private void Configure() => FSM
        .OnException<State>(nameof(HandleException))
        .State(State.A)
            .OnExit(nameof(OnExit))
            .On(Trigger.Go)
                .Guard(nameof(Guard))
                .Action(nameof(Action))
                .GoTo(State.B)
        .State(State.B)
            .OnEntry(nameof(OnEntry));

    private bool Guard() => true;
    private void OnExit() { }
    private void OnEntry() { }
    private void Action() { }
    private ExceptionDirective HandleException(ExceptionContext<State, Trigger> context)
        => ExceptionDirective.Propagate;
}
""";

        AssertOutcomeEmission(source);
    }

    [Fact]
    public void Extensible_async_machine_emits_fault_and_cancellation_results()
    {
        const string source = """
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;

namespace Sample;

public enum State { A, B }
public enum Trigger { Go }

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class Machine
{
    private void Configure() => FSM
        .OnException<State>(nameof(HandleExceptionAsync))
        .State(State.A)
            .OnExitAsync(nameof(OnExitAsync))
            .On(Trigger.Go)
                .Guard(nameof(GuardAsync))
                .Action(nameof(ActionAsync))
                .GoTo(State.B)
        .State(State.B)
            .OnEntryAsync(nameof(OnEntryAsync));

    private ValueTask<bool> GuardAsync() => ValueTask.FromResult(true);
    private ValueTask OnExitAsync() => ValueTask.CompletedTask;
    private ValueTask OnEntryAsync() => ValueTask.CompletedTask;
    private ValueTask ActionAsync() => ValueTask.CompletedTask;
    private ValueTask<ExceptionDirective> HandleExceptionAsync(
        ExceptionContext<State, Trigger> context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(ExceptionDirective.Propagate);
}
""";

        AssertOutcomeEmission(source);
    }

    private void AssertOutcomeEmission(string source)
    {
        var (assembly, diagnostics, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(assembly);

        var generated = generatedSources.Values.Single(text => text.Contains("public partial class Machine"));
        Assert.Contains("TransitionOutcome.Canceled", generated);
        Assert.Contains("TransitionOutcome.Faulted", generated);
        Assert.Contains("catch (System.OperationCanceledException ex)", generated);
        Assert.Contains("when (ex is not System.OperationCanceledException)", generated);
        Assert.Contains("TransitionStage.Guard", generated);
        Assert.Contains("TransitionStage.OnExit", generated);
        Assert.Contains("TransitionStage.OnEntry", generated);
        Assert.Contains("TransitionStage.Action", generated);
        Assert.Contains(
            "extensionSet.Hooks & (ExtensionHooks.Transitions | ExtensionHooks.Guards | ExtensionHooks.States | ExtensionHooks.Callbacks)",
            generated);
        Assert.DoesNotContain("if (extensionSet.Items.Length != 0)", generated);
        Assert.Contains("RunStateExiting", generated);
        Assert.Contains("RunStateEntered", generated);
        Assert.Contains("RunCallbackExecuting", generated);
        Assert.Contains("RunCallbackFaulted", generated);
    }
}