using System;
using Xunit;
using Xunit.Abstractions;

namespace StateMachine.Tests.EdgeCases
{
    public class EmptyMachineTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public void EmptyStateMachine_WithNoTransitions_ShouldCompileAndWork()
        {
            // Arrange & Act
            var machine = new NoTransitionsMachine(EmptyState.Only);

            // Assert
            Assert.Equal(EmptyState.Only, machine.CurrentState);
            Assert.False(machine.CanFire(EmptyTrigger.Trigger));
            Assert.False(machine.TryFire(EmptyTrigger.Trigger));
            Assert.Empty(machine.GetPermittedTriggers());

            // Fire should throw
            Assert.Throws<InvalidOperationException>(() => machine.Fire(EmptyTrigger.Trigger));
        }

        [Fact]
        public void StateMachine_WithSingleState_CanHaveSelfTransition()
        {
            // Arrange
            var machine = new Machines.SingleStateMachine(SingleState.Only);

            // Act & Assert
            Assert.True(machine.CanFire(SingleTrigger.Loop));
            Assert.True(machine.TryFire(SingleTrigger.Loop));
            Assert.Equal(SingleState.Only, machine.CurrentState);

            var typedMachine = machine as Machines.SingleStateMachine;
            Assert.Equal(1, typedMachine?.ActionCount);
        }

        [Fact]
        public void StateMachine_WithUnreachableStates_ShouldStillFunction()
        {
            // Arrange
            var machine = new Machines.UnreachableMachine(UnreachableState.Start);

            // Act & Assert
            Assert.Equal(UnreachableState.Start, machine.CurrentState);
            Assert.Single(machine.GetPermittedTriggers());

            // Can reach Connected
            Assert.True(machine.TryFire(UnreachableTrigger.Connect));
            Assert.Equal(UnreachableState.Connected, machine.CurrentState);

            // Cannot reach Isolated
            Assert.False(machine.CanFire(UnreachableTrigger.Isolate));

            // Isolated is truly unreachable
            var permittedFromConnected = machine.GetPermittedTriggers();
            Assert.DoesNotContain(UnreachableTrigger.Isolate, permittedFromConnected);
        }

        [Fact]
        public void StateMachine_WithOnlyInternalTransitions_NeverChangesState()
        {
            // Arrange
            var machine = new Machines.InternalOnlyMachine(InternalOnlyState.Static);
            var typedMachine = machine as Machines.InternalOnlyMachine;

            // Act
            for (int i = 0; i < 10; i++)
            {
                machine.Fire(InternalOnlyTrigger.Action);
            }

            // Assert
            Assert.Equal(InternalOnlyState.Static, machine.CurrentState);
            Assert.Equal(10, typedMachine?.ActionCount);
        }

        // Test enums and machines
        public enum EmptyState { Only }
        public enum EmptyTrigger { Trigger }



        public enum SingleState { Only }
        public enum SingleTrigger { Loop }

 

        public enum UnreachableState { Start, Connected, Isolated }
        public enum UnreachableTrigger { Connect, Disconnect, Isolate }

     

        public enum InternalOnlyState { Static }
        public enum InternalOnlyTrigger { Action }

    
    }
}