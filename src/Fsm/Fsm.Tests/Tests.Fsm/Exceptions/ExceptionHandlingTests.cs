using Tests.Machines.Extensions;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Exceptions
{
    /// <summary>
    /// Test suite verifying correct exception handling in extensions.
    /// </summary>
    public class ExceptionHandlingTests
    {
        /// <summary>
        /// Verifies that an exception thrown by one extension
        /// does not abort the state transition and still allows later extensions to run.
        /// </summary>
        [Fact]
        public void Extension_Exception_DoesNot_Break_Transition()
        {
            // Arrange
            var throwing = new ThrowingExtension();
            var counting = new CountingExtension();
            // Create the machine, passing extensions directly to the constructor
            var machine = new TestMachine(BasicState.Initial, [throwing, counting]);
            machine.Start();

            // Act
            var result = machine.TryFire(Trigger.Next);

            // Assert
            Assert.True(result); // The state transition should succeed
            Assert.Equal(BasicState.Final, machine.CurrentState); // The machine is in the new state
            Assert.Equal(1, counting.BeforeTransitionCount); // The second, correct extension was executed
        }

        /// <summary>
        /// Verifies that an exception from an extension is logged correctly.
        /// </summary>
        //[Fact]
        //public void Extension_Exception_Is_Logged()
        //{
        //    // Arrange
        //    var logger = new TestLogger<ExtensionRunner>();
        //    var throwing = new ThrowingExtension();
        //    var runner = new ExtensionRunner(logger);
        //    var context = new StateMachineContext<State, Trigger>(
        //        instanceId: "test-instance-1",
        //        fromState: State.Initial,
        //        trigger: Trigger.Next,
        //        toState: State.Final);

        //    // Act
        //    runner.RunBeforeTransition([throwing], context);

        //    // Assert
        //    Assert.Single(logger.LoggedErrors); // There should be exactly one error in the logs

        //    var loggedMessage = logger.LoggedErrors.First();
        //    Assert.Contains(nameof(ThrowingExtension), loggedMessage); // Log contains the name of the broken extension
        //    Assert.Contains(nameof(IStateMachineExtension.OnBeforeTransition), loggedMessage); // Log contains the method name
        //    Assert.Contains("FromState=Initial", loggedMessage); // Log contains the correct context
        //    Assert.Contains("Trigger=Next", loggedMessage);
        //}
    }
}
