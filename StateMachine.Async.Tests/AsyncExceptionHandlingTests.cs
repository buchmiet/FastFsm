// AsyncExceptionHandlingTests.cs
using Shouldly;
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

            var ok = await m.TryFireAsync(ExTriggers.GuardBoom);

            ok.ShouldBeFalse();
            m.CurrentState.ShouldBe(ExStates.Init);
            m.Log.ShouldContain("Guard:Begin");
            m.Log.ShouldNotContain("Action:Begin");
            m.Log.ShouldNotContain("OnEntry:Begin");
            m.Log.ShouldNotContain("OnExit:Begin");
        }

        [Fact]
        public async Task TryFireAsync_When_Action_Throws_Should_Return_False_And_State_Unchanged()
        {
            var m = new ExceptionAsyncMachine(ExStates.Init);

            var ok = await m.TryFireAsync(ExTriggers.ActionBoom);

            ok.ShouldBeFalse();
            m.CurrentState.ShouldBe(ExStates.Init);          // akcja rzuciła, więc stan nie zmieniony
            m.Log.ShouldContain("GuardOk");
            m.Log.ShouldContain("Action:Begin");
            m.Log.ShouldNotContain("OnEntry:Begin");          // nie doszliśmy do OnEntry
            m.Log.ShouldNotContain("OnExit:Begin");           // nie wychodziliśmy ze stanu z OnExit
        }

        [Fact]
        public async Task TryFireAsync_When_OnEntry_Throws_Should_Return_False_And_State_Unchanged()
        {
            var m = new ExceptionAsyncMachine(ExStates.Init);

            var ok = await m.TryFireAsync(ExTriggers.EntryBoom);

            ok.ShouldBeFalse();
            m.CurrentState.ShouldBe(ExStates.Init);
            m.Log.ShouldContain("GuardOk");
            m.Log.ShouldContain("OnEntry:Begin");             // weszliśmy w OnEntry i boom
            m.Log.ShouldNotContain("Action:Begin");
        }

        [Fact]
        public async Task TryFireAsync_When_OnExit_Throws_Should_Return_False_And_State_Unchanged()
        {
            // startujemy w stanie Middle, który ma rzucające OnExit
            var m = new ExceptionAsyncMachine(ExStates.Middle);

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

            var list = await m.GetPermittedTriggersAsync();

            list.ShouldNotContain(ExTriggers.GuardBoom); // guard rzuca, więc trigger nie powinien być dozwolony
        }
    }
}
