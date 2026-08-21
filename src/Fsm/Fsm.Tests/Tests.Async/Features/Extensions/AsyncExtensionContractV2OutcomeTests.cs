using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Contracts;
using FastFsm.Exceptions;
using Xunit;

namespace Tests.Async.Features.Extensions;

public sealed class AsyncExtensionContractV2OutcomeTests
{
    [Theory]
    [InlineData(TransitionStage.Guard, AsyncOutcomeState.A)]
    [InlineData(TransitionStage.OnExit, AsyncOutcomeState.A)]
    [InlineData(TransitionStage.OnEntry, AsyncOutcomeState.B)]
    [InlineData(TransitionStage.Action, AsyncOutcomeState.B)]
    public async Task Callback_exception_reports_faulted_result_and_actual_final_state(
        TransitionStage stage,
        AsyncOutcomeState finalState)
    {
        var extension = new AsyncOutcomeExtension();
        var machine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension]) { FailureStage = stage };
        await machine.StartAsync();

        AsyncOutcomeTestException? propagatedException = null;
        if (stage is TransitionStage.Guard or TransitionStage.OnExit)
        {
            Assert.False(await machine.TryFireAsync(AsyncOutcomeTrigger.Go));
        }
        else
        {
            propagatedException = await Assert.ThrowsAsync<AsyncOutcomeTestException>(
                async () => await machine.FireAsync(AsyncOutcomeTrigger.Go));
        }

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(stage, result.Stage);
        if (propagatedException is not null)
            Assert.Same(propagatedException, result.Exception);
        else
            Assert.IsType<AsyncOutcomeTestException>(result.Exception);
        Assert.Equal(finalState, result.FinalState);
        Assert.Equal(finalState, machine.CurrentState);
        Assert.NotNull(result.MatchedTransition);
    }

    [Fact]
    public async Task False_guard_reports_rejection_without_an_exception()
    {
        var extension = new AsyncOutcomeExtension();
        var machine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension]) { RejectGuard = true };
        await machine.StartAsync();

        Assert.False(await machine.TryFireAsync(AsyncOutcomeTrigger.Go));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.GuardRejected, result.Outcome);
        Assert.Null(result.Stage);
        Assert.Null(result.Exception);
        Assert.Equal(AsyncOutcomeState.A, result.FinalState);
        Assert.NotNull(result.MatchedTransition);
    }

    [Theory]
    [InlineData(TransitionStage.OnExit, "ThrowFromOnExitAsync")]
    [InlineData(TransitionStage.OnEntry, "ThrowFromOnEntryAsync")]
    [InlineData(TransitionStage.Action, "ThrowFromActionAsync")]
    public async Task Continued_callback_exception_reports_fault_and_successful_attempt(
        TransitionStage stage,
        string callbackName)
    {
        var extension = new AsyncOutcomeExtension();
        var machine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension])
        {
            FailureStage = stage,
            Directive = ExceptionDirective.Continue
        };
        await machine.StartAsync();

        await machine.FireAsync(AsyncOutcomeTrigger.Go);

        var fault = Assert.Single(extension.CallbackFaults);
        Assert.Equal(stage, fault.Stage);
        Assert.Equal(callbackName, fault.CallbackName);
        Assert.IsType<AsyncOutcomeTestException>(fault.Exception);

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Succeeded, result.Outcome);
        Assert.Null(result.Stage);
        Assert.Null(result.Exception);
        Assert.Equal(AsyncOutcomeState.B, result.FinalState);
        Assert.Equal(AsyncOutcomeState.B, machine.CurrentState);
    }

    [Theory]
    [InlineData(TransitionStage.Guard, AsyncOutcomeState.A)]
    [InlineData(TransitionStage.OnExit, AsyncOutcomeState.A)]
    [InlineData(TransitionStage.OnEntry, AsyncOutcomeState.B)]
    [InlineData(TransitionStage.Action, AsyncOutcomeState.B)]
    public async Task OperationCanceledException_reports_canceled_even_when_handler_requests_continue(
        TransitionStage stage,
        AsyncOutcomeState finalState)
    {
        var extension = new AsyncOutcomeExtension();
        var machine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension])
        {
            CancellationStage = stage,
            Directive = ExceptionDirective.Continue
        };
        await machine.StartAsync();

        OperationCanceledException? propagatedException = null;
        if (stage == TransitionStage.Guard)
        {
            Assert.False(await machine.TryFireAsync(AsyncOutcomeTrigger.Go));
        }
        else
        {
            propagatedException = await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await machine.FireAsync(AsyncOutcomeTrigger.Go));
        }

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Canceled, result.Outcome);
        Assert.Equal(stage, result.Stage);
        if (propagatedException is not null)
            Assert.Same(propagatedException, result.Exception);
        else
            Assert.IsType<OperationCanceledException>(result.Exception);
        Assert.Equal(finalState, result.FinalState);
        Assert.Equal(finalState, machine.CurrentState);
        Assert.Empty(extension.CallbackFaults);
    }

    [Theory]
    [InlineData(TransitionStage.Guard)]
    [InlineData(TransitionStage.OnExit)]
    public async Task Plain_and_extensible_variants_return_false_for_the_same_async_failure(TransitionStage stage)
    {
        var plainMachine = new AsyncPlainOutcomeMachine(AsyncOutcomeState.A) { FailureStage = stage };
        await plainMachine.StartAsync();

        Assert.False(await plainMachine.TryFireAsync(AsyncOutcomeTrigger.Go));

        var extension = new AsyncOutcomeExtension();
        var extensibleMachine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension])
        {
            FailureStage = stage
        };
        await extensibleMachine.StartAsync();

        Assert.False(await extensibleMachine.TryFireAsync(AsyncOutcomeTrigger.Go));

        Assert.Equal(plainMachine.CurrentState, extensibleMachine.CurrentState);
        Assert.Equal(TransitionOutcome.Faulted, Assert.Single(extension.Results).Outcome);
    }

    [Theory]
    [InlineData(TransitionStage.Guard)]
    [InlineData(TransitionStage.OnExit)]
    public async Task Plain_and_extensible_variants_have_the_same_async_cancellation_semantics(
        TransitionStage stage)
    {
        var plainMachine = new AsyncPlainOutcomeMachine(AsyncOutcomeState.A) { CancellationStage = stage };
        await plainMachine.StartAsync();

        OperationCanceledException? plainException = null;
        if (stage == TransitionStage.Guard)
        {
            Assert.False(await plainMachine.TryFireAsync(AsyncOutcomeTrigger.Go));
        }
        else
        {
            plainException = await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await plainMachine.TryFireAsync(AsyncOutcomeTrigger.Go));
        }

        var extension = new AsyncOutcomeExtension();
        var extensibleMachine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension])
        {
            CancellationStage = stage,
            Directive = ExceptionDirective.Continue
        };
        await extensibleMachine.StartAsync();

        OperationCanceledException? extensibleException = null;
        if (stage == TransitionStage.Guard)
        {
            Assert.False(await extensibleMachine.TryFireAsync(AsyncOutcomeTrigger.Go));
        }
        else
        {
            extensibleException = await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await extensibleMachine.TryFireAsync(AsyncOutcomeTrigger.Go));
        }

        Assert.Equal(plainMachine.CurrentState, extensibleMachine.CurrentState);
        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Canceled, result.Outcome);
        Assert.Equal(stage, result.Stage);
        if (extensibleException is not null)
        {
            Assert.NotNull(plainException);
            Assert.Same(extensibleException, result.Exception);
        }
        else
        {
            Assert.Null(plainException);
            Assert.IsType<OperationCanceledException>(result.Exception);
        }
        Assert.Empty(extension.CallbackFaults);
    }

    [Fact]
    public async Task Plain_and_extensible_payload_variants_return_false_on_exit_failure()
    {
        var payload = new AsyncOutcomePayload(42);
        var plainMachine = new AsyncPlainPayloadOutcomeMachine(AsyncOutcomeState.A);
        await plainMachine.StartAsync();

        Assert.False(await plainMachine.TryFireAsync(AsyncOutcomeTrigger.Go, payload));

        var extension = new AsyncOutcomeExtension();
        var extensibleMachine = new AsyncExtensiblePayloadOutcomeMachine(
            AsyncOutcomeState.A,
            [extension]);
        await extensibleMachine.StartAsync();

        Assert.False(await extensibleMachine.TryFireAsync(AsyncOutcomeTrigger.Go, payload));

        Assert.Equal(plainMachine.CurrentState, extensibleMachine.CurrentState);
        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(TransitionStage.OnExit, result.Stage);
        Assert.IsType<AsyncOutcomeTestException>(result.Exception);
    }

    [Theory]
    [InlineData(AsyncOutcomeTrigger.Internal, TransitionKind.Internal, null)]
    [InlineData(AsyncOutcomeTrigger.Self, TransitionKind.External, AsyncOutcomeState.A)]
    public async Task Internal_and_self_transition_action_failures_report_their_actual_kind_and_target(
        AsyncOutcomeTrigger trigger,
        TransitionKind kind,
        AsyncOutcomeState? resolvedTarget)
    {
        var extension = new AsyncOutcomeExtension();
        var machine = new AsyncOutcomeMachine(AsyncOutcomeState.A, [extension])
        {
            FailureStage = TransitionStage.Action
        };
        await machine.StartAsync();

        var exception = await Assert.ThrowsAsync<AsyncOutcomeTestException>(
            async () => await machine.FireAsync(trigger));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(TransitionStage.Action, result.Stage);
        Assert.Same(exception, result.Exception);
        Assert.Equal(kind, result.MatchedTransition?.Kind);
        Assert.Equal(resolvedTarget, result.ResolvedTarget);
        Assert.Equal(AsyncOutcomeState.A, result.FinalState);
        Assert.Equal(AsyncOutcomeState.A, machine.CurrentState);
    }

    [Fact]
    public async Task Hsm_failure_reports_resolved_leaf_as_final_state()
    {
        var extension = new AsyncOutcomeExtension();
        var machine = new AsyncOutcomeHsmMachine(AsyncOutcomeState.A, [extension]);
        await machine.StartAsync();

        var exception = await Assert.ThrowsAsync<AsyncOutcomeTestException>(
            async () => await machine.FireAsync(AsyncOutcomeTrigger.Go));

        var result = Assert.Single(extension.Results);
        Assert.Equal(TransitionOutcome.Faulted, result.Outcome);
        Assert.Equal(TransitionStage.Action, result.Stage);
        Assert.Same(exception, result.Exception);
        Assert.Equal(AsyncOutcomeState.Parent, result.MatchedTransition?.DeclaredTarget);
        Assert.Equal(AsyncOutcomeState.Child, result.ResolvedTarget);
        Assert.Equal(AsyncOutcomeState.Child, result.FinalState);
        Assert.Equal(AsyncOutcomeState.Child, machine.CurrentState);
    }
}

public enum AsyncOutcomeState { A, B, Parent, Child }
public enum AsyncOutcomeTrigger { Go, Internal, Self }

public sealed class AsyncOutcomeTestException : Exception;
public sealed record AsyncOutcomePayload(int Value);

[StateMachine(
    typeof(AsyncOutcomeState),
    typeof(AsyncOutcomeTrigger),
    GenerateExtensibleVersion = true,
    ContinueOnCapturedContext = false)]
public partial class AsyncOutcomeMachine
{
    public TransitionStage? FailureStage { get; init; }
    public TransitionStage? CancellationStage { get; init; }
    public ExceptionDirective Directive { get; init; } = ExceptionDirective.Propagate;
    public bool RejectGuard { get; init; }

    private void Configure() => FSM
        .OnException<AsyncOutcomeState>(nameof(HandleExceptionAsync))
        .State(AsyncOutcomeState.A)
            .OnExitAsync(nameof(ThrowFromOnExitAsync))
            .On(AsyncOutcomeTrigger.Go)
                .Guard(nameof(ThrowFromGuardAsync))
                .Action(nameof(ThrowFromActionAsync))
                .GoTo(AsyncOutcomeState.B)
            .OnInternal(AsyncOutcomeTrigger.Internal)
                .Action(nameof(ThrowFromActionAsync))
                .Internal()
            .On(AsyncOutcomeTrigger.Self)
                .Action(nameof(ThrowFromActionAsync))
                .GoTo(AsyncOutcomeState.A)
        .State(AsyncOutcomeState.B)
            .OnEntryAsync(nameof(ThrowFromOnEntryAsync));

    private async ValueTask<bool> ThrowFromGuardAsync()
    {
        await Task.Yield();
        ThrowIfRequested(TransitionStage.Guard);
        return !RejectGuard;
    }

    private async ValueTask ThrowFromOnExitAsync()
    {
        await Task.Yield();
        ThrowIfRequested(TransitionStage.OnExit);
    }

    private async ValueTask ThrowFromOnEntryAsync()
    {
        await Task.Yield();
        ThrowIfRequested(TransitionStage.OnEntry);
    }

    public async ValueTask ThrowFromActionAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        ThrowIfRequested(TransitionStage.Action);
    }

    private void ThrowIfRequested(TransitionStage stage)
    {
        if (CancellationStage == stage)
            throw new OperationCanceledException();

        if (FailureStage == stage)
            throw new AsyncOutcomeTestException();
    }

    private ValueTask<ExceptionDirective> HandleExceptionAsync(
        ExceptionContext<AsyncOutcomeState, AsyncOutcomeTrigger> context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(Directive);
}

[StateMachine(
    typeof(AsyncOutcomeState),
    typeof(AsyncOutcomeTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true,
    ContinueOnCapturedContext = false)]
public partial class AsyncOutcomeHsmMachine
{
    [State(AsyncOutcomeState.Parent)]
    [State(AsyncOutcomeState.Child, Parent = AsyncOutcomeState.Parent, IsInitial = true)]
    [Transition(
        AsyncOutcomeState.A,
        AsyncOutcomeTrigger.Go,
        AsyncOutcomeState.Parent,
        Action = nameof(ThrowFromActionAsync))]
    private void Configure() { }

    public async ValueTask ThrowFromActionAsync()
    {
        await Task.Yield();
        throw new AsyncOutcomeTestException();
    }
}

[StateMachine(
    typeof(AsyncOutcomeState),
    typeof(AsyncOutcomeTrigger),
    ContinueOnCapturedContext = false)]
public partial class AsyncPlainOutcomeMachine
{
    public TransitionStage? FailureStage { get; init; }
    public TransitionStage? CancellationStage { get; init; }

    private void Configure() => FSM
        .State(AsyncOutcomeState.A)
            .OnExitAsync(nameof(ThrowFromOnExitAsync))
            .On(AsyncOutcomeTrigger.Go)
                .Guard(nameof(ThrowFromGuardAsync))
                .GoTo(AsyncOutcomeState.B)
        .State(AsyncOutcomeState.B);

    private async ValueTask<bool> ThrowFromGuardAsync()
    {
        await Task.Yield();
        ThrowIfRequested(TransitionStage.Guard);
        return true;
    }

    private async ValueTask ThrowFromOnExitAsync()
    {
        await Task.Yield();
        ThrowIfRequested(TransitionStage.OnExit);
    }

    private void ThrowIfRequested(TransitionStage stage)
    {
        if (CancellationStage == stage)
            throw new OperationCanceledException();

        if (FailureStage == stage)
            throw new AsyncOutcomeTestException();
    }
}

[StateMachine(
    typeof(AsyncOutcomeState),
    typeof(AsyncOutcomeTrigger),
    ContinueOnCapturedContext = false)]
[PayloadType(typeof(AsyncOutcomePayload))]
public partial class AsyncPlainPayloadOutcomeMachine
{
    [State(AsyncOutcomeState.A, OnExit = nameof(ThrowFromOnExitAsync))]
    [Transition(AsyncOutcomeState.A, AsyncOutcomeTrigger.Go, AsyncOutcomeState.B)]
    private void Configure() { }

    private async ValueTask ThrowFromOnExitAsync()
    {
        await Task.Yield();
        throw new AsyncOutcomeTestException();
    }
}

[StateMachine(
    typeof(AsyncOutcomeState),
    typeof(AsyncOutcomeTrigger),
    GenerateExtensibleVersion = true,
    ContinueOnCapturedContext = false)]
[PayloadType(typeof(AsyncOutcomePayload))]
public partial class AsyncExtensiblePayloadOutcomeMachine
{
    [State(AsyncOutcomeState.A, OnExit = nameof(ThrowFromOnExitAsync))]
    [Transition(AsyncOutcomeState.A, AsyncOutcomeTrigger.Go, AsyncOutcomeState.B)]
    private void Configure() { }

    private async ValueTask ThrowFromOnExitAsync()
    {
        await Task.Yield();
        throw new AsyncOutcomeTestException();
    }
}

public sealed class AsyncOutcomeExtension : IStateMachineExtension<AsyncOutcomeState, AsyncOutcomeTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Callbacks;
    public List<TransitionResult<AsyncOutcomeState>> Results { get; } = [];
    public List<(TransitionStage Stage, string CallbackName, Exception Exception)> CallbackFaults { get; } = [];

    public void OnAttemptCompleted(
        in TransitionAttemptContext<AsyncOutcomeState, AsyncOutcomeTrigger> attempt,
        in TransitionResult<AsyncOutcomeState> result)
        => Results.Add(result);

    public void OnCallbackFaulted(
        in TransitionAttemptContext<AsyncOutcomeState, AsyncOutcomeTrigger> attempt,
        TransitionStage stage,
        string callbackName,
        Exception exception)
        => CallbackFaults.Add((stage, callbackName, exception));
}