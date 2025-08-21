using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Shouldly;

using Xunit;

namespace  FastFsm.Async.Tests.Features.Extensions;

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

public sealed class AsyncRecordingExtension : IStateMachineExtension
{
    public readonly List<string> Log = new();
    public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext => Log.Add("Before");
    public void OnAfterTransition<T>(T ctx, bool s) where T : IStateMachineContext => Log.Add($"After:{(s ? "Success" : "Fail")}");
    public void OnGuardEvaluation<T>(T ctx, string _) where T : IStateMachineContext => Log.Add("GuardEval");
    public void OnGuardEvaluated<T>(T ctx, string _, bool res) where T : IStateMachineContext => Log.Add("GuardEvaluated");
}

public class AsyncExtensionHookOrderTests
{
    [Fact]
    public async Task Hooks_AreInvoked_InExpectedOrder_OnSuccess()
    {
        var log = new List<string>();
        var ext = new AsyncRecordingExtension { };
        var m = new AsyncHookOrderMachineSuccess(AState.A, new IStateMachineExtension[] { ext });
        await m.StartAsync();

        var ok = await m.TryFireAsync(ATrigger.Next);
        ok.ShouldBeTrue();

        // Expected order per f08: Before -> GuardEvaluation -> GuardEvaluated -> ... -> After:Success
        // Check prefix order and last element
        ext.Log.Count.ShouldBeGreaterThanOrEqualTo(4);
        ext.Log[0].ShouldBe("Before");
        ext.Log[1].ShouldBe("GuardEval");
        ext.Log[2].ShouldBe("GuardEvaluated");
        ext.Log[^1].ShouldBe("After:Success");
    }

    [Fact]
    public async Task Hooks_AreInvoked_InExpectedOrder_OnGuardFail()
    {
        var ext = new AsyncRecordingExtension();
        var m = new AsyncHookOrderMachineFail(AState.A, new IStateMachineExtension[] { ext });
        await m.StartAsync();

        var ok = await m.TryFireAsync(ATrigger.Fail);
        ok.ShouldBeFalse();

        // On guard fail: Before -> GuardEvaluation -> GuardEvaluated -> After(Fail)
        ext.Log.ShouldBe(new[] { "Before", "GuardEval", "GuardEvaluated", "After:Fail" });
    }

    [Fact]
    public async Task GetPermittedTriggersAsync_DoesNot_Emit_Guard_Hooks()
    {
        var ext = new AsyncRecordingExtension();
        var m = new AsyncHookOrderMachineSuccess(AState.A, new IStateMachineExtension[] { ext });
        await m.StartAsync();

        var permitted = await m.GetPermittedTriggersAsync();
        permitted.ShouldContain(ATrigger.Next);

        // No GuardEval hooks are emitted during GetPermittedTriggersAsync
        ext.Log.ShouldBeEmpty();
    }
}
