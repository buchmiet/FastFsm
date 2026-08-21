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
    public async Task Success_guard_rejection_and_unhandled_paths_preserve_v1_context_and_order()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Go));
        Assert.Equal(
            ["Before", "GuardEvaluating:CanGoAsync", "GuardEvaluated:CanGoAsync:True", "Transitioned", "After:True"],
            extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.A, e.From));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.B, e.To));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].InstanceId, e.InstanceId));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].Timestamp, e.Timestamp));

        extension.Events.Clear();
        var rejected = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await rejected.StartAsync();
        Assert.False(await rejected.TryFireAsync(AsyncCharacterizationTrigger.Reject));
        Assert.Equal(
            ["Before", "GuardEvaluating:CannotGoAsync", "GuardEvaluated:CannotGoAsync:False", "After:False"],
            extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.B, e.To));

        extension.Events.Clear();
        Assert.False(await rejected.TryFireAsync(AsyncCharacterizationTrigger.Missing));
        Assert.Equal(["Unhandled", "After:False"], extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.A, e.To));
    }

    [Theory]
    [InlineData(AsyncCharacterizationTrigger.Internal)]
    [InlineData(AsyncCharacterizationTrigger.Self)]
    public async Task Internal_and_external_self_transition_are_indistinguishable(
        AsyncCharacterizationTrigger trigger)
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationFlatMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(trigger));

        Assert.Equal(["Before", "Internal", "Transitioned", "After:True"], extension.Events.Select(e => e.Name));
    }

    [Fact]
    public async Task Payload_is_exposed_on_every_hook_and_invalid_payload_emits_no_hooks()
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
        Assert.Empty(extension.Events);
        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.AlternatePayload, invalid));
        Assert.All(extension.Events, e => Assert.Same(invalid, e.Payload));
    }

    [Fact]
    public async Task Composite_target_and_ancestor_internal_preserve_v1_hsm_semantics()
    {
        var extension = new AsyncCharacterizationExtension();
        var machine = new AsyncCharacterizationHsmMachine(AsyncCharacterizationState.A, [extension]);
        await machine.StartAsync();

        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.EnterComposite));
        Assert.Equal(AsyncCharacterizationState.Child1, machine.CurrentState);
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.Parent, e.To));

        extension.Events.Clear();
        Assert.True(await machine.TryFireAsync(AsyncCharacterizationTrigger.Internal));
        Assert.Equal(["Before", "Transitioned", "After:True"], extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.Child1, e.From));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.Parent, e.To));
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
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.B, e.From));
        Assert.All(extension.Events, e => Assert.Equal(AsyncCharacterizationState.Parent, e.To));
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
    string InstanceId,
    DateTime Timestamp,
    object From,
    object To,
    object Trigger,
    object? Payload);

public sealed class AsyncCharacterizationExtension : IStateMachineExtension
{
    public List<AsyncCharacterizationEvent> Events { get; } = [];

    public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext => Add("Before", context);
    public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext => Add($"After:{success}", context);
    public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext => Add($"GuardEvaluating:{guardName}", context);
    public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext => Add($"GuardEvaluated:{guardName}:{result}", context);
    public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext => Add("Unhandled", context);
    public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext => Add("Internal", context);
    public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext => Add("Transitioned", context);

    private void Add<TContext>(string name, TContext context) where TContext : IStateMachineContext
    {
        var snapshot = Assert.IsAssignableFrom<IStateSnapshot>(context);
        var payload = context is IStateMachineContext<AsyncCharacterizationState, AsyncCharacterizationTrigger> typed
            ? typed.Payload
            : null;
        Events.Add(new AsyncCharacterizationEvent(
            name,
            context.InstanceId,
            context.Timestamp,
            snapshot.FromState,
            snapshot.ToState,
            snapshot.Trigger,
            payload));
    }
}