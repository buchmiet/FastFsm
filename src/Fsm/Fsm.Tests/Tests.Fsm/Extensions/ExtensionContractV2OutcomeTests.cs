using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Contracts;
using FastFsm.Exceptions;
using Xunit;

namespace Tests.Fsm.Extensions;

public sealed class ExtensionContractV2OutcomeTests
{
    [Theory]
    [InlineData(TransitionStage.Guard, OutcomeState.A)]
    [InlineData(TransitionStage.OnExit, OutcomeState.A)]
    [InlineData(TransitionStage.OnEntry, OutcomeState.B)]
    [InlineData(TransitionStage.Action, OutcomeState.B)]
    public void Propagated_exception_reports_faulted_result_and_actual_final_state(
        TransitionStage stage,
        OutcomeState finalState)
    {
        var extension = new OutcomeExtension();
        var machine = new OutcomeMachine(OutcomeState.A, [extension]) { FailureStage = stage };
        machine.Start();

        var exception = Assert.Throws<OutcomeTestException>(() => machine.Fire(OutcomeTrigger.Go));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(stage, result.Stage);
        Assert.Same(exception, result.Exception);
        Assert.Equal(finalState, result.FinalState);
        Assert.Equal(finalState, machine.CurrentState);
        Assert.NotNull(result.MatchedTransition);
    }

    [Fact]
    public void False_guard_reports_rejection_without_an_exception()
    {
        var extension = new OutcomeExtension();
        var machine = new OutcomeMachine(OutcomeState.A, [extension]) { RejectGuard = true };
        machine.Start();

        Assert.False(machine.TryFire(OutcomeTrigger.Go));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.GuardRejected, result.Outcome);
        Assert.Null(result.Stage);
        Assert.Null(result.Exception);
        Assert.Equal(OutcomeState.A, result.FinalState);
        Assert.NotNull(result.MatchedTransition);
    }

    [Theory]
    [InlineData(TransitionStage.OnExit, "ThrowFromOnExit")]
    [InlineData(TransitionStage.OnEntry, "ThrowFromOnEntry")]
    [InlineData(TransitionStage.Action, "ThrowFromAction")]
    public void Continued_callback_exception_reports_fault_and_successful_attempt(
        TransitionStage stage,
        string callbackName)
    {
        var extension = new OutcomeExtension();
        var machine = new OutcomeMachine(OutcomeState.A, [extension])
        {
            FailureStage = stage,
            Directive = ExceptionDirective.Continue
        };
        machine.Start();

        machine.Fire(OutcomeTrigger.Go);

        var fault = Assert.Single(extension.CallbackFaults);
        Assert.Equal(stage, fault.Stage);
        Assert.Equal(callbackName, fault.CallbackName);
        Assert.IsType<OutcomeTestException>(fault.Exception);

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Succeeded, result.Outcome);
        Assert.Null(result.Stage);
        Assert.Null(result.Exception);
        Assert.Equal(OutcomeState.B, result.FinalState);
        Assert.Equal(OutcomeState.B, machine.CurrentState);
    }

    [Theory]
    [InlineData(TransitionStage.Guard, OutcomeState.A)]
    [InlineData(TransitionStage.OnExit, OutcomeState.A)]
    [InlineData(TransitionStage.OnEntry, OutcomeState.B)]
    [InlineData(TransitionStage.Action, OutcomeState.B)]
    public void OperationCanceledException_reports_canceled_even_when_handler_requests_continue(
        TransitionStage stage,
        OutcomeState finalState)
    {
        var extension = new OutcomeExtension();
        var machine = new OutcomeMachine(OutcomeState.A, [extension])
        {
            CancellationStage = stage,
            Directive = ExceptionDirective.Continue
        };
        machine.Start();

        var exception = Assert.Throws<OperationCanceledException>(() => machine.Fire(OutcomeTrigger.Go));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Canceled, result.Outcome);
        Assert.Equal(stage, result.Stage);
        Assert.Same(exception, result.Exception);
        Assert.Equal(finalState, result.FinalState);
        Assert.Equal(finalState, machine.CurrentState);
        Assert.Empty(extension.CallbackFaults);
    }

    [Theory]
    [InlineData(OutcomeTrigger.Internal, TransitionKind.Internal, null)]
    [InlineData(OutcomeTrigger.Self, TransitionKind.External, OutcomeState.A)]
    public void Internal_and_self_transition_action_failures_report_their_actual_kind_and_target(
        OutcomeTrigger trigger,
        TransitionKind kind,
        OutcomeState? resolvedTarget)
    {
        var extension = new OutcomeExtension();
        var machine = new OutcomeMachine(OutcomeState.A, [extension])
        {
            FailureStage = TransitionStage.Action
        };
        machine.Start();

        Assert.Throws<OutcomeTestException>(() => machine.Fire(trigger));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(TransitionStage.Action, result.Stage);
        Assert.Equal(kind, result.MatchedTransition?.Kind);
        Assert.Equal(resolvedTarget, result.ResolvedTarget);
        Assert.Equal(OutcomeState.A, result.FinalState);
        Assert.Equal(OutcomeState.A, machine.CurrentState);
    }

    [Fact]
    public void Hsm_failure_reports_resolved_leaf_as_final_state()
    {
        var extension = new OutcomeExtension();
        var machine = new OutcomeHsmMachine(OutcomeState.A, [extension]);
        machine.Start();

        Assert.Throws<OutcomeTestException>(() => machine.Fire(OutcomeTrigger.Go));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(TransitionStage.Action, result.Stage);
        Assert.Equal(OutcomeState.Parent, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(OutcomeState.Child, result.ResolvedTarget);
        Assert.Equal(OutcomeState.Child, result.FinalState);
        Assert.Equal(OutcomeState.Child, machine.CurrentState);
    }

    [Fact]
    public void Plain_and_extensible_payload_variants_propagate_the_same_on_exit_failure()
    {
        var payload = new OutcomePayload(42);
        var plainMachine = new PlainPayloadOutcomeMachine(OutcomeState.A);
        plainMachine.Start();

        Assert.Throws<OutcomeTestException>(() => plainMachine.TryFire(OutcomeTrigger.Go, payload));

        var extension = new OutcomeExtension();
        var extensibleMachine = new ExtensiblePayloadOutcomeMachine(OutcomeState.A, [extension]);
        extensibleMachine.Start();

        var exception = Assert.Throws<OutcomeTestException>(
            () => extensibleMachine.TryFire(OutcomeTrigger.Go, payload));

        Assert.Equal(plainMachine.CurrentState, extensibleMachine.CurrentState);
        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(TransitionStage.OnExit, result.Stage);
        Assert.Same(exception, result.Exception);
    }
}

public enum OutcomeState { A, B, Parent, Child }
public enum OutcomeTrigger { Go, Internal, Self }

public sealed class OutcomeTestException : Exception;
public sealed record OutcomePayload(int Value);

[StateMachine(typeof(OutcomeState), typeof(OutcomeTrigger), GenerateExtensibleVersion = true)]
public partial class OutcomeMachine
{
    public TransitionStage? FailureStage { get; init; }
    public TransitionStage? CancellationStage { get; init; }
    public ExceptionDirective Directive { get; init; } = ExceptionDirective.Propagate;
    public bool RejectGuard { get; init; }

    private void Configure() => FSM
        .OnException<OutcomeState>(nameof(HandleException))
        .State(OutcomeState.A)
            .OnExit(nameof(ThrowFromOnExit))
            .On(OutcomeTrigger.Go)
                .Guard(nameof(ThrowFromGuard))
                .Action(nameof(ThrowFromAction))
                .GoTo(OutcomeState.B)
            .OnInternal(OutcomeTrigger.Internal)
                .Action(nameof(ThrowFromAction))
                .Internal()
            .On(OutcomeTrigger.Self)
                .Action(nameof(ThrowFromAction))
                .GoTo(OutcomeState.A)
        .State(OutcomeState.B)
            .OnEntry(nameof(ThrowFromOnEntry));

    private bool ThrowFromGuard()
    {
        ThrowIfRequested(TransitionStage.Guard);
        return !RejectGuard;
    }

    private void ThrowFromOnExit() => ThrowIfRequested(TransitionStage.OnExit);
    private void ThrowFromOnEntry() => ThrowIfRequested(TransitionStage.OnEntry);

    public void ThrowFromAction()
        => ThrowIfRequested(TransitionStage.Action);

    private void ThrowIfRequested(TransitionStage stage)
    {
        if (CancellationStage == stage)
            throw new OperationCanceledException();

        if (FailureStage == stage)
            throw new OutcomeTestException();
    }

    private ExceptionDirective HandleException(ExceptionContext<OutcomeState, OutcomeTrigger> context)
        => Directive;
}

[StateMachine(
    typeof(OutcomeState),
    typeof(OutcomeTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class OutcomeHsmMachine
{
    [State(OutcomeState.Parent)]
    [State(OutcomeState.Child, Parent = OutcomeState.Parent, IsInitial = true)]
    [Transition(OutcomeState.A, OutcomeTrigger.Go, OutcomeState.Parent, Action = nameof(ThrowFromAction))]
    private void Configure() { }

    public void ThrowFromAction() => throw new OutcomeTestException();
}

[StateMachine(typeof(OutcomeState), typeof(OutcomeTrigger))]
[PayloadType(typeof(OutcomePayload))]
public partial class PlainPayloadOutcomeMachine
{
    [State(OutcomeState.A, OnExit = nameof(ThrowFromOnExit))]
    [Transition(OutcomeState.A, OutcomeTrigger.Go, OutcomeState.B)]
    private void Configure() { }

    private void ThrowFromOnExit() => throw new OutcomeTestException();
}

[StateMachine(typeof(OutcomeState), typeof(OutcomeTrigger), GenerateExtensibleVersion = true)]
[PayloadType(typeof(OutcomePayload))]
public partial class ExtensiblePayloadOutcomeMachine
{
    [State(OutcomeState.A, OnExit = nameof(ThrowFromOnExit))]
    [Transition(OutcomeState.A, OutcomeTrigger.Go, OutcomeState.B)]
    private void Configure() { }

    private void ThrowFromOnExit() => throw new OutcomeTestException();
}

public sealed class OutcomeExtension : IStateMachineExtension<OutcomeState, OutcomeTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Callbacks;
    public List<TransitionResult<OutcomeState>> Results { get; } = [];
    public List<(TransitionStage Stage, string CallbackName, Exception Exception)> CallbackFaults { get; } = [];

    public void OnAttemptCompleted(
        in TransitionAttemptContext<OutcomeState, OutcomeTrigger> attempt,
        in TransitionResult<OutcomeState> result)
        => Results.Add(result);

    public void OnCallbackFaulted(
        in TransitionAttemptContext<OutcomeState, OutcomeTrigger> attempt,
        TransitionStage stage,
        string callbackName,
        Exception exception)
        => CallbackFaults.Add((stage, callbackName, exception));
}