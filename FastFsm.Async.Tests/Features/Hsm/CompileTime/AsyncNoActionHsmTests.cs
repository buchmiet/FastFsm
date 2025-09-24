using System.Threading.Tasks;
using Abstractions.Attributes;
using Xunit;
using Dsl;

namespace FastFsm.Async.Tests.Features.Hsm.CompileTime;

public partial class AsyncNoActionHsmTests
{
    public enum S { Outside, Menu, Menu_Item }
    public enum T { Enter }

    [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
    public partial class TinyAsyncHsm
    {
        // Parent with shallow history; initial child
        [State(S.Menu, History = Abstractions.Attributes.HistoryMode.Shallow, OnEntry = nameof(OnMenuEntryAsync))]
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

    // Fluent API version
    [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
    public partial class TinyAsyncHsmFluentFsm
    {
        private void Configure()
        {
            // Parent with shallow history and initial child
            FSM.State(S.Menu)
                .HistoryShallow()
                .OnEntryAsync(nameof(OnMenuEntryAsync))
                .Initial(S.Menu_Item);
                
            FSM.State(S.Menu_Item)
                .ChildOf(S.Menu);
            
            FSM.State(S.Outside);
            
            // Simple external transition
            FSM.At(S.Outside)
                .On(T.Enter)
                .GoTo(S.Menu);
        }

        // Async OnEntry to force async machine variant
        private ValueTask OnMenuEntryAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Async_HSM_Fluent_without_actions_compiles_and_runs()
    {
        var sm = new TinyAsyncHsmFluentFsm(S.Outside);
        await sm.StartAsync();
        await sm.FireAsync(T.Enter);
        Assert.Equal(S.Menu_Item, sm.CurrentState);
    }
}
