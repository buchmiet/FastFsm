//using System;
//using System.Threading;
//using Xunit;
//using Xunit.Abstractions;

//namespace StateMachine.Tests.HierarchicalStateMachine;

//public class HsmExceptionTests
//{
//    private readonly ITestOutputHelper _output;

//    public HsmExceptionTests(ITestOutputHelper output)
//    {
//        _output = output;
//    }

//    [Fact]
//    public void OnExit_Exception_PreventsTransition()
//    {
//        // Arrange
//        var machine = new ExceptionHandlingHsmMachine(HsmState.A1);
//        machine.ThrowInExit = true;
//        machine.ExecutionLog.Clear();

//        // Act & Assert
//        Assert.Throws<InvalidOperationException>(() => machine.Fire(HsmTrigger.ToB));
        
//        // State should not have changed
//        Assert.Equal(HsmState.A1, machine.CurrentState);
        
//        // Exit was attempted but failed
//        Assert.Contains("Exit-A1", machine.ExecutionLog);
        
//        // No entry should have occurred
//        Assert.DoesNotContain("Entry-B", machine.ExecutionLog);
//        Assert.DoesNotContain("Action-A1-to-B", machine.ExecutionLog);
//    }

//    [Fact]
//    public void OnEntry_Exception_WithContinue_SwallowsException()
//    {
//        // Arrange
//        var machine = new ExceptionHandlingHsmMachine(HsmState.D);
//        machine.ThrowInEntry = true;
//        machine.ExecutionLog.Clear();

//        // Act - Should not throw due to Continue directive
//        machine.Fire(HsmTrigger.ToA1);

//        // Assert - Transition completes despite exception
//        Assert.Equal(HsmState.A1, machine.CurrentState);
        
//        // Exception was handled
//        Assert.Contains("Exception-OnEntry-Entry exception", machine.ExecutionLog);
        
//        // Entry callbacks were attempted
//        Assert.Contains("Entry-A", machine.ExecutionLog);
//        Assert.Contains("Entry-A1", machine.ExecutionLog);
//    }

//    [Fact]
//    public void Action_Exception_WithContinue_SwallowsException()
//    {
//        // Arrange
//        var machine = new ExceptionHandlingHsmMachine(HsmState.A1);
//        machine.ThrowInAction = true;
//        machine.ExecutionLog.Clear();

//        // Act - Should not throw due to Continue directive
//        machine.Fire(HsmTrigger.ToB);

//        // Assert - Transition completes despite exception
//        Assert.Equal(HsmState.B, machine.CurrentState);
        
//        // Exception was handled
//        Assert.Contains("Exception-Action-Action exception", machine.ExecutionLog);
        
//        // Exit and entry occurred
//        Assert.Contains("Exit-A1", machine.ExecutionLog);
//        Assert.Contains("Exit-A", machine.ExecutionLog);
//        Assert.Contains("Entry-B", machine.ExecutionLog);
//    }

//    [Fact]
//    public void OperationCanceledException_AlwaysPropagates()
//    {
//        // This would require an async machine with cancellation token support
//        // Placeholder for when async HSM machines are implemented
//        // The test would verify that OCE always propagates regardless of OnException directive
//    }

//    [Fact]
//    public void MultipleExceptions_InHierarchy_HandlesSeparately()
//    {
//        // Test that exceptions at different levels of hierarchy are handled independently
//        // This would require a more complex test machine with multiple OnException handlers
//        // at different hierarchy levels
//    }
//}