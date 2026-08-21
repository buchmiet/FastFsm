using System;
using System.Collections.Generic;
using System.Diagnostics;
using Abstractions.Attributes;
using FastFsm.Contracts;
using FastFsm.Exceptions;
using Xunit;

namespace Tests.Fsm.Extensions;

public enum LifecycleSurfaceState { A, B, C, Parent, Child }
public enum LifecycleSurfaceTrigger { Go, Next, EnterParent }

[StateMachine(typeof(LifecycleSurfaceState), typeof(LifecycleSurfaceTrigger), GenerateExtensibleVersion = true)]
public partial class LifecycleSurfaceMachine
{
    [State(LifecycleSurfaceState.A, OnExit = nameof(ExitA))]
    [State(LifecycleSurfaceState.B, OnEntry = nameof(EnterB))]
    private void ConfigureStates() { }

    [Transition(LifecycleSurfaceState.A, LifecycleSurfaceTrigger.Go, LifecycleSurfaceState.B, Action = nameof(Act))]
    [Transition(LifecycleSurfaceState.B, LifecycleSurfaceTrigger.Next, LifecycleSurfaceState.C)]
    private void ConfigureTransitions() { }

    private void ExitA() { }
    private void EnterB() { }
    private void Act() { }
}

[StateMachine(
    typeof(LifecycleSurfaceState),
    typeof(LifecycleSurfaceTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class LifecycleSurfaceHsm
{
    [State(LifecycleSurfaceState.Parent)]
    [State(LifecycleSurfaceState.Child, Parent = LifecycleSurfaceState.Parent, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(LifecycleSurfaceState.A, LifecycleSurfaceTrigger.EnterParent, LifecycleSurfaceState.Parent)]
    private void ConfigureTransitions() { }
}

public sealed class ExtensionLifecycleSurfaceTests
{
    [Fact]
    public void Lifecycle_hooks_have_semantic_order_and_include_states_without_callbacks()
    {
        var extension = new LifecycleSurfaceExtension();
        var machine = new LifecycleSurfaceMachine(LifecycleSurfaceState.A, [extension]);

        machine.Start();
        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.Go));

        Assert.Equal(
            [
                "started:A",
                "attempt",
                "matched",
                "exiting:A",
                "callback:OnExit:ExitA",
                "entered:B",
                "callback:OnEntry:EnterB",
                "callback:Action:Act",
                "completed:Succeeded"
            ],
            extension.Events);

        extension.Events.Clear();
        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.Next));
        Assert.Equal(
            ["attempt", "matched", "exiting:B", "entered:C", "completed:Succeeded"],
            extension.Events);
    }

    [Fact]
    public void Hsm_state_hooks_cover_the_full_exit_and_entry_paths()
    {
        var extension = new LifecycleSurfaceExtension(ExtensionHooks.States);
        var machine = new LifecycleSurfaceHsm(LifecycleSurfaceState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.EnterParent));

        Assert.Equal(["exiting:A", "entered:Parent", "entered:Child"], extension.Events);
    }

    [Fact]
    public void Transitions_only_extension_skips_hsm_state_hooks()
    {
        var extension = new LifecycleSurfaceExtension(ExtensionHooks.Transitions);
        var machine = new LifecycleSurfaceHsm(LifecycleSurfaceState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.EnterParent));

        Assert.Equal(["attempt", "matched", "completed:Succeeded"], extension.Events);
    }

    [Fact]
    public void Hook_mask_is_authoritative_for_each_extension()
    {
        var declared = new LifecycleSurfaceExtension(ExtensionHooks.Transitions | ExtensionHooks.Lifecycle);
        var undeclared = new UndeclaredLifecycleSurfaceExtension();
        var machine = new LifecycleSurfaceMachine(LifecycleSurfaceState.A, [declared, undeclared]);

        machine.Start();
        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.Go));

        Assert.False(undeclared.MachineStartedCalled);
        Assert.False(undeclared.AttemptStartingCalled);
    }

    [Fact]
    public void Attempt_timestamp_uses_the_monotonic_stopwatch_clock()
    {
        var extension = new LifecycleSurfaceExtension(ExtensionHooks.Transitions);
        var machine = new LifecycleSurfaceMachine(LifecycleSurfaceState.A, [extension]);
        machine.Start();

        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.Go));
        var first = extension.StartTimestamps[0];
        Assert.True(machine.TryFire(LifecycleSurfaceTrigger.Next));
        var second = extension.StartTimestamps[1];

        Assert.True(first > 0);
        Assert.True(second >= first);
        Assert.True(Stopwatch.GetElapsedTime(first) >= TimeSpan.Zero);
    }
}

public sealed class LifecycleSurfaceExtension(ExtensionHooks hooks = ExtensionHooks.All)
    : IStateMachineExtension<LifecycleSurfaceState, LifecycleSurfaceTrigger>
{
    public ExtensionHooks Hooks => hooks;
    public List<string> Events { get; } = [];
    public List<long> StartTimestamps { get; } = [];

    public void OnMachineStarted(Guid instanceId, LifecycleSurfaceState initialState)
        => Events.Add($"started:{initialState}");

    public void OnAttemptStarting(in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt)
    {
        Events.Add("attempt");
        StartTimestamps.Add(attempt.StartTimestamp);
    }

    public void OnTransitionMatched(
        in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt,
        in TransitionInfo<LifecycleSurfaceState> matched)
        => Events.Add("matched");

    public void OnStateExiting(
        in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt,
        LifecycleSurfaceState state)
        => Events.Add($"exiting:{state}");

    public void OnStateEntered(
        in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt,
        LifecycleSurfaceState state)
        => Events.Add($"entered:{state}");

    public void OnCallbackExecuting(
        in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt,
        TransitionStage stage,
        string callbackName)
        => Events.Add($"callback:{stage}:{callbackName}");

    public void OnAttemptCompleted(
        in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt,
        in TransitionResult<LifecycleSurfaceState> result)
        => Events.Add($"completed:{result.Outcome}");
}

public sealed class UndeclaredLifecycleSurfaceExtension
    : IStateMachineExtension<LifecycleSurfaceState, LifecycleSurfaceTrigger>
{
    public ExtensionHooks Hooks => ExtensionHooks.None;
    public bool MachineStartedCalled { get; private set; }
    public bool AttemptStartingCalled { get; private set; }

    public void OnMachineStarted(Guid instanceId, LifecycleSurfaceState initialState)
        => MachineStartedCalled = true;

    public void OnAttemptStarting(in TransitionAttemptContext<LifecycleSurfaceState, LifecycleSurfaceTrigger> attempt)
        => AttemptStartingCalled = true;
}