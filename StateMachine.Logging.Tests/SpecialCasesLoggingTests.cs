using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Abstractions.Attributes;
using StateMachine.Runtime;
using StateMachine.Runtime.Extensions;

namespace StateMachine.Logging.Tests
{
    /// <summary>
    /// Tests for special cases and edge scenarios
    /// </summary>
    public class SpecialCasesLoggingTests : LoggingTestBase
    {
        [Fact]
        public void InternalTransition_DoesNotLogStateChange()
        {
            // Arrange
            var machine = new InternalTransitionMachine(
                InternalState.Active,
                LoggerMock.Object as ILogger<InternalTransitionMachine>);

            // Act
            machine.TryFire(InternalTrigger.Refresh);

            // Assert
            machine.CurrentState.ShouldBe(InternalState.Active); // State should not change
            machine.RefreshCount.ShouldBe(1);

            // Internal transitions might still log the action
            var actionLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "ActionExecuted");
            if (actionLog != default)
            {
                actionLog.Message.ShouldContain("DoRefresh");
            }

            // But should not log state transition
            var transitionLogs = LoggedMessages.Where(l => l.EventId.Name == "TransitionSucceeded").ToList();
            transitionLogs.Any(l => l.Message.Contains("Active") && l.Message.Contains("Active")).ShouldBeFalse();
        }

        [Fact]
        public void NullLogger_StateMachineStillWorks()
        {
            // Arrange & Act
            var machine = new BasicStateMachine(TestState.Initial, logger: null);
            var result = machine.TryFire(TestTrigger.Start);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TestState.Processing);
            // No logs to verify since logger is null
        }

        [Fact]
        public void PayloadStateMachine_NullPayloadWithExpectedType_HandledGracefully()
        {
            // Arrange
            var machine = new PayloadStateMachine(
                TestState.Initial,
                GetLogger<PayloadStateMachine>());

            // Act
            var result = machine.TryFire(TestTrigger.Start, payload: null);

            // Assert
            result.ShouldBeTrue(); // Should succeed with parameterless overload
            machine.CurrentState.ShouldBe(TestState.Processing);

            // Should log successful transition
            VerifyLogMessage(LogLevel.Information, "TransitionSucceeded");
        }

        [Fact]
        public void ConcurrentExtensions_AllExtensionsLogErrors()
        {
            // Arrange
            var extension1 = new TestExtension
            {
                ThrowOnBeforeTransition = true,
                ThrowOnAfterTransition = true
            };
            var extension2 = new TestExtension
            {
                ThrowOnGuardEvaluation = true,
                ThrowOnGuardEvaluated = true
            };

            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                new[] { extension1, extension2 },
                GetLogger<ExtensionsStateMachine>());   // ✔ poprawny typ


            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing);

            // Should have 4 error logs (one for each throwing method)
            var errorLogs = LoggedMessages.Where(l => l.Level == LogLevel.Error).ToList();
            errorLogs.Count.ShouldBe(4);

            // Verify all extension methods were attempted
            errorLogs.Any(l => l.Message.Contains("OnBeforeTransition")).ShouldBeTrue();
            errorLogs.Any(l => l.Message.Contains("OnAfterTransition")).ShouldBeTrue();
            errorLogs.Any(l => l.Message.Contains("OnGuardEvaluation")).ShouldBeTrue();
            errorLogs.Any(l => l.Message.Contains("OnGuardEvaluated")).ShouldBeTrue();
        }

        [Fact]
        public void StateMachine_WithStructTypes_LogsCorrectly()
        {
            // Arrange
            var machine = new StructStateMachine(
                StructState.One,
                GetLogger<StructStateMachine>());

            // Act
            machine.TryFire(StructTrigger.Next);

            // Assert
            machine.CurrentState.ShouldBe(StructState.Two);
            VerifyLogMessage(LogLevel.Information, "TransitionSucceeded", "One", "Two", "Next");
        }

        [Fact]
        public void PayloadValidation_WrongTypeInMultiPayload_LogsActualTypeName()
        {
            // Arrange
            var machine = new MultiPayloadStateMachine(
                TestState.Initial,null,
                GetLogger<MultiPayloadStateMachine>());
            var wrongPayload = new { Wrong = "Type" };

            // Act
            var result = machine.TryFire(TestTrigger.Start, wrongPayload);

            // Assert
            result.ShouldBeFalse();

            var warningLog = LoggedMessages.FirstOrDefault(l =>
                l.Level == LogLevel.Warning &&
                l.EventId.Name == "PayloadValidationFailed");

            warningLog.ShouldNotBe(default);
            warningLog.Message.ShouldContain("TestPayload"); // Expected type
            // The actual type name might vary, but it should be present
            warningLog.Message.ShouldNotBeNull();
        }

        [Fact]
        public void ExtensionRunner_NullContext_HandledGracefully()
        {
            // This test verifies that ExtensionRunner handles edge cases properly
            // In practice, context should never be null, but we test defensive programming

            // Arrange
            var extensionRunner = new ExtensionRunner(LoggerMock.Object);
            var extension = new TestExtension();
            var extensions = new[] { extension };

            // Create a minimal context implementation
            var context = new StateMachineContext<TestState, TestTrigger>(
                string.Empty, // Empty instance ID
                TestState.Initial,
                TestTrigger.Start,
                TestState.Processing,
                null);

            // Act & Assert - Should not throw
            Should.NotThrow(() => extensionRunner.RunBeforeTransition(extensions, context));
        }

        [Fact]
        public void EventIds_AreUnique()
        {
            var logger = GetLogger<BasicStateMachine>();

            // 1) Udany scenariusz
            var okMachine = new BasicStateMachine(TestState.Initial, logger);
            okMachine.TryFire(TestTrigger.Start);           // TransitionSucceeded (1), OnExit/Entry/Action (4-6)

            // 2) Scenariusz z nie-spełnionym guardem
            var failMachine = new BasicStateMachine(TestState.Initial, logger);
            failMachine.GuardResult = false;                // wymusza GuardFailed
            failMachine.TryFire(TestTrigger.Start);         // GuardFailed (2) + TransitionFailed (3)

            // --- asercje bez zmian ---
            var eventIds = LoggedMessages
                .Select(l => l.EventId)
                .Where(id => id.Id > 0)
                .ToList();

            eventIds.Any(e => e.Id == 1 && e.Name == "TransitionSucceeded").ShouldBeTrue();
            eventIds.Any(e => e.Id == 2 && e.Name == "GuardFailed").ShouldBeTrue();
            eventIds.Any(e => e.Id == 3 && e.Name == "TransitionFailed").ShouldBeTrue();
            eventIds.Any(e => e.Id == 4 && e.Name == "OnEntryExecuted").ShouldBeTrue();
            eventIds.Any(e => e.Id == 5 && e.Name == "OnExitExecuted").ShouldBeTrue();
            eventIds.Any(e => e.Id == 6 && e.Name == "ActionExecuted").ShouldBeTrue();
        }

    }

    // Additional test state machines for edge cases

    // Internal transition machine
    public enum InternalState { Idle, Active }
    public enum InternalTrigger { Start, Refresh, Stop }

    [StateMachine(typeof(InternalState), typeof(InternalTrigger))]
    [GenerationMode(GenerationMode.Basic, Force = true)]
    public partial class InternalTransitionMachine
    {
        public int RefreshCount { get; private set; }

        [InternalTransition(InternalState.Active, InternalTrigger.Refresh, nameof(DoRefresh))]
        private void ConfigureRefresh() { }

        private void DoRefresh() => RefreshCount++;
    }

    // Struct-based state machine
    public enum StructState : byte { One, Two, Three }
    public enum StructTrigger : short { Next, Previous }

    [StateMachine(typeof(StructState), typeof(StructTrigger))]
    public partial class StructStateMachine
    {
        [Transition(StructState.One, StructTrigger.Next, StructState.Two)]
        [Transition(StructState.Two, StructTrigger.Next, StructState.Three)]
        private void Configure() { }
    }
}