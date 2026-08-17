using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Shouldly;
using Xunit;

namespace Tests.Async.Features.Extensions;

[StateMachine(typeof(TState), typeof(TTrigger), GenerateExtensibleVersion = true)]
public partial class AsyncTransitionedMachine
{
    private async ValueTask<bool> GuardTrueAsync() { await Task.Yield(); return true; }

    [Transition(TState.A, TTrigger.Next, TState.B, Guard = nameof(GuardTrueAsync))]
    private void Configure() { }
}

public enum TState { A, B }
public enum TTrigger { Next }

public sealed class AsyncTransitionedRecorder : IStateMachineExtension
{
    public readonly List<string> Log = new();
    public int TransitionedCount { get; private set; }
    public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext => Log.Add("Before");
    public void OnAfterTransition<T>(T ctx, bool s) where T : IStateMachineContext => Log.Add($"After:{(s ? "Success" : "Fail")}");
    public void OnGuardEvaluation<T>(T ctx, string _) where T : IStateMachineContext => Log.Add("GuardEval");
    public void OnGuardEvaluated<T>(T ctx, string _, bool res) where T : IStateMachineContext => Log.Add("GuardEvaluated");
    public void OnUnhandledTrigger<T>(T ctx) where T : IStateMachineContext { }
    public void OnInternalTransition<T>(T ctx) where T : IStateMachineContext { }
    public void OnTransitioned<T>(T ctx) where T : IStateMachineContext { TransitionedCount++; Log.Add("Transitioned"); }
}

public class AsyncTransitionedHookTests
{
    [Fact]
    public async Task Async_Transitioned_FiresOnce_InOrder()
    {
        var ext = new AsyncTransitionedRecorder();
        var m = new AsyncTransitionedMachine(TState.A, new IStateMachineExtension[] { ext });
        await m.StartAsync();

        var ok = await m.TryFireAsync(TTrigger.Next);
        ok.ShouldBeTrue();
        m.CurrentState.ShouldBe(TState.B);

        ext.TransitionedCount.ShouldBe(1);
        ext.Log.Count.ShouldBeGreaterThanOrEqualTo(5);
        ext.Log[0].ShouldBe("Before");
        ext.Log[1].ShouldBe("GuardEval");
        ext.Log[2].ShouldBe("GuardEvaluated");
        ext.Log[^2].ShouldBe("Transitioned");
        ext.Log[^1].ShouldBe("After:Success");
    }
}
