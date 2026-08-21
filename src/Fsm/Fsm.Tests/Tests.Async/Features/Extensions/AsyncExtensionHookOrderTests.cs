using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Shouldly;

using Xunit;
using Dsl;

namespace Tests.Async.Features.Extensions;

// Minimal async machines with extensions enabled
[StateMachine(typeof(AState), typeof(ATrigger), GenerateExtensibleVersion = true)]
public partial class AsyncHookOrderMachineSuccess
{
    private async ValueTask<bool> GuardTrueAsync()
    {
        await Task.Yield();
        return true;
    }

    [Transition(AState.A, ATrigger.Next, AState.B, Guard = nameof(GuardTrueAsync))]
    private void Configure() { }
}

[StateMachine(typeof(AState), typeof(ATrigger), GenerateExtensibleVersion = true)]
public partial class AsyncHookOrderMachineFail
{
    private async ValueTask<bool> GuardFalseAsync()
    {
        await Task.Yield();
        return false;
    }

    [Transition(AState.A, ATrigger.Fail, AState.B, Guard = nameof(GuardFalseAsync))]
    private void Configure() { }
}

public enum AState { A, B }
public enum ATrigger { Next, Fail }

public sealed class AsyncRecordingExtension : IStateMachineExtension<AState, ATrigger>
{
    public readonly List<string> Log = new();
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;
    public void OnAttemptStarting(in TransitionAttemptContext<AState, ATrigger> attempt) => Log.Add("AttemptStarting");
    public void OnTransitionMatched(in TransitionAttemptContext<AState, ATrigger> attempt, in TransitionInfo<AState> matched) => Log.Add("TransitionMatched");
    public void OnAttemptCompleted(in TransitionAttemptContext<AState, ATrigger> attempt, in TransitionResult<AState> result) => Log.Add($"AttemptCompleted:{result.Outcome}");
    public void OnGuardEvaluating(in TransitionAttemptContext<AState, ATrigger> attempt, in TransitionInfo<AState> candidate, string _) => Log.Add("GuardEvaluating");
    public void OnGuardEvaluated(in TransitionAttemptContext<AState, ATrigger> attempt, in TransitionInfo<AState> candidate, string _, bool result) => Log.Add("GuardEvaluated");
}

public class AsyncExtensionHookOrderTests
{
    [Fact]
    public async Task Hooks_AreInvoked_InExpectedOrder_OnSuccess()
    {
        var ext = new AsyncRecordingExtension();
        var m = new AsyncHookOrderMachineSuccess(AState.A, new IStateMachineExtension<AState, ATrigger>[] { ext });
        await m.StartAsync();

        var ok = await m.TryFireAsync(ATrigger.Next);
        ok.ShouldBeTrue();

        ext.Log.ShouldBe(new[]
        {
            "AttemptStarting",
            "TransitionMatched",
            "GuardEvaluating",
            "GuardEvaluated",
            "AttemptCompleted:Succeeded"
        });
    }

    [Fact]
    public async Task Hooks_AreInvoked_InExpectedOrder_OnGuardFail()
    {
        var ext = new AsyncRecordingExtension();
        var m = new AsyncHookOrderMachineFail(AState.A, new IStateMachineExtension<AState, ATrigger>[] { ext });
        await m.StartAsync();

        var ok = await m.TryFireAsync(ATrigger.Fail);
        ok.ShouldBeFalse();

        ext.Log.ShouldBe(new[]
        {
            "AttemptStarting",
            "TransitionMatched",
            "GuardEvaluating",
            "GuardEvaluated",
            "AttemptCompleted:GuardRejected"
        });
    }

    [Fact]
    public async Task GetPermittedTriggersAsync_DoesNot_Emit_Guard_Hooks()
    {
        var ext = new AsyncRecordingExtension();
        var m = new AsyncHookOrderMachineSuccess(AState.A, new IStateMachineExtension<AState, ATrigger>[] { ext });
        await m.StartAsync();

        var permitted = await m.GetPermittedTriggersAsync();
        permitted.ShouldContain(ATrigger.Next);

        // No GuardEval hooks are emitted during GetPermittedTriggersAsync
        ext.Log.ShouldBeEmpty();
    }
}

// Fluent API versions for parity
[StateMachine(typeof(AState), typeof(ATrigger), GenerateExtensibleVersion = true)]
public partial class AsyncHookOrderMachineSuccessFluentFsm
{
    private void Configure() => FSM
        .State(AState.A)
            .On(ATrigger.Next)
                .Guard(nameof(GuardTrueAsync))
                .GoTo(AState.B)
        .State(AState.B);

    private async ValueTask<bool> GuardTrueAsync() { await Task.Yield(); return true; }
}

[StateMachine(typeof(AState), typeof(ATrigger), GenerateExtensibleVersion = true)]
public partial class AsyncHookOrderMachineFailFluentFsm
{
    private void Configure() => FSM
        .State(AState.A)
            .On(ATrigger.Fail)
                .Guard(nameof(GuardFalseAsync))
                .GoTo(AState.B)
        .State(AState.B);

    private async ValueTask<bool> GuardFalseAsync() { await Task.Yield(); return false; }
}
