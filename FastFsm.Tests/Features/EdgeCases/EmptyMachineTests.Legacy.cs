using System;
using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.EdgeCases
{
    public class EmptyMachineTestsLegacy(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public void Legacy_EmptyStateMachine_WithNoTransitions_ShouldCompileAndWork()
        {
            // Arrange & Act
            var machine = new NoTransitionsMachineLegacy(EmptyMachineTests.EmptyState.Only);
            machine.Start();

            // Assert
            Assert.Equal(EmptyMachineTests.EmptyState.Only, machine.CurrentState);
            Assert.False(machine.CanFire(EmptyMachineTests.EmptyTrigger.Trigger));
            Assert.False(machine.TryFire(EmptyMachineTests.EmptyTrigger.Trigger));
            Assert.Empty(machine.GetPermittedTriggers());

            // Fire should throw
            Assert.Throws<InvalidOperationException>(() => machine.Fire(EmptyMachineTests.EmptyTrigger.Trigger));
        }

        [Fact]
        public void Legacy_StateMachine_WithSingleState_CanHaveSelfTransition()
        {
            // Arrange
            var machine = new Machines.SingleStateMachineLegacy(EmptyMachineTests.SingleState.Only);
            machine.Start();

            // Act & Assert
            Assert.True(machine.CanFire(EmptyMachineTests.SingleTrigger.Loop));
            Assert.True(machine.TryFire(EmptyMachineTests.SingleTrigger.Loop));
            Assert.Equal(EmptyMachineTests.SingleState.Only, machine.CurrentState);

            var typedMachine = machine as Machines.SingleStateMachineLegacy;
            Assert.Equal(1, typedMachine?.ActionCount);
        }

        [Fact]
        public void Legacy_StateMachine_WithUnreachableStates_ShouldStillFunction()
        {
            // Arrange
            var machine = new Machines.UnreachableMachineLegacy(EmptyMachineTests.UnreachableState.Start);
            machine.Start();

            // Act & Assert
            Assert.Equal(EmptyMachineTests.UnreachableState.Start, machine.CurrentState);
            Assert.Single(machine.GetPermittedTriggers());

            // Can reach Connected
            Assert.True(machine.TryFire(EmptyMachineTests.UnreachableTrigger.Connect));
            Assert.Equal(EmptyMachineTests.UnreachableState.Connected, machine.CurrentState);

            // Cannot reach Isolated
            Assert.False(machine.CanFire(EmptyMachineTests.UnreachableTrigger.Isolate));

            // Isolated is truly unreachable
            var permittedFromConnected = machine.GetPermittedTriggers();
            Assert.DoesNotContain(EmptyMachineTests.UnreachableTrigger.Isolate, permittedFromConnected);
        }

        [Fact]
        public void Legacy_StateMachine_WithOnlyInternalTransitions_NeverChangesState()
        {
            // Arrange
            var machine = new Machines.InternalOnlyMachineLegacy(EmptyMachineTests.InternalOnlyState.Static);
            machine.Start();
            var typedMachine = machine as Machines.InternalOnlyMachineLegacy;

            // Act
            for (int i = 0; i < 10; i++)
            {
                machine.Fire(EmptyMachineTests.InternalOnlyTrigger.Action);
            }

            // Assert
            Assert.Equal(EmptyMachineTests.InternalOnlyState.Static, machine.CurrentState);
            Assert.Equal(10, typedMachine?.ActionCount);
        }
    }
}