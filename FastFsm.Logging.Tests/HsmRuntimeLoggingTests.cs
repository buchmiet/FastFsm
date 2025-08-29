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




        [Fact]
        public void HistoryRestored_WhenReturningToA_IsLogged()
        {
            // Arrange
            LoggedMessages.Clear();
            var machine = new HsmMachine(HState.A, GetLogger<HsmMachine>());
            machine.Start(); // A1

            // Move within A to A2 to establish history (external within same composite)
            machine.TryFire(HTrigger.MoveToA2); // A1 -> A2

            // Switch A -> B (land at B1)
            machine.TryFire(HTrigger.Switch);
            // Go back B -> A, should use shallow history to A2
            machine.TryFire(HTrigger.Back);

            // Assert
            // CompositeStateEntry for A should resolve to A2 with History
            VerifyLogMessage(LogLevel.Debug, "CompositeStateEntry", "A", "A2", "History");
            // HistoryRestored with Shallow and restored A2
            VerifyLogMessage(LogLevel.Debug, "HistoryRestored", "Shallow", "A", "A2");
            // Also expect hierarchical transition summary
            VerifyLogMessage(LogLevel.Debug, "HierarchicalTransition", "B1", "A2");
            // And ActivePath reflecting A / A2
            VerifyLogMessage(LogLevel.Trace, "ActivePath", "A", "A2");
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
