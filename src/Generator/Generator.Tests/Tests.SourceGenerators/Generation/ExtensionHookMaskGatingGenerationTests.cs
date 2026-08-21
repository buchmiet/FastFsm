using System.Linq;
using Generator;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Generation;

public sealed class ExtensionHookMaskGatingGenerationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Extensible_hsm_gates_lifecycle_traversal_and_transition_payload_on_hook_mask()
    {
        const string source = """
using Abstractions.Attributes;

namespace Sample;

public enum State { Outside, Root, Left, LeftLeaf, Right, RightLeaf }
public enum Trigger { Go, Refresh }

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true, EnableHierarchy = true)]
public partial class Machine
{
    [State(State.Root)]
    [State(State.Left, Parent = State.Root, IsInitial = true)]
    [State(State.LeftLeaf, Parent = State.Left, IsInitial = true)]
    [State(State.Right, Parent = State.Root)]
    [State(State.RightLeaf, Parent = State.Right, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(State.Outside, Trigger.Go, State.Root)]
    [Transition(State.Root, Trigger.Go, State.RightLeaf)]
    [InternalTransition(State.Root, Trigger.Refresh, nameof(NoOp))]
    private void ConfigureTransitions() { }

    private void NoOp() { }
}
""";

        var (assembly, diagnostics, generatedSources) =
            CompileAndRunGenerator([source], new StateMachineGenerator());

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.NotNull(assembly);

        var generated = generatedSources.Values.Single(text => text.Contains("public partial class Machine"));
        Assert.Contains("if ((extensionSet.Hooks & ExtensionHooks.States) != 0)", generated);
        Assert.Contains("if ((extensionSet.Hooks & ExtensionHooks.Transitions) != 0)", generated);
        Assert.Contains(
            "if ((extensionSet.Hooks & (ExtensionHooks.Transitions | ExtensionHooks.Guards)) != 0)",
            generated);
        Assert.Contains("matchedTransition = new TransitionInfo<State>", generated);

        var statesGate = generated.IndexOf("ExtensionHooks.States", System.StringComparison.Ordinal);
        var lca = generated.IndexOf("FindLowestCommonAncestor", System.StringComparison.Ordinal);
        Assert.True(statesGate >= 0);
        Assert.True(lca > statesGate);
    }
}
