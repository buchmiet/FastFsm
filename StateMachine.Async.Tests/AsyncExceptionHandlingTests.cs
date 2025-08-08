// AsyncExceptionHandlingTests.cs
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Async.Tests
{
    public class AsyncExceptionHandlingTests
    {
        [Fact]
        public async Task TryFireAsync_When_Guard_Throws_Should_Return_False_And_State_Unchanged()
        {
            var m = new ExceptionAsyncMachine(ExStates.Init);
            await m.StartAsync();

            var ok = await m.TryFireAsync(ExTriggers.GuardBoom);

            ok.ShouldBeFalse();
            m.CurrentState.ShouldBe(ExStates.Init);
            m.Log.ShouldContain("Guard:Begin");
            m.Log.ShouldNotContain("Action:Begin");
            m.Log.ShouldNotContain("OnEntry:Begin");
            m.Log.ShouldNotContain("OnExit:Begin");
        }

        [Fact]
        public async Task TryFireAsync_When_Action_Throws_Should_Throw_And_State_Changed()
        {
            var m = new ExceptionAsyncMachine(ExStates.Init);
            await m.StartAsync();

            // Teraz oczekujemy propagacji wyjątku z akcji:
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await m.TryFireAsync(ExTriggers.ActionBoom));

            // Brak rollbacku: stan docelowy ustawiony przed OnEntry/Action
            m.CurrentState.ShouldBe(ExStates.Middle);

            // Logi: guard przeszedł, akcja zaczęła i rzuciła
            m.Log.ShouldContain("GuardOk");
            m.Log.ShouldContain("Action:Begin");

            // Brak OnEntry/OnExit w tym scenariuszu:
            // - Init nie ma OnExit
            // - Middle nie ma OnEntry
            m.Log.ShouldNotContain("OnEntry:Begin");
            m.Log.ShouldNotContain("OnExit:Begin");
        }


        [Fact]
        public async Task TryFireAsync_When_OnEntry_Throws_Should_Throw_And_State_Changed()
        {
            var m = new ExceptionAsyncMachine(ExStates.Init);
            await m.StartAsync();

            await Should.ThrowAsync<InvalidOperationException>(
                async () => await m.TryFireAsync(ExTriggers.EntryBoom));

            // Brak rollbacku – stan docelowy ustawiony przed OnEntry
            m.CurrentState.ShouldBe(ExStates.Next);

            // Logi: guard przeszedł, OnEntry rozpoczęte i rzuciło, akcji nie ma
            m.Log.ShouldContain("GuardOk");
            m.Log.ShouldContain("OnEntry:Begin");
            m.Log.ShouldNotContain("Action:Begin");
        }


        [Fact]
        public async Task TryFireAsync_When_OnExit_Throws_Should_Return_False_And_State_Unchanged()
        {
            // startujemy w stanie Middle, który ma rzucające OnExit
            var m = new ExceptionAsyncMachine(ExStates.Middle);
            await m.StartAsync();
            var ok = await m.TryFireAsync(ExTriggers.ExitBoom);

            ok.ShouldBeFalse();
            m.CurrentState.ShouldBe(ExStates.Middle);
            m.Log.ShouldContain("OnExit:Begin");
            m.Log.ShouldNotContain("OnEntry:Begin");
        }

        [Fact]
        public async Task GetPermittedTriggersAsync_Should_Ignore_Guard_Exception()
        {
            var m = new ExceptionAsyncMachine(ExStates.Init);
            await m.StartAsync();

            var list = await m.GetPermittedTriggersAsync();

            list.ShouldNotContain(ExTriggers.GuardBoom); // guard rzuca, więc trigger nie powinien być dozwolony
        }
    }
}
