using System;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Extensions;

public sealed class ExtensionContractV2ApiTests
{
    [Fact]
    public void Public_contract_exposes_typed_attempt_transition_and_result_data()
    {
        var instanceId = Guid.NewGuid();
        var attempt = new TransitionAttemptContext<ApiState, ApiTrigger>(
            instanceId,
            attemptId: 42,
            ApiState.A,
            ApiTrigger.Go,
            payload: "payload",
            startTimestamp: 123);
        var transition = new TransitionInfo<ApiState>(ApiState.A, ApiState.B, TransitionKind.External);
        var result = new TransitionResult<ApiState>(
            TransitionOutcome.Succeeded,
            ApiState.B,
            ApiState.B,
            transition);

        Assert.Equal(instanceId, attempt.InstanceId);
        Assert.Equal(42, attempt.AttemptId);
        Assert.Equal(ApiState.A, attempt.SourceState);
        Assert.Equal(ApiTrigger.Go, attempt.Trigger);
        Assert.Equal("payload", attempt.Payload);
        Assert.Equal(123, attempt.StartTimestamp);

        Assert.Equal(ApiState.A, transition.HandledAtState);
        Assert.Equal(ApiState.B, transition.DeclaredTarget);
        Assert.Equal(TransitionKind.External, transition.Kind);

        Assert.Equal(TransitionOutcome.Succeeded, result.Outcome);
        Assert.Equal(ApiState.B, result.FinalState);
        Assert.Equal(ApiState.B, result.ResolvedTarget);
        Assert.Equal(transition, result.MatchedTransition);
        Assert.Null(result.Stage);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Typed_extension_can_rely_on_default_hook_implementations()
    {
        IStateMachineExtension<ApiState, ApiTrigger> extension = new EmptyApiExtension();

        Assert.Equal(ExtensionHooks.Transitions, extension.Hooks);
        Assert.Equal(
            ExtensionHooks.Transitions |
            ExtensionHooks.Guards |
            ExtensionHooks.States |
            ExtensionHooks.Callbacks |
            ExtensionHooks.Hierarchy |
            ExtensionHooks.Lifecycle,
            ExtensionHooks.All);
    }

    [Fact]
    public void V1_untyped_contract_types_are_removed()
    {
        var assembly = typeof(IStateMachineExtension<ApiState, ApiTrigger>).Assembly;

        Assert.Null(assembly.GetType("FastFsm.Contracts.IStateMachineExtension"));
        Assert.Null(assembly.GetType("FastFsm.Contracts.IStateMachineContext"));
        Assert.Null(assembly.GetType("FastFsm.Contracts.IStateSnapshot"));
        Assert.Null(assembly.GetType("FastFsm.Contracts.IExtensibleStateMachine"));
    }

    private sealed class EmptyApiExtension : IStateMachineExtension<ApiState, ApiTrigger>;

    private enum ApiState { A, B }
    private enum ApiTrigger { Go }
}