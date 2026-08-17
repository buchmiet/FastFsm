using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Tests.Logging
{
    /// <summary>
    /// Tests that cover HSM-specific logging helper methods
    /// (currently not exercised by generator flows, but validated here).
    /// </summary>
    public class HsmLoggingTests : LoggingTestBase
    {
        [Fact]
        public void InternalTransitionOnAncestor_EmitsDebugEvent10()
        {
            // Act
            BasicStateMachineLog.InternalTransitionOnAncestor(
                LoggerMock.Object,
                instanceId: "id-1",
                ancestorState: "Parent",
                currentState: "Child",
                trigger: "Refresh");

            // Assert
            VerifyLogMessage(LogLevel.Debug, "InternalTransitionOnAncestor",
                "id-1", "Parent", "Child", "Refresh");
        }

        [Fact]
        public void HierarchicalTransition_EmitsDebugEvent11()
        {
            // Act
            BasicStateMachineLog.HierarchicalTransition(
                LoggerMock.Object,
                instanceId: "id-2",
                fromState: "S1.ChildA",
                toState: "S2.ChildB",
                lcaState: "Root",
                exitCount: 2,
                entryCount: 3);

            // Assert
            VerifyLogMessage(LogLevel.Debug, "HierarchicalTransition",
                "id-2", "S1.ChildA", "S2.ChildB", "Root");
        }

        [Fact]
        public void CompositeStateEntry_EmitsDebugEvent12()
        {
            // Act
            BasicStateMachineLog.CompositeStateEntry(
                LoggerMock.Object,
                instanceId: "id-3",
                compositeState: "Composite",
                resolvedTarget: "Composite.Initial",
                resolutionMethod: "Initial");

            // Assert
            VerifyLogMessage(LogLevel.Debug, "CompositeStateEntry",
                "id-3", "Composite", "Composite.Initial", "Initial");
        }

        [Fact]
        public void HistoryRestored_EmitsDebugEvent13()
        {
            // Act
            BasicStateMachineLog.HistoryRestored(
                LoggerMock.Object,
                instanceId: "id-4",
                compositeState: "Parent",
                restoredState: "Parent.ChildX",
                historyType: "Shallow");

            // Assert
            VerifyLogMessage(LogLevel.Debug, "HistoryRestored",
                "id-4", "Shallow", "Parent", "Parent.ChildX");
        }

        [Fact]
        public void ActivePath_EmitsTraceEvent14()
        {
            // Act
            BasicStateMachineLog.ActivePath(
                LoggerMock.Object,
                instanceId: "id-5",
                path: "Root / A / A1");

            // Assert
            VerifyLogMessage(LogLevel.Trace, "ActivePath",
                "id-5", "Root / A / A1");
        }
    }
}

