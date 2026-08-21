using System.Linq;
using Generator;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Generation;

public sealed class ExtensionHsmGenerationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Extensible_hsm_emits_handled_at_lca_and_does_not_emit_v1_hierarchy_stubs()
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
        Assert.Contains("FindLowestCommonAncestor(__handledState, __lifecycleTarget)", generated);
        Assert.Contains("if ((extensionSet.Hooks & ExtensionHooks.States) != 0)", generated);
        Assert.Contains("if ((extensionSet.Hooks & ExtensionHooks.Transitions) != 0)", generated);
        Assert.Contains("int __lifecycleSource = (int)attempt.SourceState;", generated);
        Assert.Contains("RunStateExiting", generated);
        Assert.Contains("RunStateEntered", generated);
        Assert.DoesNotContain("__fromName", generated);
        Assert.DoesNotContain("RunBubbleToParent", generated);
        Assert.DoesNotContain("RunInitialSubstateEntered", generated);
        Assert.DoesNotContain("RunHistoryRestore", generated);
        Assert.DoesNotContain("RunAncestorPathChanged", generated);
        Assert.DoesNotContain("RunTransitionCompleted", generated);
        Assert.DoesNotContain("OnBubbleToParent", generated);
        Assert.DoesNotContain("static (ext, ctx)", generated);
        Assert.DoesNotContain("(ext, ctx) =>", generated);
    }
}
