using System;
using System.Collections.Generic;
using System.Linq;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Extensions;

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

public sealed class ExtensionContractV2RuntimeTests
{
    [Fact]
    public void Attempt_has_stable_machine_identity_monotonic_id_and_typed_outcomes()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.Equal(machine.InstanceId, extension.StartedInstanceId);
        Assert.True(machine.TryFire(CharacterizationTrigger.Reject) is false);
        Assert.True(machine.TryFire(CharacterizationTrigger.Missing) is false);

        Assert.Equal([1L, 2L], extension.Attempts.Select(a => a.AttemptId));
        Assert.All(extension.Attempts, a => Assert.Equal(machine.InstanceId, a.InstanceId));
        Assert.Equal([TransitionOutcome.GuardRejected, TransitionOutcome.UnhandledTrigger], extension.Results.Select(r => r.Outcome));
        Assert.Equal(CharacterizationState.A, extension.Results[0].MatchedTransition?.HandledAtState);
        Assert.Equal(CharacterizationState.B, extension.Results[0].MatchedTransition?.DeclaredTarget);
        Assert.Null(extension.Results[1].MatchedTransition);
    }

    [Fact]
    public void Internal_and_external_transitions_are_distinct()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationFlatMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(CharacterizationTrigger.Internal));
        Assert.Equal(TransitionKind.Internal, extension.Results.Single().MatchedTransition?.Kind);
        Assert.Null(extension.Results.Single().ResolvedTarget);
    }

    [Fact]
    public void Hsm_result_contains_declared_composite_and_effective_leaf()
    {
        var extension = new CharacterizationExtension();
        var machine = new CharacterizationHsmMachine(CharacterizationState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(CharacterizationTrigger.EnterComposite));

        var result = Assert.Single(extension.Results);
        Assert.Equal(CharacterizationState.Parent, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(CharacterizationState.Child1, result.ResolvedTarget);
        Assert.Equal(CharacterizationState.Child1, result.FinalState);
    }

    [Fact]
    public void Extension_mutation_becomes_visible_only_to_the_next_attempt()
    {
        var late = new CharacterizationExtension();
        CharacterizationFlatMachine? machine = null;
        var mutating = new CharacterizationExtension
        {
            AttemptStarting = () => machine!.AddExtension(late)
        };
        machine = new CharacterizationFlatMachine(CharacterizationState.A, [mutating]);
        machine.Start();

        Assert.False(machine.Extensions is IStateMachineExtension<CharacterizationState, CharacterizationTrigger>[]);

        Assert.True(machine.TryFire(CharacterizationTrigger.Go));
        Assert.Empty(late.Attempts);
        Assert.Empty(late.Results);

        Assert.False(machine.TryFire(CharacterizationTrigger.Missing));
        Assert.Single(late.Attempts);
        Assert.Single(late.Results);
    }
}

public sealed class CharacterizationExtension : IStateMachineExtension<CharacterizationState, CharacterizationTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.All;
    public Guid StartedInstanceId { get; private set; }
    public Action? AttemptStarting { get; init; }
    public List<TransitionAttemptContext<CharacterizationState, CharacterizationTrigger>> Attempts { get; } = [];
    public List<TransitionResult<CharacterizationState>> Results { get; } = [];

    public void OnMachineStarted(Guid instanceId, CharacterizationState initialState)
        => StartedInstanceId = instanceId;

    public void OnAttemptStarting(in TransitionAttemptContext<CharacterizationState, CharacterizationTrigger> attempt)
    {
        Attempts.Add(attempt);
        AttemptStarting?.Invoke();
    }

    public void OnAttemptCompleted(
        in TransitionAttemptContext<CharacterizationState, CharacterizationTrigger> attempt,
        in TransitionResult<CharacterizationState> result)
        => Results.Add(result);
}