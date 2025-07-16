using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Abstractions.Attributes;

namespace StateMachine.Logging.Tests
{
    /// <summary>
    /// Integration tests for complex logging scenarios
    /// </summary>
    public class LoggingIntegrationTests : LoggingTestBase
    {
        [Fact]
        public void ComplexTransition_VerifyLogOrder()
        {
            // Arrange
            var machine = new BasicStateMachine(TestState.Initial, GetLogger<BasicStateMachine>());

            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert - Verify exact order of logs
            LoggedMessages.Count.ShouldBe(4);

            // 1. OnExit should be first
            LoggedMessages[0].EventId.Name.ShouldBe("OnExitExecuted");
            LoggedMessages[0].Level.ShouldBe(LogLevel.Debug);

            // 2. Action should be second
            LoggedMessages[1].EventId.Name.ShouldBe("ActionExecuted");
            LoggedMessages[1].Level.ShouldBe(LogLevel.Debug);

            // 3. OnEntry should be third
            LoggedMessages[2].EventId.Name.ShouldBe("OnEntryExecuted");
            LoggedMessages[2].Level.ShouldBe(LogLevel.Debug);

            // 4. TransitionSucceeded should be last
            LoggedMessages[3].EventId.Name.ShouldBe("TransitionSucceeded");
            LoggedMessages[3].Level.ShouldBe(LogLevel.Information);
        }

        [Fact]
        public void InitialStateOnEntry_LoggedDuringConstruction()
        {
            // Arrange
            LoggedMessages.Clear(); // Clear any previous logs

            // Act - OnEntry for initial state should be called in constructor
            var machine = new InitialOnEntryStateMachine(
                TestInitialState.Ready,
                GetLogger < InitialOnEntryStateMachine >());

            // Assert
            VerifyLogCount(1);
            VerifyLogMessage(LogLevel.Debug, "OnEntryExecuted", "OnReadyEntry", "Ready");
        }

        [Fact]
        public void LoggerDisabled_NoLogsGenerated()
        {
            // Arrange
            LoggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(false);
            var machine = new BasicStateMachine(TestState.Initial, LoggerMock.Object as ILogger<BasicStateMachine>);

            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing); // Transition should work
            VerifyNoLogs(); // But no logs should be generated
        }

        [Fact]
        public void MultipleTransitions_LogsAccumulate()
        {
            // Arrange
            var machine = new PureStateMachine(TestState.Initial, GetLogger<PureStateMachine>());

            // Act - Multiple transitions
            machine.TryFire(TestTrigger.Start);
            machine.TryFire(TestTrigger.Complete);
            machine.TryFire(TestTrigger.Reset); // This should fail

            // Assert
            machine.CurrentState.ShouldBe(TestState.Completed);
            LoggedMessages.Count.ShouldBe(3);

            // First transition
            LoggedMessages[0].EventId.Name.ShouldBe("TransitionSucceeded");
            LoggedMessages[0].Message.ShouldContain("Initial");
            LoggedMessages[0].Message.ShouldContain("Processing");

            // Second transition
            LoggedMessages[1].EventId.Name.ShouldBe("TransitionSucceeded");
            LoggedMessages[1].Message.ShouldContain("Processing");
            LoggedMessages[1].Message.ShouldContain("Completed");

            // Failed transition
            LoggedMessages[2].EventId.Name.ShouldBe("TransitionFailed");
            LoggedMessages[2].Message.ShouldContain("Completed");
            LoggedMessages[2].Message.ShouldContain("Reset");
        }

        [Fact]
        public void FullVariant_CompleteScenario_AllLogsPresent()
        {
            // Arrange
            var extensionBeforeCalled = false;
            var extensionAfterCalled = false;
            var extension = new TestExtension
            {
                BeforeTransitionCallback = _ => extensionBeforeCalled = true,
                AfterTransitionCallback = (_, success) =>
                {
                    extensionAfterCalled = true;
                    success.ShouldBeTrue();
                }
            };

            var machine = new FullStateMachine(
                TestState.Initial,
                new[] { extension },
                GetLogger < FullStateMachine >());

            var payload = new TestPayload { Id = 99, Data = "Integration Test" };

            // Act
            var result = machine.TryFire(TestTrigger.Start, payload);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TestState.Processing);
            machine.LastPayload.ShouldBe(payload);
            extensionBeforeCalled.ShouldBeTrue();
            extensionAfterCalled.ShouldBeTrue();

            // Verify logs
            var actionLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "ActionExecuted");
            actionLog.ShouldNotBe(default);
            actionLog.Message.ShouldContain("ProcessAction");

            var onEntryLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "OnEntryExecuted");
            onEntryLog.ShouldNotBe(default);
            onEntryLog.Message.ShouldContain("OnProcessingEntry");

            var transitionLog = LoggedMessages.FirstOrDefault(l => l.EventId.Name == "TransitionSucceeded");
            transitionLog.ShouldNotBe(default);
            transitionLog.Message.ShouldContain("Initial");
            transitionLog.Message.ShouldContain("Processing");
        }

        [Fact]
        public void InstanceId_RemainsConsistentAcrossLogs()
        {
            // Arrange
            var machine = new BasicStateMachine(TestState.Initial, GetLogger<BasicStateMachine>());

            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            // Extract instance IDs from all log messages
            var instanceIds = LoggedMessages
                .Select(log => ExtractInstanceId(log.Message))
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            instanceIds.Count.ShouldBe(1); // All logs should have the same instance ID
            instanceIds[0].ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void DifferentLogLevels_OnlyEnabledLevelsLogged()
        {
            // -------------------------------------------------
            // Arrange
            // -------------------------------------------------
            // Wyłączamy poziom Debug, zostawiamy Info i Warning
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(false);
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

            var machine = new BasicStateMachine(
                TestState.Initial,
                GetLogger<BasicStateMachine>());   // logger z przekierowaniem do LoggerMock

            // -------------------------------------------------
            // Act
            // -------------------------------------------------
            machine.TryFire(TestTrigger.Start);

            // -------------------------------------------------
            // Assert
            // -------------------------------------------------
            // Powinien pojawić się tylko TransitionSucceeded (Information)
            // Logi Debug (OnExit, Action, OnEntry) powinny być odfiltrowane
            VerifyLogCount(1);
            VerifyLogMessage(LogLevel.Information, "TransitionSucceeded");
        }


        private string ExtractInstanceId(string message)
        {
            // Simple extraction - assumes instance ID is a GUID in the message
            var parts = message.Split(' ');
            foreach (var part in parts)
            {
                if (Guid.TryParse(part.Trim(',', '.', '"'), out _))
                {
                    return part.Trim(',', '.', '"');
                }
            }
            return string.Empty;
        }
    }

    // Additional test state machine for initial OnEntry testing
    public enum TestInitialState { Ready, Working, Done }
    public enum TestInitialTrigger { Go, Stop }

    [StateMachine(typeof(TestInitialState), typeof(TestInitialTrigger))]
    [GenerationMode(GenerationMode.Basic, Force = true)]
    public partial class InitialOnEntryStateMachine
    {
        [State(TestInitialState.Ready, OnEntry = nameof(OnReadyEntry))]
        private void ConfigureReady() { }

        private void OnReadyEntry() { }
    }
}