using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Shouldly;
using Xunit;

namespace Tests.Async.Features.Extensions;

[StateMachine(typeof(TState), typeof(TTrigger), GenerateExtensibleVersion = true)]
public partial class AsyncTransitionMatchedMachine
{
    private async ValueTask<bool> GuardTrueAsync() { await Task.Yield(); return true; }

    [Transition(TState.A, TTrigger.Next, TState.B, Guard = nameof(GuardTrueAsync))]
    private void Configure() { }
}

public enum TState { A, B }
public enum TTrigger { Next }

public sealed class AsyncTransitionMatchedRecorder : IStateMachineExtension<TState, TTrigger>
{
    public readonly List<string> Log = new();
    public int TransitionMatchedCount { get; private set; }
    public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;
    public void OnAttemptStarting(in TransitionAttemptContext<TState, TTrigger> attempt) => Log.Add("AttemptStarting");
    public void OnAttemptCompleted(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionResult<TState> result) => Log.Add($"AttemptCompleted:{result.Outcome}");
    public void OnGuardEvaluating(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionInfo<TState> candidate, string _) => Log.Add("GuardEvaluating");
    public void OnGuardEvaluated(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionInfo<TState> candidate, string _, bool result) => Log.Add("GuardEvaluated");
    public void OnTransitionMatched(in TransitionAttemptContext<TState, TTrigger> attempt, in TransitionInfo<TState> matched) { TransitionMatchedCount++; Log.Add("TransitionMatched"); }
}

public class AsyncTransitionMatchedHookTests
{
    [Fact]
    public async Task Async_TransitionMatched_FiresOnce_InOrder()
    {
        var ext = new AsyncTransitionMatchedRecorder();
        var m = new AsyncTransitionMatchedMachine(TState.A, new IStateMachineExtension<TState, TTrigger>[] { ext });
        await m.StartAsync();

        var ok = await m.TryFireAsync(TTrigger.Next);
        ok.ShouldBeTrue();
        m.CurrentState.ShouldBe(TState.B);

        ext.TransitionMatchedCount.ShouldBe(1);
        ext.Log.ShouldBe(new[]
        {
            "AttemptStarting",
            "TransitionMatched",
            "GuardEvaluating",
            "GuardEvaluated",
            "AttemptCompleted:Succeeded"
        });
    }
}
