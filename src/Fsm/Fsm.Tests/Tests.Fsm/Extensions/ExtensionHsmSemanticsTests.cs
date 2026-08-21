using System;
using System.Collections.Generic;
using System.Linq;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Extensions;

public sealed class ExtensionHsmSemanticsTests
{
    [Fact]
    public void Sibling_branches_emit_leaf_to_lca_and_lca_to_leaf_paths()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.SwitchBranch));

        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "enter:Right", "enter:RightLeaf"],
            extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.LeftLeaf,
            handledAt: HsmSemanticsState.LeftLeaf,
            declared: HsmSemanticsState.RightLeaf,
            resolved: HsmSemanticsState.RightLeaf,
            kind: TransitionKind.External);
    }

    [Fact]
    public void Ancestor_external_transition_reports_leaf_source_and_ancestor_owner()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.AncestorTransition));

        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "exit:Root", "enter:Root", "enter:Right", "enter:RightLeaf"],
            extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.LeftLeaf,
            handledAt: HsmSemanticsState.Root,
            declared: HsmSemanticsState.RightLeaf,
            resolved: HsmSemanticsState.RightLeaf,
            kind: TransitionKind.External);
    }

    [Fact]
    public void Ancestor_to_active_descendant_uses_declared_source_for_lifecycle_boundary()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.ToActiveDescendant));

        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "exit:Root", "enter:Root", "enter:Left", "enter:LeftLeaf"],
            extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.LeftLeaf,
            handledAt: HsmSemanticsState.Root,
            declared: HsmSemanticsState.LeftLeaf,
            resolved: HsmSemanticsState.LeftLeaf,
            kind: TransitionKind.External);
    }

    [Fact]
    public void Ancestor_internal_transition_has_no_targets_or_state_hooks()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.Refresh));

        Assert.Empty(extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.LeftLeaf,
            handledAt: HsmSemanticsState.Root,
            declared: null,
            resolved: null,
            kind: TransitionKind.Internal);
        Assert.Equal(HsmSemanticsState.LeftLeaf, machine.CurrentState);
    }

    [Fact]
    public void Leaf_internal_transition_is_classified_from_the_model_not_from_equals()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.LeafRefresh));

        Assert.Empty(extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.LeftLeaf,
            handledAt: HsmSemanticsState.LeftLeaf,
            declared: null,
            resolved: null,
            kind: TransitionKind.Internal);
    }

    [Fact]
    public void Ancestor_external_self_transition_exits_and_reenters_the_active_subtree()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.Self));

        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "exit:Root", "enter:Root", "enter:Left", "enter:LeftLeaf"],
            extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.LeftLeaf,
            handledAt: HsmSemanticsState.Root,
            declared: HsmSemanticsState.Root,
            resolved: HsmSemanticsState.LeftLeaf,
            kind: TransitionKind.External);
    }

    [Fact]
    public void Composite_target_reports_declared_composite_and_resolved_initial_leaf_once()
    {
        var (extension, machine) = Started(HsmSemanticsState.Outside);

        Assert.True(machine.TryFire(HsmSemanticsTrigger.EnterComposite));

        Assert.Equal(["exit:Outside", "enter:Root", "enter:Left", "enter:LeftLeaf"], extension.StateEvents);
        AssertResult(
            extension,
            machine,
            source: HsmSemanticsState.Outside,
            handledAt: HsmSemanticsState.Outside,
            declared: HsmSemanticsState.Root,
            resolved: HsmSemanticsState.LeftLeaf,
            kind: TransitionKind.External);
        Assert.Equal(extension.StateEvents.Count, extension.StateEvents.DistinctCount());
    }

    [Fact]
    public void Unhandled_trigger_keeps_the_active_leaf_and_has_no_matched_transition()
    {
        var (extension, machine) = Started(HsmSemanticsState.Root);

        Assert.False(machine.TryFire(HsmSemanticsTrigger.Missing));

        Assert.Empty(extension.StateEvents);
        var result = Assert.Single(extension.Results);
        Assert.Equal(HsmSemanticsState.LeftLeaf, Assert.Single(extension.Attempts).SourceState);
        Assert.Equal(TransitionOutcome.UnhandledTrigger, result.Outcome);
        Assert.Null(result.MatchedTransition);
        Assert.Null(result.ResolvedTarget);
        Assert.Equal(HsmSemanticsState.LeftLeaf, result.FinalState);
        Assert.Equal(machine.CurrentState, result.FinalState);
    }

    [Fact]
    public void Shallow_history_reports_restored_child_as_resolved_target_and_entry_path()
    {
        var extension = new HsmHistoryExtension();
        var machine = new HsmShallowHistoryMachine(HsmHistoryState.Outside, [extension]);
        machine.Start();
        machine.Fire(HsmHistoryTrigger.Enter);
        machine.Fire(HsmHistoryTrigger.Next);
        machine.Fire(HsmHistoryTrigger.Exit);
        extension.Clear();

        Assert.True(machine.TryFire(HsmHistoryTrigger.Enter));

        Assert.Equal(["exit:Outside", "enter:Composite", "enter:Second"], extension.StateEvents);
        var result = Assert.Single(extension.Results);
        Assert.Equal(HsmHistoryState.Outside, Assert.Single(extension.Attempts).SourceState);
        Assert.Equal(HsmHistoryState.Outside, result.MatchedTransition?.HandledAtState);
        Assert.Equal(HsmHistoryState.Composite, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(TransitionKind.External, result.MatchedTransition?.Kind);
        Assert.Equal(HsmHistoryState.Second, result.ResolvedTarget);
        Assert.Equal(machine.CurrentState, result.FinalState);
    }

    [Fact]
    public void Deep_history_reports_restored_nested_leaf_and_full_entry_path()
    {
        var extension = new HsmHistoryExtension();
        var machine = new HsmDeepHistoryMachine(HsmHistoryState.Outside, [extension]);
        machine.Start();
        machine.Fire(HsmHistoryTrigger.Enter);
        machine.Fire(HsmHistoryTrigger.Next);
        machine.Fire(HsmHistoryTrigger.Exit);
        extension.Clear();

        Assert.True(machine.TryFire(HsmHistoryTrigger.Enter));

        Assert.Equal(["exit:Outside", "enter:Composite", "enter:Nested", "enter:Second"], extension.StateEvents);
        var result = Assert.Single(extension.Results);
        Assert.Equal(HsmHistoryState.Composite, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(HsmHistoryState.Second, result.ResolvedTarget);
        Assert.Equal(machine.CurrentState, result.FinalState);
    }

    [Fact]
    public void Failure_after_composite_resolution_reports_the_real_active_leaf()
    {
        var extension = new HsmFailureExtension();
        var machine = new HsmResolutionFailureMachine(HsmFailureState.Outside, [extension]);
        machine.Start();

        Assert.Throws<HsmResolutionException>(() => machine.Fire(HsmFailureTrigger.Enter));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(HsmFailureState.Composite, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(HsmFailureState.Leaf, result.ResolvedTarget);
        Assert.Equal(HsmFailureState.Leaf, result.FinalState);
        Assert.Equal(machine.CurrentState, result.FinalState);
    }

    [Fact]
    public void Declared_callbacks_keep_exit_entry_then_action_order()
    {
        var extension = new HsmCallbackExtension();
        var machine = new HsmCallbackExtensibleMachine(HsmCallbackState.Root, [extension]);
        machine.Start();
        machine.Callbacks.Clear();

        Assert.True(machine.TryFire(HsmCallbackTrigger.SwitchBranch));

        Assert.Equal(
            ["exiting:LeftLeaf", "exiting:Left", "callback:OnExit:ExitLeftLeaf",
             "entered:Right", "entered:RightLeaf", "callback:OnEntry:EnterRightLeaf",
             "callback:Action:Act", "completed:Succeeded"],
            extension.Events);
        Assert.Equal(["ExitLeftLeaf", "EnterRightLeaf", "Act"], machine.Callbacks);
    }

    [Fact]
    public void Plain_and_extensible_variants_invoke_the_same_declared_callbacks()
    {
        var extensible = new HsmCallbackExtensibleMachine(HsmCallbackState.Root);
        var plain = new HsmCallbackPlainMachine(HsmCallbackState.Root);
        extensible.Start();
        plain.Start();
        extensible.Callbacks.Clear();
        plain.Callbacks.Clear();

        Assert.True(extensible.TryFire(HsmCallbackTrigger.SwitchBranch));
        Assert.True(plain.TryFire(HsmCallbackTrigger.SwitchBranch));
        Assert.Equal(plain.CurrentState, extensible.CurrentState);
        AssertDeclaredOnly(extensible.Callbacks, "ExitLeftLeaf", "EnterRightLeaf", "Act");
        AssertDeclaredOnly(plain.Callbacks, "ExitLeftLeaf", "EnterRightLeaf", "Act");

        extensible.Callbacks.Clear();
        plain.Callbacks.Clear();
        Assert.True(extensible.TryFire(HsmCallbackTrigger.AncestorTransition));
        Assert.True(plain.TryFire(HsmCallbackTrigger.AncestorTransition));
        Assert.Equal(plain.CurrentState, extensible.CurrentState);
        AssertDeclaredOnly(extensible.Callbacks, "ExitRoot", "EnterRightLeaf", "Act");
        AssertDeclaredOnly(plain.Callbacks, "ExitRoot", "EnterRightLeaf", "Act");
    }

    private static void AssertDeclaredOnly(List<string> actual, params string[] expected)
    {
        Assert.True(expected.ToHashSet().SetEquals(actual));
        Assert.DoesNotContain(actual, name => name is "ExitLeft" or "EnterLeft" or "EnterRoot" or "ExitRight" or "EnterRight");
    }

    private static (HsmSemanticsExtension Extension, HsmSemanticsMachine Machine) Started(HsmSemanticsState initial)
    {
        var extension = new HsmSemanticsExtension();
        var machine = new HsmSemanticsMachine(initial, [extension]);
        machine.Start();
        return (extension, machine);
    }

    private static void AssertResult(
        HsmSemanticsExtension extension,
        HsmSemanticsMachine machine,
        HsmSemanticsState source,
        HsmSemanticsState handledAt,
        HsmSemanticsState? declared,
        HsmSemanticsState? resolved,
        TransitionKind kind)
    {
        var attempt = Assert.Single(extension.Attempts);
        var result = Assert.Single(extension.Results);
        Assert.Equal(source, attempt.SourceState);
        Assert.Equal(handledAt, result.MatchedTransition?.HandledAtState);
        Assert.Equal(declared, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(kind, result.MatchedTransition?.Kind);
        Assert.Equal(resolved, result.ResolvedTarget);
        Assert.Equal(machine.CurrentState, result.FinalState);
        Assert.Equal(TransitionOutcome.Succeeded, result.Outcome);
    }
}

internal static class HsmSemanticsCollectionExtensions
{
    public static int DistinctCount(this IReadOnlyList<string> items)
    {
        var seen = new HashSet<string>();
        foreach (var item in items)
            seen.Add(item);
        return seen.Count;
    }
}

public enum HsmSemanticsState { Outside, Root, Left, LeftLeaf, Right, RightLeaf }
public enum HsmSemanticsTrigger
{
    EnterComposite,
    SwitchBranch,
    AncestorTransition,
    ToActiveDescendant,
    Refresh,
    LeafRefresh,
    Self,
    Missing
}

[StateMachine(
    typeof(HsmSemanticsState),
    typeof(HsmSemanticsTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class HsmSemanticsMachine
{
    [State(HsmSemanticsState.Root)]
    [State(HsmSemanticsState.Left, Parent = HsmSemanticsState.Root, IsInitial = true)]
    [State(HsmSemanticsState.LeftLeaf, Parent = HsmSemanticsState.Left, IsInitial = true)]
    [State(HsmSemanticsState.Right, Parent = HsmSemanticsState.Root)]
    [State(HsmSemanticsState.RightLeaf, Parent = HsmSemanticsState.Right, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(HsmSemanticsState.Outside, HsmSemanticsTrigger.EnterComposite, HsmSemanticsState.Root)]
    [Transition(HsmSemanticsState.LeftLeaf, HsmSemanticsTrigger.SwitchBranch, HsmSemanticsState.RightLeaf)]
    [Transition(HsmSemanticsState.Root, HsmSemanticsTrigger.AncestorTransition, HsmSemanticsState.RightLeaf)]
    [Transition(HsmSemanticsState.Root, HsmSemanticsTrigger.ToActiveDescendant, HsmSemanticsState.LeftLeaf)]
    [Transition(HsmSemanticsState.Root, HsmSemanticsTrigger.Self, HsmSemanticsState.Root)]
    [InternalTransition(HsmSemanticsState.Root, HsmSemanticsTrigger.Refresh, Action = nameof(NoOp))]
    [InternalTransition(HsmSemanticsState.LeftLeaf, HsmSemanticsTrigger.LeafRefresh, Action = nameof(NoOp))]
    private void ConfigureTransitions() { }

    private void NoOp() { }
}

public sealed class HsmSemanticsExtension
    : IStateMachineExtension<HsmSemanticsState, HsmSemanticsTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.States;
    public List<TransitionAttemptContext<HsmSemanticsState, HsmSemanticsTrigger>> Attempts { get; } = [];
    public List<TransitionResult<HsmSemanticsState>> Results { get; } = [];
    public List<string> StateEvents { get; } = [];

    public void OnAttemptStarting(in TransitionAttemptContext<HsmSemanticsState, HsmSemanticsTrigger> attempt)
        => Attempts.Add(attempt);

    public void OnStateExiting(
        in TransitionAttemptContext<HsmSemanticsState, HsmSemanticsTrigger> attempt,
        HsmSemanticsState state)
        => StateEvents.Add($"exit:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<HsmSemanticsState, HsmSemanticsTrigger> attempt,
        HsmSemanticsState state)
        => StateEvents.Add($"enter:{state}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<HsmSemanticsState, HsmSemanticsTrigger> attempt,
        in TransitionResult<HsmSemanticsState> result)
        => Results.Add(result);
}

public enum HsmHistoryState { Outside, Composite, Nested, First, Second }
public enum HsmHistoryTrigger { Enter, Next, Exit }

[StateMachine(
    typeof(HsmHistoryState),
    typeof(HsmHistoryTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class HsmShallowHistoryMachine
{
    [State(HsmHistoryState.Composite, History = HistoryMode.Shallow)]
    [State(HsmHistoryState.First, Parent = HsmHistoryState.Composite, IsInitial = true)]
    [State(HsmHistoryState.Second, Parent = HsmHistoryState.Composite)]
    private void ConfigureStates() { }

    [Transition(HsmHistoryState.Outside, HsmHistoryTrigger.Enter, HsmHistoryState.Composite)]
    [Transition(HsmHistoryState.First, HsmHistoryTrigger.Next, HsmHistoryState.Second)]
    [Transition(HsmHistoryState.Composite, HsmHistoryTrigger.Exit, HsmHistoryState.Outside)]
    private void ConfigureTransitions() { }
}

[StateMachine(
    typeof(HsmHistoryState),
    typeof(HsmHistoryTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class HsmDeepHistoryMachine
{
    [State(HsmHistoryState.Composite, History = HistoryMode.Deep)]
    [State(HsmHistoryState.Nested, Parent = HsmHistoryState.Composite, IsInitial = true)]
    [State(HsmHistoryState.First, Parent = HsmHistoryState.Nested, IsInitial = true)]
    [State(HsmHistoryState.Second, Parent = HsmHistoryState.Nested)]
    private void ConfigureStates() { }

    [Transition(HsmHistoryState.Outside, HsmHistoryTrigger.Enter, HsmHistoryState.Composite)]
    [Transition(HsmHistoryState.First, HsmHistoryTrigger.Next, HsmHistoryState.Second)]
    [Transition(HsmHistoryState.Composite, HsmHistoryTrigger.Exit, HsmHistoryState.Outside)]
    private void ConfigureTransitions() { }
}

public sealed class HsmHistoryExtension
    : IStateMachineExtension<HsmHistoryState, HsmHistoryTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.States;
    public List<TransitionAttemptContext<HsmHistoryState, HsmHistoryTrigger>> Attempts { get; } = [];
    public List<TransitionResult<HsmHistoryState>> Results { get; } = [];
    public List<string> StateEvents { get; } = [];

    public void OnAttemptStarting(in TransitionAttemptContext<HsmHistoryState, HsmHistoryTrigger> attempt)
        => Attempts.Add(attempt);

    public void OnStateExiting(
        in TransitionAttemptContext<HsmHistoryState, HsmHistoryTrigger> attempt,
        HsmHistoryState state)
        => StateEvents.Add($"exit:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<HsmHistoryState, HsmHistoryTrigger> attempt,
        HsmHistoryState state)
        => StateEvents.Add($"enter:{state}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<HsmHistoryState, HsmHistoryTrigger> attempt,
        in TransitionResult<HsmHistoryState> result)
        => Results.Add(result);

    public void Clear()
    {
        Attempts.Clear();
        StateEvents.Clear();
        Results.Clear();
    }
}

public enum HsmFailureState { Outside, Composite, Leaf }
public enum HsmFailureTrigger { Enter }
public sealed class HsmResolutionException : Exception;

[StateMachine(
    typeof(HsmFailureState),
    typeof(HsmFailureTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class HsmResolutionFailureMachine
{
    [State(HsmFailureState.Composite, OnEntry = nameof(ThrowOnEntry))]
    [State(HsmFailureState.Leaf, Parent = HsmFailureState.Composite, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(HsmFailureState.Outside, HsmFailureTrigger.Enter, HsmFailureState.Composite)]
    private void ConfigureTransitions() { }

    private void ThrowOnEntry() => throw new HsmResolutionException();
}

public sealed class HsmFailureExtension
    : IStateMachineExtension<HsmFailureState, HsmFailureTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions;
    public List<TransitionResult<HsmFailureState>> Results { get; } = [];

    public void OnAttemptCompleted(
        in TransitionAttemptContext<HsmFailureState, HsmFailureTrigger> attempt,
        in TransitionResult<HsmFailureState> result)
        => Results.Add(result);
}

public enum HsmCallbackState { Root, Left, LeftLeaf, Right, RightLeaf }
public enum HsmCallbackTrigger { SwitchBranch, AncestorTransition }

[StateMachine(
    typeof(HsmCallbackState),
    typeof(HsmCallbackTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class HsmCallbackExtensibleMachine
{
    public List<string> Callbacks { get; } = [];

    [State(HsmCallbackState.Root, OnExit = nameof(ExitRoot), OnEntry = nameof(EnterRoot))]
    [State(HsmCallbackState.Left, Parent = HsmCallbackState.Root, IsInitial = true, OnExit = nameof(ExitLeft), OnEntry = nameof(EnterLeft))]
    [State(HsmCallbackState.LeftLeaf, Parent = HsmCallbackState.Left, IsInitial = true, OnExit = nameof(ExitLeftLeaf), OnEntry = nameof(EnterLeftLeaf))]
    [State(HsmCallbackState.Right, Parent = HsmCallbackState.Root, OnExit = nameof(ExitRight), OnEntry = nameof(EnterRight))]
    [State(HsmCallbackState.RightLeaf, Parent = HsmCallbackState.Right, IsInitial = true, OnExit = nameof(ExitRightLeaf), OnEntry = nameof(EnterRightLeaf))]
    private void ConfigureStates() { }

    [Transition(HsmCallbackState.LeftLeaf, HsmCallbackTrigger.SwitchBranch, HsmCallbackState.RightLeaf, Action = nameof(Act))]
    [Transition(HsmCallbackState.Root, HsmCallbackTrigger.AncestorTransition, HsmCallbackState.RightLeaf, Action = nameof(Act))]
    private void ConfigureTransitions() { }

    private void ExitRoot() => Callbacks.Add(nameof(ExitRoot));
    private void EnterRoot() => Callbacks.Add(nameof(EnterRoot));
    private void ExitLeft() => Callbacks.Add(nameof(ExitLeft));
    private void EnterLeft() => Callbacks.Add(nameof(EnterLeft));
    private void ExitLeftLeaf() => Callbacks.Add(nameof(ExitLeftLeaf));
    private void EnterLeftLeaf() => Callbacks.Add(nameof(EnterLeftLeaf));
    private void ExitRight() => Callbacks.Add(nameof(ExitRight));
    private void EnterRight() => Callbacks.Add(nameof(EnterRight));
    private void ExitRightLeaf() => Callbacks.Add(nameof(ExitRightLeaf));
    private void EnterRightLeaf() => Callbacks.Add(nameof(EnterRightLeaf));
    private void Act() => Callbacks.Add(nameof(Act));
}

[StateMachine(
    typeof(HsmCallbackState),
    typeof(HsmCallbackTrigger),
    EnableHierarchy = true)]
public partial class HsmCallbackPlainMachine
{
    public List<string> Callbacks { get; } = [];

    [State(HsmCallbackState.Root, OnExit = nameof(ExitRoot), OnEntry = nameof(EnterRoot))]
    [State(HsmCallbackState.Left, Parent = HsmCallbackState.Root, IsInitial = true, OnExit = nameof(ExitLeft), OnEntry = nameof(EnterLeft))]
    [State(HsmCallbackState.LeftLeaf, Parent = HsmCallbackState.Left, IsInitial = true, OnExit = nameof(ExitLeftLeaf), OnEntry = nameof(EnterLeftLeaf))]
    [State(HsmCallbackState.Right, Parent = HsmCallbackState.Root, OnExit = nameof(ExitRight), OnEntry = nameof(EnterRight))]
    [State(HsmCallbackState.RightLeaf, Parent = HsmCallbackState.Right, IsInitial = true, OnExit = nameof(ExitRightLeaf), OnEntry = nameof(EnterRightLeaf))]
    private void ConfigureStates() { }

    [Transition(HsmCallbackState.LeftLeaf, HsmCallbackTrigger.SwitchBranch, HsmCallbackState.RightLeaf, Action = nameof(Act))]
    [Transition(HsmCallbackState.Root, HsmCallbackTrigger.AncestorTransition, HsmCallbackState.RightLeaf, Action = nameof(Act))]
    private void ConfigureTransitions() { }

    private void ExitRoot() => Callbacks.Add(nameof(ExitRoot));
    private void EnterRoot() => Callbacks.Add(nameof(EnterRoot));
    private void ExitLeft() => Callbacks.Add(nameof(ExitLeft));
    private void EnterLeft() => Callbacks.Add(nameof(EnterLeft));
    private void ExitLeftLeaf() => Callbacks.Add(nameof(ExitLeftLeaf));
    private void EnterLeftLeaf() => Callbacks.Add(nameof(EnterLeftLeaf));
    private void ExitRight() => Callbacks.Add(nameof(ExitRight));
    private void EnterRight() => Callbacks.Add(nameof(EnterRight));
    private void ExitRightLeaf() => Callbacks.Add(nameof(ExitRightLeaf));
    private void EnterRightLeaf() => Callbacks.Add(nameof(EnterRightLeaf));
    private void Act() => Callbacks.Add(nameof(Act));
}

public sealed class HsmCallbackExtension : IStateMachineExtension<HsmCallbackState, HsmCallbackTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.All;
    public List<string> Events { get; } = [];

    public void OnStateExiting(
        in TransitionAttemptContext<HsmCallbackState, HsmCallbackTrigger> attempt,
        HsmCallbackState state)
        => Events.Add($"exiting:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<HsmCallbackState, HsmCallbackTrigger> attempt,
        HsmCallbackState state)
        => Events.Add($"entered:{state}");

    public void OnCallbackExecuting(
        in TransitionAttemptContext<HsmCallbackState, HsmCallbackTrigger> attempt,
        FastFsm.Exceptions.TransitionStage stage,
        string callbackName)
        => Events.Add($"callback:{stage}:{callbackName}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<HsmCallbackState, HsmCallbackTrigger> attempt,
        in TransitionResult<HsmCallbackState> result)
        => Events.Add($"completed:{result.Outcome}");
}
