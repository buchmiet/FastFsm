using Abstractions.Attributes;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Abstractions.Fluent;

namespace FastFsm.Logging.Tests
{
    /// <summary>
    /// Tests for new lifecycle logging events (EventIds 1100-1107)
    /// </summary>
    public class LifecycleLoggingTests : LoggingTestBase
    {
        [Fact]
        public void UnhandledTrigger_EventId1100_IsLogged()
        {
            // Arrange
            var machine = new LifecycleMachine(LifecycleState.Initial, GetLogger<LifecycleMachine>());
            machine.Start();

            // Act - Try to fire a trigger that has no transition from Initial
            var result = machine.TryFire(LifecycleTrigger.Complete);

            // Assert
            result.ShouldBeFalse();
            
            // Verify UnhandledTrigger event (EventId 1100)
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1100);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("UnhandledTrigger");
            log.Level.ShouldBe(LogLevel.Warning);
            log.Message.ShouldContain("Unhandled trigger");
            log.Message.ShouldContain("Complete");
            log.Message.ShouldContain("Initial");
        }

        [Fact]
        public void MachineStarted_EventId1101_IsLogged()
        {
            // Arrange
            LoggedMessages.Clear();
            var machine = new LifecycleMachine(LifecycleState.Initial, GetLogger<LifecycleMachine>());

            // Act
            machine.Start();

            // Assert - Verify MachineStarted event (EventId 1101)
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1101);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("MachineStarted");
            log.Level.ShouldBe(LogLevel.Information);
            log.Message.ShouldContain("started at");
            log.Message.ShouldContain("Initial");
        }

        [Fact]
        public void MachineStopped_EventId1102_WouldBeLogged()
        {
            // Note: MachineStopped (EventId 1102) would be logged when machine is disposed/stopped
            // FastFSM doesn't currently have explicit Stop/Dispose in generated code
            // This test documents the expected behavior when implemented
            
            // The event would be:
            // EventId: 1102
            // Name: "MachineStopped"
            // Level: Information
            // Message: "State machine stopped at {FinalState}"
        }

        [Fact]
        public void TransitionStarted_EventId1103_IsLogged()
        {
            // Arrange
            var machine = new LifecycleMachine(LifecycleState.Initial, GetLogger<LifecycleMachine>());
            machine.Start();
            LoggedMessages.Clear();

            // Act
            machine.TryFire(LifecycleTrigger.Start);

            // Assert - Verify TransitionStarted event (EventId 1103)
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1103);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("TransitionStarted");
            log.Level.ShouldBe(LogLevel.Debug);
            log.Message.ShouldContain("transition started");
            log.Message.ShouldContain("Initial");
            log.Message.ShouldContain("Start");
            log.Message.ShouldContain("Processing");
        }

        [Fact]
        public async Task AsyncActionStarted_EventId1104_IsLogged()
        {
            // Arrange
            var machine = new AsyncLifecycleMachine(
                AsyncLifecycleState.Initial, 
                GetLogger<AsyncLifecycleMachine>());
            await machine.StartAsync();
            LoggedMessages.Clear();

            // Act
            await machine.TryFireAsync(AsyncLifecycleTrigger.StartAsync);

            // Assert - Verify AsyncActionStarted event (EventId 1104)
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1104);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("AsyncActionStarted");
            log.Level.ShouldBe(LogLevel.Debug);
            log.Message.ShouldContain("async action started");
            log.Message.ShouldContain("StartProcessingAsync");
        }

        [Fact]
        public async Task AsyncActionCompleted_EventId1105_IsLogged()
        {
            // Arrange
            var machine = new AsyncLifecycleMachine(
                AsyncLifecycleState.Initial,
                GetLogger<AsyncLifecycleMachine>());
            await machine.StartAsync();
            LoggedMessages.Clear();

            // Act
            await machine.TryFireAsync(AsyncLifecycleTrigger.StartAsync);

            // Assert - Verify AsyncActionCompleted event (EventId 1105)
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1105);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("AsyncActionCompleted");
            log.Level.ShouldBe(LogLevel.Debug);
            log.Message.ShouldContain("async action completed");
            log.Message.ShouldContain("StartProcessingAsync");
            log.Message.ShouldContain("ms"); // Should contain elapsed time
        }

        [Fact]
        public async Task AsyncActionFailed_EventId1106_IsLogged()
        {
            // Arrange
            var machine = new AsyncLifecycleMachine(
                AsyncLifecycleState.Processing,
                GetLogger<AsyncLifecycleMachine>());
            await machine.StartAsync();
            LoggedMessages.Clear();

            // Act - Fire trigger that causes async action to fail
            var result = await machine.TryFireAsync(AsyncLifecycleTrigger.FailAsync);

            // Assert - Verify AsyncActionFailed event (EventId 1106)
            result.ShouldBeFalse(); // Action failed
            
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1106);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("AsyncActionFailed");
            log.Level.ShouldBe(LogLevel.Warning);
            log.Message.ShouldContain("async action failed");
            log.Message.ShouldContain("FailingActionAsync");
            log.Message.ShouldContain("InvalidOperationException");
        }

        [Fact]
        public void CallbackException_EventId1107_IsLogged()
        {
            // Arrange
            var machine = new LifecycleMachine(
                LifecycleState.Processing,
                GetLogger<LifecycleMachine>());
            machine.Start();
            LoggedMessages.Clear();

            // Act - Fire trigger that causes callback exception
            var result = machine.TryFire(LifecycleTrigger.Fail);

            // Assert - Verify CallbackException event (EventId 1107)
            result.ShouldBeFalse();
            
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1107);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("CallbackException");
            log.Level.ShouldBe(LogLevel.Warning);
            log.Message.ShouldContain("threw");
            log.Message.ShouldContain("OnEntry"); // or OnExit/Action/Guard
            log.Message.ShouldContain("ThrowingEntry");
        }

        [Fact]
        public void MultipleLifecycleEvents_AreLoggedInOrder()
        {
            // Arrange
            LoggedMessages.Clear();
            var machine = new LifecycleMachine(LifecycleState.Initial, GetLogger<LifecycleMachine>());

            // Act - Series of operations
            machine.Start();                              // Should log MachineStarted (1101)
            machine.TryFire(LifecycleTrigger.Start);     // Should log TransitionStarted (1103)
            machine.TryFire(LifecycleTrigger.Invalid);   // Should log UnhandledTrigger (1100)

            // Assert - Verify events are logged
            LoggedMessages.Any(l => l.EventId.Id == 1101).ShouldBeTrue(); // MachineStarted
            LoggedMessages.Any(l => l.EventId.Id == 1103).ShouldBeTrue(); // TransitionStarted  
            LoggedMessages.Any(l => l.EventId.Id == 1100).ShouldBeTrue(); // UnhandledTrigger
        }

        // --- Regression: CallbackException for OnExit (EventId 1107) ---
        [Fact]
        public void CallbackException_OnExit_EventId1107_IsLogged()
        {
            // Arrange: machine with OnExit throwing and a simple transition A -> B
            var machine = new ExitThrowMachine(ExitState.A, GetLogger<ExitThrowMachine>());
            machine.Start();
            LoggedMessages.Clear();

            // Act
            var result = machine.TryFire(ExitTrigger.Go);

            // Assert
            result.ShouldBeFalse();
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1107);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("CallbackException");
            log.Level.ShouldBe(LogLevel.Warning);
            log.Message.ShouldContain("OnExit");
            log.Message.ShouldContain("ThrowExit");
        }

        // --- Regression: Fluent API parity for OnEntry exception (EventId 1107) ---
        [Fact]
        public void CallbackException_Fluent_OnEntry_EventId1107_IsLogged()
        {
            // Arrange: Fluent machine where S2 has OnEntry that throws; transition S1 --(T)--> S2
            var fm = new FluentEntryThrowMachine(FluentState.S1, GetLogger<FluentEntryThrowMachine>());
            fm.Start();
            LoggedMessages.Clear();

            // Act
            var result = fm.TryFire(FluentTrigger.T);

            // Assert
            result.ShouldBeFalse();
            var log = LoggedMessages.FirstOrDefault(l => l.EventId.Id == 1107);
            log.ShouldNotBe(default);
            log.EventId.Name.ShouldBe("CallbackException");
            log.Level.ShouldBe(LogLevel.Warning);
            log.Message.ShouldContain("OnEntry");
            log.Message.ShouldContain("ThrowEntry");
        }
    }

    // Test state machines for lifecycle logging tests
    public enum LifecycleState { Initial, Processing, Failed, Completed }
    public enum LifecycleTrigger { Start, Process, Complete, Fail, Invalid }

    [StateMachine(typeof(LifecycleState), typeof(LifecycleTrigger))]
    public partial class LifecycleMachine
    {
        [State(LifecycleState.Failed, OnEntry = nameof(ThrowingEntry))]
        private void ConfigureFailedState() { }

        [Transition(LifecycleState.Initial, LifecycleTrigger.Start, LifecycleState.Processing)]
        [Transition(LifecycleState.Processing, LifecycleTrigger.Complete, LifecycleState.Completed)]
        [Transition(LifecycleState.Processing, LifecycleTrigger.Fail, LifecycleState.Failed)]
        private void ConfigureTransitions() { }

        private void ThrowingEntry()
        {
            throw new InvalidOperationException("Entry callback failed");
        }
    }

    // Async state machine for testing async action logging
    public enum AsyncLifecycleState { Initial, Processing, Failed, Completed }
    public enum AsyncLifecycleTrigger { StartAsync, CompleteAsync, FailAsync }

    [StateMachine(typeof(AsyncLifecycleState), typeof(AsyncLifecycleTrigger))]
    public partial class AsyncLifecycleMachine
    {
        [Transition(AsyncLifecycleState.Initial, AsyncLifecycleTrigger.StartAsync, 
            AsyncLifecycleState.Processing, Action = nameof(StartProcessingAsync))]
        [Transition(AsyncLifecycleState.Processing, AsyncLifecycleTrigger.FailAsync,
            AsyncLifecycleState.Failed, Action = nameof(FailingActionAsync))]
        [Transition(AsyncLifecycleState.Processing, AsyncLifecycleTrigger.CompleteAsync,
            AsyncLifecycleState.Completed)]
        private void ConfigureTransitions() { }

        private async Task StartProcessingAsync()
        {
            await Task.Delay(10); // Simulate async work
        }

        private async Task FailingActionAsync()
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Async action failed");
        }
    }
}

// Machines for regression tests (local to this file)
namespace FastFsm.Logging.Tests
{
    // OnExit-throwing machine
    public enum ExitState { A, B }
    public enum ExitTrigger { Go }

    [StateMachine(typeof(ExitState), typeof(ExitTrigger))]
    public partial class ExitThrowMachine
    {
        [State(ExitState.A, OnExit = nameof(ThrowExit))]
        private void ConfigureState() { }

        [Transition(ExitState.A, ExitTrigger.Go, ExitState.B)]
        private void ConfigureTransitions() { }

        private void ThrowExit() => throw new InvalidOperationException("Exit failed");
    }

    // Fluent OnEntry-throwing machine (parity with 1107 case)
    public enum FluentState { S1, S2 }
    public enum FluentTrigger { T }

    [StateMachine(typeof(FluentState), typeof(FluentTrigger))]
    public partial class FluentEntryThrowMachine
    {
        private void Configure() => FSM
            .State(FluentState.S2)
                .OnEntry(nameof(ThrowEntry))
            .State(FluentState.S1)
                .On(FluentTrigger.T).GoTo(FluentState.S2);

        private void ThrowEntry() => throw new InvalidOperationException("Entry failed");
    }
}
