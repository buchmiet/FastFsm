using Abstractions.Attributes;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace FastFsm.Logging.Tests
{
    public class HsmRuntimeLoggingTests : LoggingTestBase
    {
        [Fact]
        public void InternalTransitionOnAncestor_IsLogged()
        {
            // Arrange
            var machine = new HsmMachine(HState.A, GetLogger<HsmMachine>());
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
            var machine = new HsmMachine(HState.A, GetLogger<HsmMachine>());
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

            void ILogger.Log<TState>(LogLevel level, EventId eventId, TState state, Exception? ex, Func<TState, Exception?, string> formatter)
            {
                Entries.Add(new(level, eventId.Name ?? string.Empty, formatter(state, ex)));
            }

            private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
        }
        [Fact]
        public void HistoryRestored_WhenReturningToA_IsLogged()
        {
            var logger = new TestLogger<HsmMachine>();
            var machine = new HsmMachine(HState.A, logger);

            machine.Start();                       // A1
            machine.TryFire(HTrigger.MoveToA2);    // A1 -> A2 (ustawia historię A=A2)
            machine.TryFire(HTrigger.Switch);      // A -> B (B1 Initial)
            machine.TryFire(HTrigger.Back);        // B -> A (przywraca historię A2)

            // Szukamy wpisów po nazwie eventu i fragmencie treści (odpornie na kolejność)
            VerifyLogMessage(logger, LogLevel.Debug, "CompositeStateEntry", "A", "A2", "History");
            VerifyLogMessage(logger, LogLevel.Debug, "HistoryRestored", "Shallow", "A", "A2");
            VerifyLogMessage(logger, LogLevel.Debug, "HierarchicalTransition", "B1", "A2");
            VerifyLogMessage(logger, LogLevel.Trace, "ActivePath", "A", "A2");
        }

        private static void VerifyLogMessage(
            TestLogger<HsmMachine> logger,
            LogLevel expectedLevel,
            string expectedEventName,
            params string[] expectedMessageParts)
        {
            var match = logger.Entries.FirstOrDefault(e =>
                e.Level == expectedLevel &&
                string.Equals(e.EventName, expectedEventName, StringComparison.Ordinal) &&
                expectedMessageParts.All(p => e.Message.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));

            if (match.Equals(default(TestLogger<HsmMachine>.LogEntry)))
            {
                var dump = string.Join(Environment.NewLine, logger.Entries.Select(e =>
                    $"[{e.Level}] {e.EventName}: {e.Message}"));
                throw new Xunit.Sdk.XunitException(
                    $"Expected event '{expectedEventName}' at level {expectedLevel} with parts: {string.Join(", ", expectedMessageParts)}" +
                    $"{Environment.NewLine}--- Captured logs ---{Environment.NewLine}{dump}");
            }
        }
    }

    public enum HState { A, A1, A2, B, B1 }
    public enum HTrigger { Refresh, MoveToA2, Switch, Back }

    [StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
    public partial class HsmMachine
    {
        public int Counter { get; private set; }

        // Define composite states and hierarchy
        [State(HState.A, History = HistoryMode.Shallow)]
        [State(HState.A1, Parent = HState.A, IsInitial = true)]
        [State(HState.A2, Parent = HState.A)]
        [State(HState.B)]
        [State(HState.B1, Parent = HState.B, IsInitial = true)]
        private void DefineStates() { }

        // Internal transition defined on ancestor A; should be matched when in A1/A2
        [InternalTransition(HState.A, HTrigger.Refresh, nameof(OnAncestorRefresh))]
        private void DefineAncestorInternal() { }

        private void OnAncestorRefresh() => Counter++;

        // External transitions
        private bool Always() => true;

        [Transition(HState.A1, HTrigger.MoveToA2, HState.A2, Guard = nameof(Always))]
        [Transition(HState.A, HTrigger.Switch, HState.B, Guard = nameof(Always))]
        [Transition(HState.B, HTrigger.Back, HState.A, Guard = nameof(Always))]
        private void DefineTransitions() { }
    }
}
