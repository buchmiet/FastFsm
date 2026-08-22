// AsyncExceptionHandlingTests.cs
using Shouldly;
using System;
using System.Threading.Tasks;
// using FastFsmTests.Tests; // unified to local namespace, no longer needed
using Xunit;

namespace Tests.Async.Features.Exceptions;
    public class AsyncExceptionHandlingTests
    {
        [Fact]
        public async Task TryFireAsync_When_Guard_Throws_Should_Return_False_And_State_Unchanged()
        {
            var m = new ExceptionAsyncMachineFluentFsm(ExStates.Init);
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
            var m = new ExceptionAsyncMachineFluentFsm(ExStates.Init);
            await m.StartAsync();

            // We now expect the exception from the action to propagate:
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await m.TryFireAsync(ExTriggers.ActionBoom));

            // No rollback: destination state is set before OnEntry/Action
            m.CurrentState.ShouldBe(ExStates.Middle);

            // Logs: guard passed, action started and threw
            m.Log.ShouldContain("GuardOk");
            m.Log.ShouldContain("Action:Begin");

            // No OnEntry/OnExit in this scenario:
            // - Init has no OnExit
            // - Middle has no OnEntry
            m.Log.ShouldNotContain("OnEntry:Begin");
            m.Log.ShouldNotContain("OnExit:Begin");
        }


        [Fact]
        public async Task TryFireAsync_When_OnEntry_Throws_Should_Throw_And_State_Changed()
        {
            var m = new ExceptionAsyncMachineFluentFsm(ExStates.Init);
            await m.StartAsync();

            await Should.ThrowAsync<InvalidOperationException>(
                async () => await m.TryFireAsync(ExTriggers.EntryBoom));

            // No rollback – destination state is set before OnEntry
            m.CurrentState.ShouldBe(ExStates.Next);

            // Logs: guard passed, OnEntry started and threw, no action
            m.Log.ShouldContain("GuardOk");
            m.Log.ShouldContain("OnEntry:Begin");
            m.Log.ShouldNotContain("Action:Begin");
        }


        [Fact]
        public async Task TryFireAsync_When_OnExit_Throws_Should_Return_False_And_State_Unchanged()
        {
            // start in Middle, which has a throwing OnExit
            var m = new ExceptionAsyncMachineFluentFsm(ExStates.Middle);
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
            var m = new ExceptionAsyncMachineFluentFsm(ExStates.Init);
            await m.StartAsync();

            var list = await m.GetPermittedTriggersAsync();

            list.ShouldNotContain(ExTriggers.GuardBoom); // guard throws, so the trigger should not be permitted
        }
    }
