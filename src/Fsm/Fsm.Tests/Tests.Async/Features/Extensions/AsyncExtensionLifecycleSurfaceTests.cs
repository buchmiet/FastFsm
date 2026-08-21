using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using FastFsm.Exceptions;
using Xunit;

namespace Tests.Async.Features.Extensions;

public enum AsyncLifecycleSurfaceState { A, B }
public enum AsyncLifecycleSurfaceTrigger { Go }

[StateMachine(
    typeof(AsyncLifecycleSurfaceState),
    typeof(AsyncLifecycleSurfaceTrigger),
    GenerateExtensibleVersion = true)]
public partial class AsyncLifecycleSurfaceMachine
{
    [State(AsyncLifecycleSurfaceState.A, OnExit = nameof(ExitAAsync))]
    [State(AsyncLifecycleSurfaceState.B, OnEntry = nameof(EnterBAsync))]
    private void ConfigureStates() { }

    [Transition(
        AsyncLifecycleSurfaceState.A,
        AsyncLifecycleSurfaceTrigger.Go,
        AsyncLifecycleSurfaceState.B,
        Action = nameof(ActAsync))]
    private void ConfigureTransitions() { }

    private async ValueTask ExitAAsync() => await Task.Yield();
    private async ValueTask EnterBAsync() => await Task.Yield();
    private async ValueTask ActAsync() => await Task.Yield();
}

public sealed class AsyncExtensionLifecycleSurfaceTests
{
    [Fact]
    public async Task Async_lifecycle_hooks_match_sync_order()
    {
        var extension = new AsyncLifecycleSurfaceExtension();
        var machine = new AsyncLifecycleSurfaceMachine(AsyncLifecycleSurfaceState.A, [extension]);

        await machine.StartAsync();
        Assert.True(await machine.TryFireAsync(AsyncLifecycleSurfaceTrigger.Go));

        Assert.Equal(
            [
                "started:A",
                "attempt",
                "matched",
                "exiting:A",
                "callback:OnExit:ExitAAsync",
                "entered:B",
                "callback:OnEntry:EnterBAsync",
                "callback:Action:ActAsync",
                "completed:Succeeded"
            ],
            extension.Events);
    }
}

public sealed class AsyncLifecycleSurfaceExtension
    : IStateMachineExtension<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.All;
    public List<string> Events { get; } = [];

    public void OnMachineStarted(Guid instanceId, AsyncLifecycleSurfaceState initialState)
        => Events.Add($"started:{initialState}");

    public void OnAttemptStarting(
        in TransitionAttemptContext<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger> attempt)
        => Events.Add("attempt");

    public void OnTransitionMatched(
        in TransitionAttemptContext<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger> attempt,
        in TransitionInfo<AsyncLifecycleSurfaceState> matched)
        => Events.Add("matched");

    public void OnStateExiting(
        in TransitionAttemptContext<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger> attempt,
        AsyncLifecycleSurfaceState state)
        => Events.Add($"exiting:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger> attempt,
        AsyncLifecycleSurfaceState state)
        => Events.Add($"entered:{state}");

    public void OnCallbackExecuting(
        in TransitionAttemptContext<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger> attempt,
        TransitionStage stage,
        string callbackName)
        => Events.Add($"callback:{stage}:{callbackName}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<AsyncLifecycleSurfaceState, AsyncLifecycleSurfaceTrigger> attempt,
        in TransitionResult<AsyncLifecycleSurfaceState> result)
        => Events.Add($"completed:{result.Outcome}");
}