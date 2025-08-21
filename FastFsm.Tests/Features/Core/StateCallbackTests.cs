using System;
using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.Core;

public class StateCallbackTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void OnEntryOnExit_ExecutionOrder_IsCorrect()
    {
        // Arrange
        var machine = new Machines.CallbackOrderMachine(CallbackState.A);
        machine.Start();
        var typedMachine = machine;

        // Act - Transition A -> B
        machine.Fire(CallbackTrigger.Next);

        // Assert
        var expected = new[] { "Exit-A", "Entry-B", "Action-A-to-B" };
        Assert.Equal(expected, typedMachine.ExecutionLog);

        // Act - Transition B -> C
        typedMachine.ExecutionLog.Clear();
        machine.Fire(CallbackTrigger.Next);

        // Assert
        expected = new[] { "Exit-B", "Entry-C", "Action-B-to-C" };
        Assert.Equal(expected, typedMachine.ExecutionLog);
    }


    [Fact]
    public void InitialState_OnEntry_IsCalledInConstructor()
    {
        // Arrange & Act
        var machine = new Machines.InitialStateMachine(InitialState.Start);
        machine.Start();
        var typedMachine = machine;

        // Assert - OnEntry was called during construction
        Assert.Single(typedMachine.EventLog);
        Assert.Equal("OnEntry-Start", typedMachine.EventLog[0]);

        // Further transitions work normally
        machine.Fire(InitialTrigger.Go);
        Assert.Equal(3, typedMachine.EventLog.Count); // +OnExit-Start, +OnEntry-Next
    }

    [Fact]
    public void InternalTransition_DoesNotTrigger_OnEntryOnExit()
    {
        // Arrange
        var machine = new Machines.InternalTransitionMachine(InternalState.Active);
        machine.Start();
        var typedMachine = machine;

        // Clear initial OnEntry
        typedMachine.EventLog.Clear();

        // Act - Internal transition
        machine.Fire(InternalTrigger.Update);

        // Assert - Only action executed, no OnEntry/OnExit
        Assert.Single(typedMachine.EventLog);
        Assert.Equal("InternalAction", typedMachine.EventLog[0]);
        Assert.Equal(InternalState.Active, machine.CurrentState);

        // Act - Normal transition
        machine.Fire(InternalTrigger.Deactivate);

        // Assert - OnExit and OnEntry called
        Assert.Equal(3, typedMachine.EventLog.Count);
        Assert.Equal("OnExit-Active", typedMachine.EventLog[1]);
        Assert.Equal("OnEntry-Inactive", typedMachine.EventLog[2]);
    }

    [Fact]
    public void FailedGuard_DoesNotTrigger_OnExitOnEntry()
    {
        // Arrange
        var machine = new Machines.GuardedCallbackMachine(GuardedState.A);
        machine.Start();
        var typedMachine = machine;

        // Clear initial OnEntry
        typedMachine.EventLog.Clear();

        // Act - Try transition with failing guard
        typedMachine.AllowTransition = false;
        var result = machine.TryFire(GuardedTrigger.Go);

        // Assert
        Assert.False(result);
        Assert.Empty(typedMachine.EventLog); // No callbacks executed
        Assert.Equal(GuardedState.A, machine.CurrentState);

        // Act - Enable guard and retry
        typedMachine.AllowTransition = true;
        result = machine.TryFire(GuardedTrigger.Go);

        // Assert
        Assert.True(result);
        Assert.Equal(2, typedMachine.EventLog.Count);
        Assert.Equal("OnExit-A", typedMachine.EventLog[0]);
        Assert.Equal("OnEntry-B", typedMachine.EventLog[1]);
    }

    [Fact]
    public void SelfTransition_Triggers_OnExitAndOnEntry()
    {
        // Arrange
        var machine = new Machines.SelfTransitionMachine(SelfState.Active);
        machine.Start();
        var typedMachine = machine;

        // Clear initial OnEntry
        typedMachine.EventLog.Clear();

        // Act - Self transition (not internal)
        machine.Fire(SelfTrigger.Refresh);

        // Assert - OnExit i OnEntry są wywołane, a następnie Action
        Assert.Equal(3, typedMachine.EventLog.Count);
        Assert.Equal("OnExit-Active", typedMachine.EventLog[0]);
        Assert.Equal("OnEntry-Active", typedMachine.EventLog[1]);
        Assert.Equal("RefreshAction", typedMachine.EventLog[2]);

        // Stan pozostaje Active (self-transition)
        Assert.Equal(SelfState.Active, machine.CurrentState);
    }

    [Fact]
    public void StateCallbacks_WithExceptions_HandledCorrectly()
    {
        // Arrange
        var machine = new Machines.ExceptionCallbackMachine(ExceptionState.A);
        machine.Start();
        var typedMachine = machine;

        // Act & Assert - OnExit throws -> wyjątek i stan BEZ zmiany
        typedMachine.ThrowInOnExit = true;
        Assert.Throws<InvalidOperationException>(() => machine.Fire(ExceptionTrigger.Go));
        Assert.Equal(ExceptionState.A, machine.CurrentState); // state unchanged

        // Act & Assert - OnEntry throws -> wyjątek, ALE stan JUŻ zmieniony
        typedMachine.ThrowInOnExit = false;
        typedMachine.ThrowInOnEntry = true;
        Assert.Throws<InvalidOperationException>(() => machine.Fire(ExceptionTrigger.Go));
        Assert.Equal(ExceptionState.B, machine.CurrentState); // state already changed
    }


    [Fact]
    public void ComplexStateCallbacks_WithMultipleStates_WorkCorrectly()
    {
        // Arrange
        var machine = new Machines.ComplexCallbackMachine(ComplexCallbackState.Idle);
        machine.Start();
        var typedMachine = machine;

        // Act - Full workflow
        machine.Fire(ComplexCallbackTrigger.Start);
        machine.Fire(ComplexCallbackTrigger.Process);
        machine.Fire(ComplexCallbackTrigger.Complete);

        // Assert
        var expectedSequence = new[]
        {
            "Entry-Idle",      // Initial
            "Exit-Idle",       // Start
            "Entry-Ready",
            "Exit-Ready",      // Process
            "Entry-Processing",
            "Exit-Processing", // Complete
            "Entry-Done"
        };

        Assert.Equal(expectedSequence, typedMachine.EventSequence);

        // Verify cleanup happened
        Assert.True(typedMachine.ResourcesCleaned);
        Assert.NotNull(typedMachine.CompletionTime);
    }

    [Fact]
    public void MultipleOnEntryOnExit_ForSameState_NotSupported()
    {
        // This is more of a compile-time test
        // The generator should handle multiple [State] attributes for the same state
        // by either using the last one or combining them

        var machine = new Machines.MultipleCallbacksMachine(MultiState.A);
        machine.Start();
        var typedMachine = machine;

        // The behavior depends on generator implementation
        // Assuming it uses the last defined callback
        Assert.Contains("Entry2", typedMachine.Log);
    }

    // Test state machines
    public enum CallbackState { A, B, C }
    public enum CallbackTrigger { Next }

      

    public enum InitialState { Start, Next }
    public enum InitialTrigger { Go }

      

    public enum InternalState { Active, Inactive }
    public enum InternalTrigger { Update, Deactivate }

      

    public enum GuardedState { A, B }
    public enum GuardedTrigger { Go }

      

    public enum SelfState { Active }
    public enum SelfTrigger { Refresh }

       

    public enum ExceptionState { A, B }
    public enum ExceptionTrigger { Go }

      

    public enum ComplexCallbackState { Idle, Ready, Processing, Done }
    public enum ComplexCallbackTrigger { Start, Process, Complete }

       

    public enum MultiState { A, B }
    public enum MultiTrigger { Go }

      
}
