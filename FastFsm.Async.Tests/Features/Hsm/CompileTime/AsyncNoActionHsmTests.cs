using System.Threading.Tasks;
using Abstractions.Attributes;
using Xunit;

namespace StateMachine.Async.Tests.Features.Hsm.CompileTime;

public partial class AsyncNoActionHsmTests
{
    public enum S { Outside, Menu, Menu_Item }
    public enum T { Enter }

    [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
    public partial class TinyAsyncHsm
    {
        // Parent with shallow history; initial child
        [State(S.Menu, History = HistoryMode.Shallow, OnEntry = nameof(OnMenuEntryAsync))]
        [State(S.Menu_Item, Parent = S.Menu, IsInitial = true)]
        private void ConfigureStates() { }

        // Simple external transition, no actions
        [Transition(S.Outside, T.Enter, S.Menu)]
        private void ConfigureTransitions() { }

        // Async OnEntry to force async machine variant
        private ValueTask OnMenuEntryAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Async_HSM_without_actions_compiles_and_runs()
    {
        var sm = new TinyAsyncHsm(S.Outside);
        await sm.StartAsync();
        var ok = await sm.TryFireAsync(T.Enter);
        Assert.True(ok);
    }
}

