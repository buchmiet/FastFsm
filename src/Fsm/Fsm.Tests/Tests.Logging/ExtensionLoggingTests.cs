using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using FastFsm.Contracts;
using FastFsm.Runtime.Extensions;

namespace Tests.Logging
{
    /// <summary>
    /// Tests for extension-related logging, especially error scenarios
    /// </summary>
    public class ExtensionLoggingTests : LoggingTestBase
    {
        [Fact]
        public void Extension_ThrowsInOnAttemptStarting_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnAttemptStarting = true };
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
            errorLog.EventId.ShouldBe(default);
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnAttemptStarting");
        }

        [Fact]
        public void Extension_ThrowsInOnAttemptCompleted_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnAttemptCompleted = true };
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
            errorLog.EventId.ShouldBe(default);
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnAttemptCompleted");
        }

        [Fact]
        public void Extension_ThrowsInOnGuardEvaluating_LogsError()
        {
            // Arrange
            var extension = new TestExtension { ThrowOnGuardEvaluating = true };
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
            errorLog.EventId.ShouldBe(default);
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnGuardEvaluating");
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
            errorLog.EventId.ShouldBe(default);
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnGuardEvaluated");
        }

        [Fact]
        public void Extension_MultipleExtensions_OneThrows_OthersStillExecute()
        {
            // Arrange
            var extensionCallCount = 0;
            var throwingExtension = new TestExtension { ThrowOnAttemptStarting = true };
            var workingExtension = new TestExtension
            {
                AttemptStartingCallback = _ => extensionCallCount++,
                AttemptCompletedCallback = (_, __) => extensionCallCount++
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
            var extension = new TestExtension { ThrowOnAttemptStarting = true };
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
            errorLog.EventId.ShouldBe(default);
            errorLog.Message.ShouldContain("SourceState=Initial");
            errorLog.Message.ShouldContain("Trigger=Start");
            errorLog.Message.ShouldContain("FinalState=Initial");
        }

        [Fact]
        public void Extension_FailedTransition_AttemptCompletedReceivesGuardRejected()
        {
            // Arrange
            TransitionOutcome? outcome = null;
            var extension = new TestExtension
            {
                AttemptCompletedCallback = (_, result) => outcome = result.Outcome
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
            outcome.ShouldBe(TransitionOutcome.GuardRejected);

            // TEMP diagnostic: dump all logged messages for inspection
            foreach (var log in LoggedMessages)
            {
                Console.WriteLine($"LOG: Level={log.Level}, Id={log.EventId.Id}, Name={(log.EventId.Name ?? "<null>")}, Msg='{log.Message}'");
            }

            // Verify logs with detailed dump on failure
            var dump = string.Join("\n", LoggedMessages.Select(l => $"Level={l.Level}, Id={l.EventId.Id}, Name={(l.EventId.Name ?? "<null>")}, Msg='{l.Message}'"));
            var hasGuardFailed = LoggedMessages.Any(l => l.Level == LogLevel.Warning && (l.EventId.Name ?? string.Empty) == "GuardFailed");
            hasGuardFailed.ShouldBeTrue($"Expected Warning/GuardFailed, got:\n{dump}");
            var hasTransitionFailed = LoggedMessages.Any(l => l.Level == LogLevel.Warning && (l.EventId.Name ?? string.Empty) == "TransitionFailed");
            hasTransitionFailed.ShouldBeTrue($"Expected Warning/TransitionFailed, got:\n{dump}");
        }

        [Fact]
        public void Extension_WithLogger_ExtensionRunnerLogsErrors()
        {
            // Arrange - Create ExtensionRunner directly with logger
            var extensionRunner = new ExtensionRunner(LoggerMock.Object);
            var extension = new TestExtension { ThrowOnAttemptStarting = true };
            var extensions = ExtensionSet<TestState, TestTrigger>.Create([extension]);

            var attempt = new TransitionAttemptContext<TestState, TestTrigger>(
                Guid.NewGuid(),
                1,
                TestState.Initial,
                TestTrigger.Start,
                null,
                0);

            // Act
            extensionRunner.RunAttemptStarting(extensions, in attempt);

            // Assert
            VerifyLogCount(1);
            var errorLog = LoggedMessages.Single();
            errorLog.Level.ShouldBe(LogLevel.Error);
            errorLog.EventId.ShouldBe(default);
            errorLog.Message.ShouldContain("TestExtension");
            errorLog.Message.ShouldContain("OnAttemptStarting");
            errorLog.Message.ShouldContain("Initial");
            errorLog.Message.ShouldContain("Start");
        }

        [Fact]
        public void ExtensionRunner_DisabledLogLevel_DoesNotLog()
        {
            // Arrange
            LoggerMock.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(false);

            var extensionRunner = new ExtensionRunner(LoggerMock.Object);
            var extension = new TestExtension { ThrowOnAttemptStarting = true };
            var extensions = ExtensionSet<TestState, TestTrigger>.Create([extension]);

            var attempt = new TransitionAttemptContext<TestState, TestTrigger>(
                Guid.NewGuid(),
                1,
                TestState.Initial,
                TestTrigger.Start,
                null,
                0);

            // Act
            extensionRunner.RunAttemptStarting(extensions, in attempt);

            // Assert
            VerifyNoLogs(); // No logs should be recorded when log level is disabled
        }
    }

    public sealed class TestExtension : IStateMachineExtension<TestState, TestTrigger>
    {
        public bool ThrowOnAttemptStarting { get; set; }
        public bool ThrowOnAttemptCompleted { get; set; }
        public bool ThrowOnGuardEvaluating { get; set; }
        public bool ThrowOnGuardEvaluated { get; set; }

        public Action<TransitionAttemptContext<TestState, TestTrigger>>? AttemptStartingCallback { get; set; }
        public Action<TransitionAttemptContext<TestState, TestTrigger>, TransitionResult<TestState>>? AttemptCompletedCallback { get; set; }

        public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;

        public void OnAttemptStarting(in TransitionAttemptContext<TestState, TestTrigger> attempt)
        {
            if (ThrowOnAttemptStarting)
                throw new InvalidOperationException("Test exception in OnAttemptStarting");

            AttemptStartingCallback?.Invoke(attempt);
        }

        public void OnAttemptCompleted(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionResult<TestState> result)
        {
            if (ThrowOnAttemptCompleted)
                throw new InvalidOperationException("Test exception in OnAttemptCompleted");

            AttemptCompletedCallback?.Invoke(attempt, result);
        }

        public void OnGuardEvaluating(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> candidate,
            string guardName)
        {
            if (ThrowOnGuardEvaluating)
                throw new InvalidOperationException("Test exception in OnGuardEvaluating");
        }

        public void OnGuardEvaluated(
            in TransitionAttemptContext<TestState, TestTrigger> attempt,
            in TransitionInfo<TestState> candidate,
            string guardName,
            bool result)
        {
            if (ThrowOnGuardEvaluated)
                throw new InvalidOperationException("Test exception in OnGuardEvaluated");
        }
    }
}
