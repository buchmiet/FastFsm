using Abstractions.Attributes;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Tests.Logging
{
    public class HsmRuntimeLoggingTests : LoggingTestBase
    {
        [Fact]
        public void InternalTransitionOnAncestor_IsLogged()
        {
            // Arrange
            var machine = new HsmMachineFluent(HState.A, GetLogger<HsmMachineFluent>());
            machine.Start(); // Descends to A1 as initial

            // Act
            machine.TryFire(HTrigger.Refresh);

            // Assert
            VerifyLogMessage(LogLevel.Debug, "InternalTransitionOnAncestor", "A", "A1", "Refresh");
        }

        [Fact]
        public void HierarchicalTransition_CompositeEntry_ActivePath_AreLogged()
        {
            // Arrange
            LoggedMessages.Clear();
            var machine = new HsmMachineFluent(HState.A, GetLogger<HsmMachineFluent>());
            machine.Start(); // A1

            // Act: A (A1) -> B (B1)
            machine.TryFire(HTrigger.Switch);

            // Assert
            // Composite entry into B resolved to B1
            VerifyLogMessage(LogLevel.Debug, "CompositeStateEntry", "B", "B1", "Initial");
            // Hierarchical transition summary contains from/to
            VerifyLogMessage(LogLevel.Debug, "HierarchicalTransition", "A1", "B1");
            // Active path after transition
            VerifyLogMessage(LogLevel.Trace, "ActivePath", "B", "B1");
        }


        public sealed class TestLogger<T> : ILogger<T>
        {
            public readonly List<LogEntry> Entries = new();
            public readonly record struct LogEntry(LogLevel Level, string EventName, string Message);

            IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;
            bool ILogger.IsEnabled(LogLevel level) => true;

            void ILogger.Log<TState>(LogLevel level, EventId eventId, TState state, Exception? ex, Func<TState, Exception?, string> formatter) => Entries.Add(new(level, eventId.Name ?? string.Empty, formatter(state, ex)));

            private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
        }
        [Fact]
        public void HistoryRestored_WhenReturningToA_IsLogged()
        {
            var logger = new TestLogger<HsmMachineFluent>();
            var machine = new HsmMachineFluent(HState.A, logger);

            machine.Start();                       // A1
            machine.TryFire(HTrigger.MoveToA2);    // A1 -> A2 (sets history A=A2)
            machine.TryFire(HTrigger.Switch);      // A -> B (B1 Initial)
            machine.TryFire(HTrigger.Back);        // B -> A (restores history A2)

            // Look up entries by event name and a message fragment (order-independent)
            VerifyLogMessage(logger, LogLevel.Debug, "CompositeStateEntry", "A", "A2", "History");
            VerifyLogMessage(logger, LogLevel.Debug, "HistoryRestored", "Shallow", "A", "A2");
            VerifyLogMessage(logger, LogLevel.Debug, "HierarchicalTransition", "B1", "A2");
            VerifyLogMessage(logger, LogLevel.Trace, "ActivePath", "A", "A2");
        }

        private static void VerifyLogMessage(
            TestLogger<HsmMachineFluent> logger,
            LogLevel expectedLevel,
            string expectedEventName,
            params string[] expectedMessageParts)
        {
            var match = logger.Entries.FirstOrDefault(e =>
                e.Level == expectedLevel &&
                string.Equals(e.EventName, expectedEventName, StringComparison.Ordinal) &&
                expectedMessageParts.All(p => e.Message.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));

            if (match.Equals(default(TestLogger<HsmMachineFluent>.LogEntry)))
            {
                var dump = string.Join(Environment.NewLine, logger.Entries.Select(e =>
                    $"[{e.Level}] {e.EventName}: {e.Message}"));
                throw new Xunit.Sdk.XunitException(
                    $"Expected event '{expectedEventName}' at level {expectedLevel} with parts: {string.Join(", ", expectedMessageParts)}" +
                    $"{Environment.NewLine}--- Captured logs ---{Environment.NewLine}{dump}");
            }
        }
    }



  
}
