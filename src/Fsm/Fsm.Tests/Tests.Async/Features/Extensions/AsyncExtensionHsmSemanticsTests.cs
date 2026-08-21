using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Async.Features.Extensions;

public sealed class AsyncExtensionHsmSemanticsTests
{
    [Fact]
    public async Task Sibling_and_ancestor_paths_match_the_sync_lifecycle_and_result_model()
    {
        var extension = new AsyncHsmSemanticsExtension();
        var machine = new AsyncHsmSemanticsMachine(AsyncHsmSemanticsState.Root, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.SwitchBranch));
        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "enter:Right", "enter:RightLeaf"],
            extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.RightLeaf,
            AsyncHsmSemanticsState.RightLeaf,
            TransitionKind.External);

        extension.Clear();
        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.AncestorTransition));
        Assert.Equal(
            ["exit:RightLeaf", "exit:Right", "exit:Root", "enter:Root", "enter:Right", "enter:RightLeaf"],
            extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.RightLeaf,
            AsyncHsmSemanticsState.Root,
            AsyncHsmSemanticsState.RightLeaf,
            AsyncHsmSemanticsState.RightLeaf,
            TransitionKind.External);
    }

    [Fact]
    public async Task Ancestor_to_descendant_and_self_transition_exit_the_owning_subtree()
    {
        var extension = new AsyncHsmSemanticsExtension();
        var machine = new AsyncHsmSemanticsMachine(AsyncHsmSemanticsState.Root, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.ToActiveDescendant));
        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "exit:Root", "enter:Root", "enter:Left", "enter:LeftLeaf"],
            extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.Root,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.LeftLeaf,
            TransitionKind.External);

        extension.Clear();
        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.Self));
        Assert.Equal(
            ["exit:LeftLeaf", "exit:Left", "exit:Root", "enter:Root", "enter:Left", "enter:LeftLeaf"],
            extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.Root,
            AsyncHsmSemanticsState.Root,
            AsyncHsmSemanticsState.LeftLeaf,
            TransitionKind.External);
    }

    [Fact]
    public async Task Internal_transitions_have_no_targets_or_state_hooks()
    {
        var extension = new AsyncHsmSemanticsExtension();
        var machine = new AsyncHsmSemanticsMachine(AsyncHsmSemanticsState.Root, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.Refresh));
        Assert.Empty(extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.Root,
            declared: null,
            resolved: null,
            TransitionKind.Internal);

        extension.Clear();
        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.LeafRefresh));
        Assert.Empty(extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.LeftLeaf,
            AsyncHsmSemanticsState.LeftLeaf,
            declared: null,
            resolved: null,
            TransitionKind.Internal);
    }

    [Fact]
    public async Task Composite_and_history_targets_report_the_resolved_leaf()
    {
        var extension = new AsyncHsmSemanticsExtension();
        var machine = new AsyncHsmSemanticsMachine(AsyncHsmSemanticsState.Outside, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncHsmSemanticsTrigger.EnterComposite));
        Assert.Equal(["exit:Outside", "enter:Root", "enter:Left", "enter:LeftLeaf"], extension.StateEvents);
        AssertSucceeded(
            extension,
            machine,
            AsyncHsmSemanticsState.Outside,
            AsyncHsmSemanticsState.Outside,
            AsyncHsmSemanticsState.Root,
            AsyncHsmSemanticsState.LeftLeaf,
            TransitionKind.External);

        var history = new AsyncHsmHistoryExtension();
        var deep = new AsyncHsmDeepHistoryMachine(AsyncHsmHistoryState.Outside, [history]);
        await deep.StartAsync();
        await deep.FireAsync(AsyncHsmHistoryTrigger.Enter);
        await deep.FireAsync(AsyncHsmHistoryTrigger.Next);
        await deep.FireAsync(AsyncHsmHistoryTrigger.Exit);
        history.Clear();

        Assert.True(await deep.TryFireAsync(AsyncHsmHistoryTrigger.Enter));
        Assert.Equal(["exit:Outside", "enter:Composite", "enter:Nested", "enter:Second"], history.StateEvents);
        var restored = Assert.Single(history.Results);
        Assert.Equal(AsyncHsmHistoryState.Composite, restored.MatchedTransition?.DeclaredTarget);
        Assert.Equal(AsyncHsmHistoryState.Second, restored.ResolvedTarget);
        Assert.Equal(deep.CurrentState, restored.FinalState);
    }

    [Fact]
    public async Task Failure_after_resolution_reports_the_active_leaf()
    {
        var extension = new AsyncHsmFailureExtension();
        var machine = new AsyncHsmResolutionFailureMachine(AsyncHsmFailureState.Outside, [extension]);
        await machine.StartAsync();

        await Assert.ThrowsAsync<AsyncHsmResolutionException>(
            async () => await machine.FireAsync(AsyncHsmFailureTrigger.Enter));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(AsyncHsmFailureState.Composite, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(AsyncHsmFailureState.Leaf, result.ResolvedTarget);
        Assert.Equal(AsyncHsmFailureState.Leaf, result.FinalState);
        Assert.Equal(machine.CurrentState, result.FinalState);
    }

    private static void AssertSucceeded(
        AsyncHsmSemanticsExtension extension,
        AsyncHsmSemanticsMachine machine,
        AsyncHsmSemanticsState source,
        AsyncHsmSemanticsState handledAt,
        AsyncHsmSemanticsState? declared,
        AsyncHsmSemanticsState? resolved,
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

public enum AsyncHsmSemanticsState { Outside, Root, Left, LeftLeaf, Right, RightLeaf }
public enum AsyncHsmSemanticsTrigger
{
    EnterComposite,
    SwitchBranch,
    AncestorTransition,
    ToActiveDescendant,
    Refresh,
    LeafRefresh,
    Self
}

[StateMachine(
    typeof(AsyncHsmSemanticsState),
    typeof(AsyncHsmSemanticsTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class AsyncHsmSemanticsMachine
{
    [State(AsyncHsmSemanticsState.Root)]
    [State(AsyncHsmSemanticsState.Left, Parent = AsyncHsmSemanticsState.Root, IsInitial = true)]
    [State(AsyncHsmSemanticsState.LeftLeaf, Parent = AsyncHsmSemanticsState.Left, IsInitial = true)]
    [State(AsyncHsmSemanticsState.Right, Parent = AsyncHsmSemanticsState.Root)]
    [State(AsyncHsmSemanticsState.RightLeaf, Parent = AsyncHsmSemanticsState.Right, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(AsyncHsmSemanticsState.Outside, AsyncHsmSemanticsTrigger.EnterComposite, AsyncHsmSemanticsState.Root, Action = nameof(NoOpAsync))]
    [Transition(AsyncHsmSemanticsState.LeftLeaf, AsyncHsmSemanticsTrigger.SwitchBranch, AsyncHsmSemanticsState.RightLeaf, Action = nameof(NoOpAsync))]
    [Transition(AsyncHsmSemanticsState.Root, AsyncHsmSemanticsTrigger.AncestorTransition, AsyncHsmSemanticsState.RightLeaf, Action = nameof(NoOpAsync))]
    [Transition(AsyncHsmSemanticsState.Root, AsyncHsmSemanticsTrigger.ToActiveDescendant, AsyncHsmSemanticsState.LeftLeaf, Action = nameof(NoOpAsync))]
    [Transition(AsyncHsmSemanticsState.Root, AsyncHsmSemanticsTrigger.Self, AsyncHsmSemanticsState.Root, Action = nameof(NoOpAsync))]
    [InternalTransition(AsyncHsmSemanticsState.Root, AsyncHsmSemanticsTrigger.Refresh, Action = nameof(NoOpAsync))]
    [InternalTransition(AsyncHsmSemanticsState.LeftLeaf, AsyncHsmSemanticsTrigger.LeafRefresh, Action = nameof(NoOpAsync))]
    private void ConfigureTransitions() { }

    private ValueTask NoOpAsync() => ValueTask.CompletedTask;
}

public sealed class AsyncHsmSemanticsExtension
    : IStateMachineExtension<AsyncHsmSemanticsState, AsyncHsmSemanticsTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.States;
    public List<TransitionAttemptContext<AsyncHsmSemanticsState, AsyncHsmSemanticsTrigger>> Attempts { get; } = [];
    public List<TransitionResult<AsyncHsmSemanticsState>> Results { get; } = [];
    public List<string> StateEvents { get; } = [];

    public void OnAttemptStarting(in TransitionAttemptContext<AsyncHsmSemanticsState, AsyncHsmSemanticsTrigger> attempt)
        => Attempts.Add(attempt);

    public void OnStateExiting(
        in TransitionAttemptContext<AsyncHsmSemanticsState, AsyncHsmSemanticsTrigger> attempt,
        AsyncHsmSemanticsState state)
        => StateEvents.Add($"exit:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<AsyncHsmSemanticsState, AsyncHsmSemanticsTrigger> attempt,
        AsyncHsmSemanticsState state)
        => StateEvents.Add($"enter:{state}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<AsyncHsmSemanticsState, AsyncHsmSemanticsTrigger> attempt,
        in TransitionResult<AsyncHsmSemanticsState> result)
        => Results.Add(result);

    public void Clear()
    {
        Attempts.Clear();
        Results.Clear();
        StateEvents.Clear();
    }
}

public enum AsyncHsmHistoryState { Outside, Composite, Nested, First, Second }
public enum AsyncHsmHistoryTrigger { Enter, Next, Exit }

[StateMachine(
    typeof(AsyncHsmHistoryState),
    typeof(AsyncHsmHistoryTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class AsyncHsmDeepHistoryMachine
{
    [State(AsyncHsmHistoryState.Composite, History = HistoryMode.Deep)]
    [State(AsyncHsmHistoryState.Nested, Parent = AsyncHsmHistoryState.Composite, IsInitial = true)]
    [State(AsyncHsmHistoryState.First, Parent = AsyncHsmHistoryState.Nested, IsInitial = true)]
    [State(AsyncHsmHistoryState.Second, Parent = AsyncHsmHistoryState.Nested)]
    private void ConfigureStates() { }

    [Transition(AsyncHsmHistoryState.Outside, AsyncHsmHistoryTrigger.Enter, AsyncHsmHistoryState.Composite, Action = nameof(NoOpAsync))]
    [Transition(AsyncHsmHistoryState.First, AsyncHsmHistoryTrigger.Next, AsyncHsmHistoryState.Second, Action = nameof(NoOpAsync))]
    [Transition(AsyncHsmHistoryState.Composite, AsyncHsmHistoryTrigger.Exit, AsyncHsmHistoryState.Outside, Action = nameof(NoOpAsync))]
    private void ConfigureTransitions() { }

    private ValueTask NoOpAsync() => ValueTask.CompletedTask;
}

public sealed class AsyncHsmHistoryExtension
    : IStateMachineExtension<AsyncHsmHistoryState, AsyncHsmHistoryTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.States;
    public List<TransitionResult<AsyncHsmHistoryState>> Results { get; } = [];
    public List<string> StateEvents { get; } = [];

    public void OnStateExiting(
        in TransitionAttemptContext<AsyncHsmHistoryState, AsyncHsmHistoryTrigger> attempt,
        AsyncHsmHistoryState state)
        => StateEvents.Add($"exit:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<AsyncHsmHistoryState, AsyncHsmHistoryTrigger> attempt,
        AsyncHsmHistoryState state)
        => StateEvents.Add($"enter:{state}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<AsyncHsmHistoryState, AsyncHsmHistoryTrigger> attempt,
        in TransitionResult<AsyncHsmHistoryState> result)
        => Results.Add(result);

    public void Clear()
    {
        StateEvents.Clear();
        Results.Clear();
    }
}

public enum AsyncHsmFailureState { Outside, Composite, Leaf }
public enum AsyncHsmFailureTrigger { Enter }
public sealed class AsyncHsmResolutionException : Exception;

[StateMachine(
    typeof(AsyncHsmFailureState),
    typeof(AsyncHsmFailureTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class AsyncHsmResolutionFailureMachine
{
    [State(AsyncHsmFailureState.Composite, OnEntry = nameof(ThrowOnEntryAsync))]
    [State(AsyncHsmFailureState.Leaf, Parent = AsyncHsmFailureState.Composite, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(AsyncHsmFailureState.Outside, AsyncHsmFailureTrigger.Enter, AsyncHsmFailureState.Composite)]
    private void ConfigureTransitions() { }

    private ValueTask ThrowOnEntryAsync() => throw new AsyncHsmResolutionException();
}

public sealed class AsyncHsmFailureExtension
    : IStateMachineExtension<AsyncHsmFailureState, AsyncHsmFailureTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions;
    public List<TransitionResult<AsyncHsmFailureState>> Results { get; } = [];

    public void OnAttemptCompleted(
        in TransitionAttemptContext<AsyncHsmFailureState, AsyncHsmFailureTrigger> attempt,
        in TransitionResult<AsyncHsmFailureState> result)
        => Results.Add(result);
}
