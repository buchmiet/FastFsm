using StateMachine.Contracts;
using StateMachine.Runtime;

using System.Linq;
using Xunit;


namespace StateMachine.Tests.Exceptions
{
    /// <summary>
    /// Zestaw testów weryfikujących poprawną obsługę wyjątków w rozszerzeniach.
    /// </summary>
    public class ExceptionHandlingTests
    {
        /// <summary>
        /// Test weryfikujący, że wyjątek rzucony przez jedno rozszerzenie
        /// nie przerywa przejścia stanu i pozwala na wykonanie kolejnych rozszerzeń.
        /// </summary>
        [Fact]
        public void Extension_Exception_DoesNot_Break_Transition()
        {
            // Arrange
            var throwing = new ThrowingExtension();
            var counting = new CountingExtension();
            // Tworzymy maszynę, przekazując rozszerzenia bezpośrednio do konstruktora
            var machine = new TestMachine(State.Initial, [throwing, counting]);

            // Act
            var result = machine.TryFire(Trigger.Next);

            // Assert
            Assert.True(result); // Przejście stanu powinno się powieść
            Assert.Equal(State.Final, machine.CurrentState); // Maszyna jest w nowym stanie
            Assert.Equal(1, counting.BeforeTransitionCount); // Drugie, poprawne rozszerzenie zostało wykonane
        }

        /// <summary>
        /// Test weryfikujący, że wyjątek z rozszerzenia jest poprawnie logowany.
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
        //    Assert.Single(logger.LoggedErrors); // Powinien być dokładnie jeden błąd w logach

        //    var loggedMessage = logger.LoggedErrors.First();
        //    Assert.Contains(nameof(ThrowingExtension), loggedMessage); // Log zawiera nazwę zepsutego rozszerzenia
        //    Assert.Contains(nameof(IStateMachineExtension.OnBeforeTransition), loggedMessage); // Log zawiera nazwę metody
        //    Assert.Contains("FromState=Initial", loggedMessage); // Log zawiera poprawny kontekst
        //    Assert.Contains("Trigger=Next", loggedMessage);
        //}
    }
}
