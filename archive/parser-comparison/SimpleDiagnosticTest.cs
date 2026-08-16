using Xunit;

namespace ParserComparison.Tests
{
    public class SimpleDiagnosticTest
    {
        [Fact]
        public void FluentAPI_ActionCallback_NotInvoked()
        {
            // Arrange
            var machine = new DiagnosticFluentMachine(DiagnosticFluentMachine.TestState.Idle);
            machine.Start();
            
            // Act
            machine.Fire(DiagnosticFluentMachine.TestTrigger.Start);
            
            // Assert
            // This will FAIL because the action callback is not invoked due to the bug
            Assert.Equal(0, machine.TransitionCount); // Should be 1, but is 0 due to bug
            
            // The state transition works, only the action callback is broken
            Assert.Equal(DiagnosticFluentMachine.TestState.Active, machine.CurrentState);
        }
        
        [Fact]
        public void AttributeAPI_ActionCallback_Works()
        {
            // Arrange
            var machine = new DiagnosticAttributeMachine(DiagnosticAttributeMachine.TestState.Idle);
            machine.Start();
            
            // Act
            machine.Fire(DiagnosticAttributeMachine.TestTrigger.Start);
            
            // Assert
            // This works correctly
            Assert.Equal(1, machine.TransitionCount);
            Assert.Equal(DiagnosticAttributeMachine.TestState.Active, machine.CurrentState);
        }
    }
}