using System;
using System.Linq;
using Xunit;
using FastFsm.Tests.Machines;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;
using Shouldly;

namespace FastFsm.Tests
{
    /// <summary>
    /// Tests comparing behavior between attribute-based and FluentAPI state machines
    /// Ensures both approaches produce identical behavior
    /// </summary>
    public class FluentAPIComparisonTests
    {
        [Fact]
        public void BasicBenchmark_AttributeVsFluentAPI_ShouldBehaveIdentically()
        {
            // Arrange
            var attrMachine = new BasicBenchmarkMachine(BasicBenchmarkState.StateA);
            var fluentMachine = new BasicBenchmarkMachineFluentAPI(BasicBenchmarkState.StateA);

            // Act & Assert - both should transition identically
            attrMachine.CurrentState.ShouldBe(BasicBenchmarkState.StateA);
            fluentMachine.CurrentState.ShouldBe(BasicBenchmarkState.StateA);

            attrMachine.Fire(BasicBenchmarkTrigger.TriggerX);
            fluentMachine.Fire(BasicBenchmarkTrigger.TriggerX);

            attrMachine.CurrentState.ShouldBe(BasicBenchmarkState.StateB);
            fluentMachine.CurrentState.ShouldBe(BasicBenchmarkState.StateB);

            attrMachine.Fire(BasicBenchmarkTrigger.TriggerY);
            fluentMachine.Fire(BasicBenchmarkTrigger.TriggerY);

            attrMachine.CurrentState.ShouldBe(BasicBenchmarkState.StateC);
            fluentMachine.CurrentState.ShouldBe(BasicBenchmarkState.StateC);
        }

        [Fact]
        public void WithGuardBenchmark_AttributeVsFluentAPI_GuardsShouldWorkIdentically()
        {
            // Arrange
            var attrMachine = new WithGuardBenchmarkMachine(WithGuardBenchmarkState.StateA);
            var fluentMachine = new WithGuardBenchmarkMachineFluentAPI(WithGuardBenchmarkState.StateA);

            // Act & Assert - guards should work the same
            attrMachine.ShouldAllow = false;
            fluentMachine.ShouldAllow = false;

            var attrCanFire = attrMachine.CanFire(WithGuardBenchmarkTrigger.TriggerX);
            var fluentCanFire = fluentMachine.CanFire(WithGuardBenchmarkTrigger.TriggerX);

            attrCanFire.ShouldBe(false);
            fluentCanFire.ShouldBe(false);

            // Enable guard
            attrMachine.ShouldAllow = true;
            fluentMachine.ShouldAllow = true;

            attrCanFire = attrMachine.CanFire(WithGuardBenchmarkTrigger.TriggerX);
            fluentCanFire = fluentMachine.CanFire(WithGuardBenchmarkTrigger.TriggerX);

            attrCanFire.ShouldBe(true);
            fluentCanFire.ShouldBe(true);

            // Fire and verify state change
            attrMachine.Fire(WithGuardBenchmarkTrigger.TriggerX);
            fluentMachine.Fire(WithGuardBenchmarkTrigger.TriggerX);

            attrMachine.CurrentState.ShouldBe(WithGuardBenchmarkState.StateB);
            fluentMachine.CurrentState.ShouldBe(WithGuardBenchmarkState.StateB);
        }

        [Fact]
        public void GuardedCallback_AttributeVsFluentAPI_CallbacksShouldExecuteIdentically()
        {
            // Arrange
            var attrMachine = new GuardedCallbackMachine(GuardedState.Initial);
            var fluentMachine = new GuardedCallbackMachineFluentAPI(GuardedState.Initial);

            // Act
            attrMachine.Fire(GuardedTrigger.Start);
            fluentMachine.Fire(GuardedTrigger.Start);

            // Assert - both should have same counter values
            attrMachine.GuardCounter.ShouldBe(1);
            fluentMachine.GuardCounter.ShouldBe(1);
            attrMachine.ActionCounter.ShouldBe(1);
            fluentMachine.ActionCounter.ShouldBe(1);

            attrMachine.CurrentState.ShouldBe(GuardedState.Active);
            fluentMachine.CurrentState.ShouldBe(GuardedState.Active);

            // Test internal transition
            attrMachine.Fire(GuardedTrigger.Process);
            fluentMachine.Fire(GuardedTrigger.Process);

            attrMachine.InternalActionCounter.ShouldBe(1);
            fluentMachine.InternalActionCounter.ShouldBe(1);
        }

        [Fact]
        public void ComplexCallback_AttributeVsFluentAPI_AllCallbacksShouldExecuteIdentically()
        {
            // Arrange
            var attrMachine = new ComplexCallbackMachine(ComplexState.Off);
            var fluentMachine = new ComplexCallbackMachineFluentAPI(ComplexState.Off);

            // Act - test entry callbacks
            attrMachine.Start();
            fluentMachine.Start();

            attrMachine.OnEntryCalled.ShouldBe(true);
            fluentMachine.OnEntryCalled.ShouldBe(true);

            // Fire transition with guard and action
            attrMachine.CanTransition = true;
            fluentMachine.CanTransition = true;

            attrMachine.Fire(ComplexTrigger.Process);
            fluentMachine.Fire(ComplexTrigger.Process);

            attrMachine.GuardEvaluated.ShouldBe(true);
            fluentMachine.GuardEvaluated.ShouldBe(true);
            attrMachine.ActionExecuted.ShouldBe(true);
            fluentMachine.ActionExecuted.ShouldBe(true);

            // Test exit callback
            attrMachine.Fire(ComplexTrigger.Shutdown);
            fluentMachine.Fire(ComplexTrigger.Shutdown);

            attrMachine.OnExitCalled.ShouldBe(true);
            fluentMachine.OnExitCalled.ShouldBe(true);
        }

        [Fact]
        public void PayloadStateMachine_AttributeVsFluentAPI_PayloadHandlingShouldBeIdentical()
        {
            // Arrange
            var attrMachine = new PayloadStateMachine(TestState.StateA);
            var fluentMachine = new PayloadStateMachineFluentAPI(TestState.StateA);

            var payload = new TestPayload { Value = 42 };

            // Act & Assert - CanFire with payload
            var attrCanFire = attrMachine.CanFire(TestTrigger.TriggerX, payload);
            var fluentCanFire = fluentMachine.CanFire(TestTrigger.TriggerX, payload);

            attrCanFire.ShouldBe(true);
            fluentCanFire.ShouldBe(true);

            // Fire with payload
            attrMachine.Fire(TestTrigger.TriggerX, payload);
            fluentMachine.Fire(TestTrigger.TriggerX, payload);

            attrMachine.CurrentState.ShouldBe(TestState.StateB);
            fluentMachine.CurrentState.ShouldBe(TestState.StateB);

            // Test parameterless transition
            attrMachine.Fire(TestTrigger.TriggerY);
            fluentMachine.Fire(TestTrigger.TriggerY);

            attrMachine.CurrentState.ShouldBe(TestState.StateC);
            fluentMachine.CurrentState.ShouldBe(TestState.StateC);
        }

        [Fact]
        public void InitialStateMachine_AttributeVsFluentAPI_InitialStateHandlingShouldBeIdentical()
        {
            // Arrange & Act
            var attrMachine = new InitialStateMachine(InitialState.Initial);
            var fluentMachine = new InitialStateMachineFluentAPI(InitialState.Initial);

            // Assert - both should be in initial state
            attrMachine.CurrentState.ShouldBe(InitialState.Initial);
            fluentMachine.CurrentState.ShouldBe(InitialState.Initial);

            // Both should have called OnEntry for initial state
            attrMachine.InitialEntryCount.ShouldBe(1);
            fluentMachine.InitialEntryCount.ShouldBe(1);

            // Test transition
            attrMachine.Fire(InitialTrigger.Start);
            fluentMachine.Fire(InitialTrigger.Start);

            attrMachine.CurrentState.ShouldBe(InitialState.Processing);
            fluentMachine.CurrentState.ShouldBe(InitialState.Processing);

            attrMachine.ProcessingEntryCount.ShouldBe(1);
            fluentMachine.ProcessingEntryCount.ShouldBe(1);
        }

        [Fact]
        public void MultipleCallbacks_AttributeVsFluentAPI_AllCallbacksShouldExecute()
        {
            // Arrange
            var attrMachine = new MultipleCallbacksMachine(MultiState.First);
            var fluentMachine = new MultipleCallbacksMachineFluentAPI(MultiState.First);

            // Act
            attrMachine.Start();
            fluentMachine.Start();

            // Assert - verify all entry callbacks were called
            attrMachine.FirstEntryCount.ShouldBe(1);
            fluentMachine.FirstEntryCount.ShouldBe(1);
            attrMachine.SecondaryActionCount.ShouldBe(1);
            fluentMachine.SecondaryActionCount.ShouldBe(1);

            // Transition to second state
            attrMachine.Fire(MultiTrigger.Next);
            fluentMachine.Fire(MultiTrigger.Next);

            attrMachine.CurrentState.ShouldBe(MultiState.Second);
            fluentMachine.CurrentState.ShouldBe(MultiState.Second);

            // Test exit callbacks
            attrMachine.Fire(MultiTrigger.Reset);
            fluentMachine.Fire(MultiTrigger.Reset);

            attrMachine.SecondExitCount.ShouldBe(1);
            fluentMachine.SecondExitCount.ShouldBe(1);
        }

        [Fact]
        public void ExceptionCallback_AttributeVsFluentAPI_ExceptionHandlingShouldBeIdentical()
        {
            // Arrange
            var attrMachine = new ExceptionCallbackMachine(ExceptionState.Safe);
            var fluentMachine = new ExceptionCallbackMachineFluentAPI(ExceptionState.Safe);

            // Act - trigger exception in OnEntry
            attrMachine.ShouldThrowInOnEntry = true;
            fluentMachine.ShouldThrowInOnEntry = true;

            Action attrAction = () => attrMachine.Fire(ExceptionTrigger.GoToDanger);
            Action fluentAction = () => fluentMachine.Fire(ExceptionTrigger.GoToDanger);

            // Assert - both should throw
            attrAction.ShouldThrow<InvalidOperationException>();
            fluentAction.ShouldThrow<InvalidOperationException>();

            // Both should remain in original state due to exception
            attrMachine.CurrentState.ShouldBe(ExceptionState.Safe);
            fluentMachine.CurrentState.ShouldBe(ExceptionState.Safe);
        }

        [Fact]
        public void FullOrderMachine_AttributeVsFluentAPI_ComplexScenarioShouldWorkIdentically()
        {
            // Arrange
            var attrMachine = new FullOrderMachine(OrderState.New);
            var fluentMachine = new FullOrderMachineFluentAPI(OrderState.New);

            var orderData = new FullOrderData
            {
                OrderId = "TEST-001",
                Amount = 100.50m,
                CustomerEmail = "test@example.com"
            };

            // Act - process order
            attrMachine.Fire(OrderTrigger.Process, orderData);
            fluentMachine.Fire(OrderTrigger.Process, orderData);

            attrMachine.CurrentState.ShouldBe(OrderState.Processing);
            fluentMachine.CurrentState.ShouldBe(OrderState.Processing);

            // Process payment
            attrMachine.Fire(OrderTrigger.Pay, orderData);
            fluentMachine.Fire(OrderTrigger.Pay, orderData);

            attrMachine.CurrentState.ShouldBe(OrderState.Paid);
            fluentMachine.CurrentState.ShouldBe(OrderState.Paid);

            // Ship order
            attrMachine.Fire(OrderTrigger.Ship, orderData);
            fluentMachine.Fire(OrderTrigger.Ship, orderData);

            attrMachine.CurrentState.ShouldBe(OrderState.Shipped);
            fluentMachine.CurrentState.ShouldBe(OrderState.Shipped);

            // Deliver
            attrMachine.Fire(OrderTrigger.Deliver, orderData);
            fluentMachine.Fire(OrderTrigger.Deliver, orderData);

            attrMachine.CurrentState.ShouldBe(OrderState.Delivered);
            fluentMachine.CurrentState.ShouldBe(OrderState.Delivered);
        }

        [Fact]
        public void CoreBenchmark_AttributeVsFluentAPI_PerformanceShouldBeComparable()
        {
            // This test verifies that both versions compile to similar code
            // Actual performance should be measured with BenchmarkDotNet
            
            var attrMachine = new CoreBenchmarkMachine(BenchmarkState.A);
            var fluentMachine = new CoreBenchmarkMachineFluentAPI(BenchmarkState.A);

            const int iterations = 1000;

            // Warm up
            for (int i = 0; i < 10; i++)
            {
                attrMachine.Fire(BenchmarkTrigger.Next);
                fluentMachine.Fire(BenchmarkTrigger.Next);
            }

            // Continue cycling through states

            // Run iterations
            for (int i = 0; i < iterations; i++)
            {
                attrMachine.Fire(BenchmarkTrigger.Next);
                fluentMachine.Fire(BenchmarkTrigger.Next);
                
                // States cycle through A->B->C->D->A
                attrMachine.CurrentState.ShouldBe(fluentMachine.CurrentState);
            }
        }
    }
}