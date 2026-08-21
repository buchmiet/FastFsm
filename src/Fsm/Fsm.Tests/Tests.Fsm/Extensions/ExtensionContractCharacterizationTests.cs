using System;
using System.Collections.Generic;
using System.Linq;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Extensions;

public sealed class ExtensionContractCharacterizationTests
{
    [Fact]
    public void Flat_success_exposes_one_stable_context_and_complete_hook_order()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(CharacterizationTrigger.Go));

        Assert.Equal(
            ["Before", "GuardEvaluating:CanGo", "GuardEvaluated:CanGo:True", "Transitioned", "After:True"],
            extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].InstanceId, e.InstanceId));
        Assert.All(extension.Events, e => Assert.Equal(extension.Events[0].Timestamp, e.Timestamp));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.A, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.B, e.To));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationTrigger.Go, e.Trigger));
        Assert.All(extension.Events, e => Assert.Null(e.Payload));
    }

    [Fact]
    public void Guard_rejection_retains_declared_target_and_has_no_success_hooks()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.False(machine.TryFire(CharacterizationTrigger.Reject));

        Assert.Equal(
            ["Before", "GuardEvaluating:CannotGo", "GuardEvaluated:CannotGo:False", "After:False"],
            extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.A, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.B, e.To));
        Assert.Equal(CharacterizationState.A, machine.CurrentState);
    }

    [Fact]
    public void Unhandled_trigger_reports_source_as_target_and_no_transitioned_hook()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.False(machine.TryFire(CharacterizationTrigger.Missing));

        Assert.Equal(["Unhandled", "After:False"], extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.A, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.A, e.To));
    }

    [Theory]
    [InlineData(CharacterizationTrigger.Internal)]
    [InlineData(CharacterizationTrigger.Self)]
    public void Internal_and_external_self_transition_are_indistinguishable_to_v1_extensions(
        CharacterizationTrigger trigger)
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(trigger));

        Assert.Equal(["Before", "Internal", "Transitioned", "After:True"], extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.A, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.A, e.To));
    }

    [Fact]
    public void Single_and_multi_payloads_are_exposed_as_the_original_object()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationPayloadMachine(CharacterizationState.A, [extension]);
        machine.Start();
        var first = new CharacterizationPayload(42);

        Assert.True(machine.TryFire(CharacterizationTrigger.Payload, first));
        Assert.All(extension.Events, e => Assert.Same(first, e.Payload));

        extension.Events.Clear();
        var second = new AlternateCharacterizationPayload("ok");
        Assert.True(machine.TryFire(CharacterizationTrigger.AlternatePayload, second));
        Assert.All(extension.Events, e => Assert.Same(second, e.Payload));
    }

    [Fact]
    public void Composite_and_ancestor_targets_describe_declared_not_effective_state()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationHsmMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(CharacterizationTrigger.EnterComposite));
        Assert.Equal(CharacterizationState.Child1, machine.CurrentState);
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.Parent, e.To));

        extension.Events.Clear();
        Assert.True(machine.TryFire(CharacterizationTrigger.Ancestor));
        Assert.Equal(CharacterizationState.B, machine.CurrentState);
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.Child1, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.B, e.To));
    }

    [Fact]
    public void Internal_transition_on_ancestor_reports_a_state_change_that_never_happened()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationHsmMachine(CharacterizationState.Parent, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(CharacterizationTrigger.Internal));

        Assert.Equal(CharacterizationState.Child1, machine.CurrentState);
        Assert.Equal(["Before", "Transitioned", "After:True"], extension.Events.Select(e => e.Name));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.Child1, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.Parent, e.To));
    }

    [Fact]
    public void Shallow_history_resolves_composite_target_after_context_is_created()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationHsmMachine(CharacterizationState.Parent, [extension]);
        machine.Start();
        Assert.True(machine.TryFire(CharacterizationTrigger.MoveChild));
        Assert.Equal(CharacterizationState.Child2, machine.CurrentState);
        Assert.True(machine.TryFire(CharacterizationTrigger.Ancestor));
        extension.Events.Clear();

        Assert.True(machine.TryFire(CharacterizationTrigger.Return));

        Assert.Equal(CharacterizationState.Child2, machine.CurrentState);
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.B, e.From));
        Assert.All(extension.Events, e => Assert.Equal(CharacterizationState.Parent, e.To));
    }

    [Fact]
    public void Instance_identity_is_stable_during_one_attempt_but_changes_between_attempts()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        machine.TryFire(CharacterizationTrigger.Missing);
        var firstAttemptId = extension.Events[0].InstanceId;
        Assert.All(extension.Events, e => Assert.Equal(firstAttemptId, e.InstanceId));

        extension.Events.Clear();
        machine.TryFire(CharacterizationTrigger.Missing);

        Assert.NotEmpty(firstAttemptId);
        Assert.NotEqual(firstAttemptId, extension.Events[0].InstanceId);
    }

    [Fact]
    public void Extensions_run_in_registration_order_and_faults_are_isolated()
    {
        var order = new List<string>();
        var first = new OrderingExtension("first", order, throwOnBefore: true);
        var second = new OrderingExtension("second", order, throwOnBefore: false);
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [first, second]);
        machine.Start();

        Assert.True(machine.TryFire(CharacterizationTrigger.Go));
        Assert.Equal(["first", "second"], order);
    }
}

public enum CharacterizationState { A, B, Parent, Child1, Child2 }
public enum CharacterizationTrigger
{
    Go,
    Reject,
    Missing,
    Internal,
    Self,
    Payload,
    AlternatePayload,
    EnterComposite,
    Ancestor,
    MoveChild,
    Return
}

public sealed record CharacterizationPayload(int Value);
public sealed record AlternateCharacterizationPayload(string Value);

[StateMachine(typeof(CharacterizationState), typeof(CharacterizationTrigger), GenerateExtensibleVersion = true)]
public partial class CharacterizationFlatMachine
{
    [Transition(CharacterizationState.A, CharacterizationTrigger.Go, CharacterizationState.B, Guard = nameof(CanGo))]
    [Transition(CharacterizationState.A, CharacterizationTrigger.Reject, CharacterizationState.B, Guard = nameof(CannotGo))]
    [Transition(CharacterizationState.A, CharacterizationTrigger.Self, CharacterizationState.A)]
    [InternalTransition(CharacterizationState.A, CharacterizationTrigger.Internal, Action = nameof(NoOp))]
    private void Configure() { }

    private bool CanGo() => true;
    private bool CannotGo() => false;
    private void NoOp() { }
}

[StateMachine(typeof(CharacterizationState), typeof(CharacterizationTrigger), GenerateExtensibleVersion = true)]
[PayloadType(CharacterizationTrigger.Payload, typeof(CharacterizationPayload))]
[PayloadType(CharacterizationTrigger.AlternatePayload, typeof(AlternateCharacterizationPayload))]
public partial class CharacterizationPayloadMachine
{
    [Transition(CharacterizationState.A, CharacterizationTrigger.Payload, CharacterizationState.B)]
    [Transition(CharacterizationState.B, CharacterizationTrigger.AlternatePayload, CharacterizationState.A)]
    private void Configure() { }
}

[StateMachine(
    typeof(CharacterizationState),
    typeof(CharacterizationTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class CharacterizationHsmMachine
{
    [State(CharacterizationState.Parent, History = HistoryMode.Shallow)]
    [State(CharacterizationState.Child1, Parent = CharacterizationState.Parent, IsInitial = true)]
    [State(CharacterizationState.Child2, Parent = CharacterizationState.Parent)]
    private void ConfigureStates() { }

    [Transition(CharacterizationState.A, CharacterizationTrigger.EnterComposite, CharacterizationState.Parent)]
    [Transition(CharacterizationState.Parent, CharacterizationTrigger.Ancestor, CharacterizationState.B)]
    [Transition(CharacterizationState.Child1, CharacterizationTrigger.MoveChild, CharacterizationState.Child2)]
    [Transition(CharacterizationState.B, CharacterizationTrigger.Return, CharacterizationState.Parent)]
    [InternalTransition(CharacterizationState.Parent, CharacterizationTrigger.Internal, Action = nameof(NoOp))]
    private void ConfigureTransitions() { }

    private void NoOp() { }
}

public sealed record CharacterizationEvent(
    string Name,
    string InstanceId,
    DateTime Timestamp,
    object From,
    object To,
    object Trigger,
    object? Payload);

public sealed class CharacterizationExtension : IStateMachineExtension
{
    public List<CharacterizationEvent> Events { get; } = [];

    public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
        => Add("Before", context);

    public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        => Add($"After:{success}", context);

    public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext
        => Add($"GuardEvaluating:{guardName}", context);

    public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext
        => Add($"GuardEvaluated:{guardName}:{result}", context);

    public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
        => Add("Unhandled", context);

    public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
        => Add("Internal", context);

    public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext
        => Add("Transitioned", context);

    private void Add<TContext>(string name, TContext context) where TContext : IStateMachineContext
    {
        var snapshot = Assert.IsAssignableFrom<IStateSnapshot>(context);
        var payload = context is IStateMachineContext<CharacterizationState, CharacterizationTrigger> typed
            ? typed.Payload
            : null;
        Events.Add(new CharacterizationEvent(
            name,
            context.InstanceId,
            context.Timestamp,
            snapshot.FromState,
            snapshot.ToState,
            snapshot.Trigger,
            payload));
    }
}

public sealed class OrderingExtension(string name, List<string> order, bool throwOnBefore) : IStateMachineExtension
{
    public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext
    {
        order.Add(name);
        if (throwOnBefore)
            throw new InvalidOperationException("characterization");
    }

    public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext { }
    public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
    public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
    public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext { }
    public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
    public void OnTransitioned<TContext>(TContext context) where TContext : IStateMachineContext { }
}