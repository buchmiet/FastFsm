using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using FastFsm.Runtime;
using FastFsm.Runtime.Extensions;

namespace FastFsm.Logging.Tests
{
    /// <summary>
    /// Tests for extension-related logging, especially error scenarios
    /// </summary>
    public class ExtensionLoggingTests : LoggingTestBase
    {
        [Fact]
        public void Extension_ThrowsInOnBeforeTransition_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnBeforeTransition = true };
            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                [extension],
                GetLogger<ExtensionsStateMachine>());
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing); // Transition should still succeed

            // Find the extension error log
            var errorLog = LoggedMessages.FirstOrDefault(l => l.Level == LogLevel.Error);
            errorLog.ShouldNotBe(default);
            errorLog.EventId.Name.ShouldBe("ExtensionError");
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnBeforeTransition");
            errorLog.Message.ShouldContain("Test exception in OnBeforeTransition");
        }

        [Fact]
        public void Extension_ThrowsInOnAfterTransition_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnAfterTransition = true };
            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                [extension],
                GetLogger<ExtensionsStateMachine>());
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing); // Transition should complete

            var errorLog = LoggedMessages.FirstOrDefault(l => l.Level == LogLevel.Error);
            errorLog.ShouldNotBe(default);
            errorLog.EventId.Name.ShouldBe("ExtensionError");
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnAfterTransition");
            errorLog.Message.ShouldContain("Test exception in OnAfterTransition");
        }

        [Fact]
        public void Extension_ThrowsInOnGuardEvaluation_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnGuardEvaluation = true };
            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                [extension],
                GetLogger<ExtensionsStateMachine>());
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing); // Guard should still be evaluated

            var errorLog = LoggedMessages.FirstOrDefault(l => l.Level == LogLevel.Error);
            errorLog.ShouldNotBe(default);
            errorLog.EventId.Name.ShouldBe("ExtensionError");
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnGuardEvaluation");
        }

        [Fact]
        public void Extension_ThrowsInOnGuardEvaluated_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnGuardEvaluated = true };
            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                [extension],
                GetLogger<ExtensionsStateMachine>());

            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing);

            var errorLog = LoggedMessages.FirstOrDefault(l => l.Level == LogLevel.Error);
            errorLog.ShouldNotBe(default);
            errorLog.EventId.Name.ShouldBe("ExtensionError");
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnGuardEvaluated");
        }

        [Fact]
        public void Extension_MultipleExtensions_OneThrows_OthersStillExecute()
        {
            // Arrange
            var extensionCallCount = 0;
            var throwingExtension = new TestExtension { ThrowOnBeforeTransition = true };
            var workingExtension = new TestExtension
            {
                BeforeTransitionCallback = _ => extensionCallCount++,
                AfterTransitionCallback = (_, __) => extensionCallCount++
            };

            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                [throwingExtension, workingExtension],
                GetLogger<ExtensionsStateMachine>());
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing);
            extensionCallCount.ShouldBe(2); // Both callbacks should have been called

            // Verify error was logged for throwing extension
            var errorLogs = LoggedMessages.Where(l => l.Level == LogLevel.Error).ToList();
            errorLogs.Count.ShouldBe(1);
            errorLogs[0].Message.ShouldContain("TestExtension");
        }

        [Fact]
        public void FullVariant_ExtensionThrowsWithPayload_LogsErrorWithContext()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnBeforeTransition = true };
            var machine = new FullStateMachine(
                TestState.Initial,
                [extension],
                GetLogger<FullStateMachine>());
            var payload = new TestPayload { Id = 42, Data = "Test" };
            machine.Start();
            // Act
            machine.TryFire(TestTrigger.Start, payload);

            // Assert
            machine.CurrentState.ShouldBe(TestState.Processing);
            machine.LastPayload.ShouldBe(payload); // Payload should be processed

            var errorLog = LoggedMessages.FirstOrDefault(l => l.Level == LogLevel.Error);
            errorLog.ShouldNotBe(default);
            errorLog.EventId.Name.ShouldBe("ExtensionError");
            errorLog.Message.ShouldContain("FromState=Initial");
            errorLog.Message.ShouldContain("Trigger=Start");
            errorLog.Message.ShouldContain("ToState=Processing");
        }

        [Fact]
        public void Extension_FailedTransition_AfterTransitionReceivesFalse()
        {
            // Arrange
            bool? afterTransitionSuccess = null;
            var extension = new TestExtension
            {
                AfterTransitionCallback = (_, success) => afterTransitionSuccess = success
            };

            var machine = new ExtensionsStateMachine(
                TestState.Initial,
                [extension],
                GetLogger<ExtensionsStateMachine>());
            machine.GuardResult = false; // Guard will fail
            machine.Start();
            // Act
            var result = machine.TryFire(TestTrigger.Start);

            // Assert
            result.ShouldBeFalse();
            afterTransitionSuccess.ShouldNotBeNull();
            afterTransitionSuccess.ShouldBe(false);

            // Verify logs
            VerifyLogMessage(LogLevel.Warning, "GuardFailed");
            VerifyLogMessage(LogLevel.Warning, "TransitionFailed");
        }

        [Fact]
        public void Extension_WithLogger_ExtensionRunnerLogsErrors()
        {
            // Arrange - Create ExtensionRunner directly with logger
            var extensionRunner = new ExtensionRunner(LoggerMock.Object);
            var extension = new TestExtension { ThrowOnBeforeTransition = true };
            var extensions = new[] { extension };

            var context = new StateMachineContext<TestState, TestTrigger>(
                Guid.NewGuid().ToString(),
                TestState.Initial,
                TestTrigger.Start,
                TestState.Processing,
                null);

            // Act
            extensionRunner.RunBeforeTransition(extensions, context);

            // Assert
            VerifyLogCount(1);
            VerifyLogMessage(LogLevel.Error, "ExtensionError",
                "TestExtension", "OnBeforeTransition", "Initial", "Start", "Processing");
        }

        [Fact]
        public void ExtensionRunner_DisabledLogLevel_DoesNotLog()
        {
            // Arrange
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(false);

            var extensionRunner = new ExtensionRunner(LoggerMock.Object);
            var extension = new TestExtension { ThrowOnBeforeTransition = true };
            var extensions = new[] { extension };

            var context = new StateMachineContext<TestState, TestTrigger>(
                Guid.NewGuid().ToString(),
                TestState.Initial,
                TestTrigger.Start,
                TestState.Processing,
                null);

            // Act
            extensionRunner.RunBeforeTransition(extensions, context);

            // Assert
            VerifyNoLogs(); // No logs should be recorded when log level is disabled
        }
    }
}