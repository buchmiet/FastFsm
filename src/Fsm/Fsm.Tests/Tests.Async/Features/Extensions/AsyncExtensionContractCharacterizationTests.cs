using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Async.Features.Extensions;

public sealed class AsyncExtensionContractCharacterizationTests
{
    [Fact]
    public async Task Pre_cancelled_token_does_not_start_an_attempt()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            machine.TryFireAsync(AsyncCharacterizationTrigger.Go, cancellationToken: cts.Token).AsTask());

        Assert.Empty(extension.Events);
    }

    [Fact]
    public async Task Success_guard_rejection_and_unhandled_paths_expose_v2_context_outcome_and_order()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Go));
        Assert.Equal(
            ["OnAttemptStarting", "OnTransitionMatched:External", "OnGuardEvaluating:CanGoAsync", "OnGuardEvaluated:CanGoAsync:True", "OnAttemptCompleted:Succeeded"],
            extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.A, e.SourceState));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].InstanceId, e.InstanceId));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].AttemptId, e.AttemptId));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].StartTimestamp, e.StartTimestamp));
        Assert.Equal(AsyncCharacterizationState.B, extension.Events[^1].Result?.MatchedTransition?.DeclaredTarget);
        Assert.Equal(AsyncCharacterizationState.B, extension.Events[^1].Result?.ResolvedTarget);
        Assert.Equal(AsyncCharacterizationState.B, extension.Events[^1].Result?.FinalState);

        extension.Events.Clear();
        var rejected = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await rejected.StartAsync();
        Assert.False(await rejected.TryFireAsync(AsyncCharacterizationTrigger.Reject));
        Assert.Equal(
            ["OnAttemptStarting", "OnTransitionMatched:External", "OnGuardEvaluating:CannotGoAsync", "OnGuardEvaluated:CannotGoAsync:False", "OnAttemptCompleted:GuardRejected"],
            extension.Events.Select(e => e.Name));
        Assert.Equal(AsyncCharacterizationState.B, extension.Events[^1].Result?.MatchedTransition?.DeclaredTarget);
        Assert.Null(extension.Events[^1].Result?.ResolvedTarget);
        Assert.Equal(AsyncCharacterizationState.A, extension.Events[^1].Result?.FinalState);

        extension.Events.Clear();
        Assert.False(await rejected.TryFireAsync(AsyncCharacterizationTrigger.Missing));
        Assert.Equal(["OnAttemptStarting", "OnAttemptCompleted:UnhandledTrigger"], extension.Events.Select(e => e.Name));
        Assert.Null(extension.Events[^1].Result?.MatchedTransition);
        Assert.Equal(AsyncCharacterizationState.A, extension.Events[^1].Result?.FinalState);
    }

    [Theory]
    [InlineData(AsyncCharacterizationTrigger.Internal)]
    [InlineData(AsyncCharacterizationTrigger.Self)]
    public async Task Internal_and_external_self_transition_are_distinct(
        AsyncCharacterizationTrigger trigger)
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(trigger));

        var expectedKind = trigger == AsyncCharacterizationTrigger.Internal
            ? TransitionKind.Internal
            : TransitionKind.External;
        Assert.Equal(
            ["OnAttemptStarting", $"OnTransitionMatched:{expectedKind}", "OnAttemptCompleted:Succeeded"],
            extension.Events.Select(e => e.Name));
        Assert.Equal(expectedKind, extension.Events[^1].Result?.MatchedTransition?.Kind);
        if (expectedKind == TransitionKind.Internal)
            Assert.Null(extension.Events[^1].Result?.ResolvedTarget);
        else
            Assert.Equal(AsyncCharacterizationState.A, extension.Events[^1].Result?.ResolvedTarget);
    }

    [Fact]
    public async Task Payload_is_exposed_on_every_hook_and_invalid_payload_reports_typed_outcome()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationPayloadMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();
        var payload = new AsyncCharacterizationPayload(42);

        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Payload, payload));
        Assert.All(extension.Events, e => Assert.Same(payload, e.Payload));

        extension.Events.Clear();
        var invalid = new AlternateAsyncCharacterizationPayload("wrong");
        Assert.False(await machine.TryFireAsync(AsyncCharacterizationTrigger.AlternatePayload, payload));
        Assert.Equal(["OnAttemptStarting", "OnAttemptCompleted:InvalidPayload"], extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Same(payload, e.Payload));
        Assert.Equal(TransitionOutcome.InvalidPayload, extension.Events[^1].Result?.Outcome);

        extension.Events.Clear();
        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.AlternatePayload, invalid));
        Assert.All(extension.Events, e => Assert.Same(invalid, e.Payload));
    }

    [Fact]
    public async Task Composite_target_and_ancestor_internal_expose_declared_and_resolved_v2_semantics()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationHsmMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.EnterComposite));
        Assert.Equal(AsyncCharacterizationState.Child1, machine.CurrentState);
        Assert.Equal(AsyncCharacterizationState.Parent, extension.Events[^1].Result?.MatchedTransition?.DeclaredTarget);
        Assert.Equal(AsyncCharacterizationState.Child1, extension.Events[^1].Result?.ResolvedTarget);
        Assert.Equal(AsyncCharacterizationState.Child1, extension.Events[^1].Result?.FinalState);

        extension.Events.Clear();
        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Internal));
        Assert.Equal(
            ["OnAttemptStarting", "OnTransitionMatched:Internal", "OnAttemptCompleted:Succeeded"],
            extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.Child1, e.SourceState));
        Assert.Equal(AsyncCharacterizationState.Parent, extension.Events[^1].Result?.MatchedTransition?.HandledAtState);
        Assert.Null(extension.Events[^1].Result?.MatchedTransition?.DeclaredTarget);
        Assert.Null(extension.Events[^1].Result?.ResolvedTarget);
        Assert.Equal(AsyncCharacterizationState.Child1, extension.Events[^1].Result?.FinalState);
    }

    [Fact]
    public async Task Shallow_history_resolves_composite_target_after_context_is_created()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationHsmMachine(AsyncCharacterizationState.Parent, [extension]);
        await machine.StartAsync();
        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.MoveChild));
        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Ancestor));
        extension.Events.Clear();

        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Return));

        Assert.Equal(AsyncCharacterizationState.Child2, machine.CurrentState);
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.B, e.SourceState));
        Assert.Equal(AsyncCharacterizationState.Parent, extension.Events[^1].Result?.MatchedTransition?.DeclaredTarget);
        Assert.Equal(AsyncCharacterizationState.Child2, extension.Events[^1].Result?.ResolvedTarget);
        Assert.Equal(AsyncCharacterizationState.Child2, extension.Events[^1].Result?.FinalState);
    }
}

public enum AsyncCharacterizationState { A, B, Parent, Child1, Child2 }
public enum AsyncCharacterizationTrigger
{
    Go,
    Reject,
    Missing,
    Internal,
    Self,
    Payload,
    AlternatePayload,
    EnterComposite,
    MoveChild,
    Ancestor,
    Return
}

public sealed record AsyncCharacterizationPayload(int Value);
public sealed record AlternateAsyncCharacterizationPayload(string Value);

[StateMachine(typeof(AsyncCharacterizationState), typeof(AsyncCharacterizationTrigger), GenerateExtensibleVersion = true)]
public partial class AsyncCharacterizationFlatMachine
{
    [Transition(AsyncCharacterizationState.A, AsyncCharacterizationTrigger.Go, AsyncCharacterizationState.B, Guard = nameof(CanGoAsync), Action = nameof(ActionAsync))]
    [Transition(AsyncCharacterizationState.A, AsyncCharacterizationTrigger.Reject, AsyncCharacterizationState.B, Guard = nameof(CannotGoAsync), Action = nameof(ActionAsync))]
    [Transition(AsyncCharacterizationState.A, AsyncCharacterizationTrigger.Self, AsyncCharacterizationState.A, Action = nameof(ActionAsync))]
    [InternalTransition(AsyncCharacterizationState.A, AsyncCharacterizationTrigger.Internal, Action = nameof(ActionAsync))]
    private void Configure() { }

    private async ValueTask<bool> CanGoAsync() { await Task.Yield(); return true; }
    private async ValueTask<bool> CannotGoAsync() { await Task.Yield(); return false; }
    private async ValueTask ActionAsync() => await Task.Yield();
}

[StateMachine(typeof(AsyncCharacterizationState), typeof(AsyncCharacterizationTrigger), GenerateExtensibleVersion = true)]
[PayloadType(AsyncCharacterizationTrigger.Payload, typeof(AsyncCharacterizationPayload))]
[PayloadType(AsyncCharacterizationTrigger.AlternatePayload, typeof(AlternateAsyncCharacterizationPayload))]
public partial class AsyncCharacterizationPayloadMachine
{
    [Transition(AsyncCharacterizationState.A, AsyncCharacterizationTrigger.Payload, AsyncCharacterizationState.B, Action = nameof(FirstAsync))]
    [Transition(AsyncCharacterizationState.B, AsyncCharacterizationTrigger.AlternatePayload, AsyncCharacterizationState.A, Action = nameof(SecondAsync))]
    private void Configure() { }

    private async ValueTask FirstAsync(AsyncCharacterizationPayload payload) => await Task.Yield();
    private async ValueTask SecondAsync(AlternateAsyncCharacterizationPayload payload) => await Task.Yield();
}

[StateMachine(
    typeof(AsyncCharacterizationState),
    typeof(AsyncCharacterizationTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class AsyncCharacterizationHsmMachine
{
    [State(AsyncCharacterizationState.Parent, History = HistoryMode.Shallow)]
    [State(AsyncCharacterizationState.Child1, Parent = AsyncCharacterizationState.Parent, IsInitial = true)]
    [State(AsyncCharacterizationState.Child2, Parent = AsyncCharacterizationState.Parent)]
    private void ConfigureStates() { }

    [Transition(AsyncCharacterizationState.A, AsyncCharacterizationTrigger.EnterComposite, AsyncCharacterizationState.Parent, Action = nameof(ActionAsync))]
    [Transition(AsyncCharacterizationState.Child1, AsyncCharacterizationTrigger.MoveChild, AsyncCharacterizationState.Child2, Action = nameof(ActionAsync))]
    [Transition(AsyncCharacterizationState.Parent, AsyncCharacterizationTrigger.Ancestor, AsyncCharacterizationState.B, Action = nameof(ActionAsync))]
    [Transition(AsyncCharacterizationState.B, AsyncCharacterizationTrigger.Return, AsyncCharacterizationState.Parent, Action = nameof(ActionAsync))]
    [InternalTransition(AsyncCharacterizationState.Parent, AsyncCharacterizationTrigger.Internal, Action = nameof(ActionAsync))]
    private void ConfigureTransitions() { }

    private async ValueTask ActionAsync() => await Task.Yield();
}

public sealed record AsyncCharacterizationEvent(
    string Name,
    Guid InstanceId,
    long AttemptId,
    long StartTimestamp,
    AsyncCharacterizationState SourceState,
    AsyncCharacterizationTrigger Trigger,
    object? Payload,
    TransitionInfo<AsyncCharacterizationState>? Transition,
    TransitionResult<AsyncCharacterizationState>? Result);

public sealed class AsyncCharacterizationExtension : IStateMachineExtension<AsyncCharacterizationState, AsyncCharacterizationTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;
    public List<AsyncCharacterizationEvent> Events { get; } = [];

    public void OnAttemptStarting(in TransitionAttemptContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> attempt)
        => Add("OnAttemptStarting", in attempt);

    public void OnTransitionMatched(
        in TransitionAttemptContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> attempt,
        in TransitionInfo<AsyncCharacterizationState> matched)
        => Add($"OnTransitionMatched:{matched.Kind}", in attempt, matched);

    public void OnAttemptCompleted(
        in TransitionAttemptContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> attempt,
        in TransitionResult<AsyncCharacterizationState> result)
        => Add($"OnAttemptCompleted:{result.Outcome}", in attempt, result.MatchedTransition, result);

    public void OnGuardEvaluating(
        in TransitionAttemptContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> attempt,
        in TransitionInfo<AsyncCharacterizationState> candidate,
        string guardName)
        => Add($"OnGuardEvaluating:{guardName}", in attempt, candidate);

    public void OnGuardEvaluated(
        in TransitionAttemptContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> attempt,
        in TransitionInfo<AsyncCharacterizationState> candidate,
        string guardName,
        bool result)
        => Add($"OnGuardEvaluated:{guardName}:{result}", in attempt, candidate);

    private void Add(
        string name,
        in TransitionAttemptContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> attempt,
        TransitionInfo<AsyncCharacterizationState>? transition = null,
        TransitionResult<AsyncCharacterizationState>? result = null)
        => Events.Add(new AsyncCharacterizationEvent(
            name,
            attempt.InstanceId,
            attempt.AttemptId,
            attempt.StartTimestamp,
            attempt.SourceState,
            attempt.Trigger,
            attempt.Payload,
            transition,
            result));
}