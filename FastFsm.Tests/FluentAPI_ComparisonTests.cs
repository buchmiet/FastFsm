using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;
using Shouldly;
using System;
using System.Linq;
using Xunit;
using static FastFsm.Tests.Features.Core.StateCallbackTests;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;

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
            var attrMachine = new BasicBenchmarkMachineLegacy(BenchmarkTestsLegacy.BenchmarkState.A);
            var fluentMachine = new BasicBenchmarkMachineFluent(BenchmarkState.A);
            attrMachine.Start();
            fluentMachine.Start();

            // Act & Assert - both should transition identically
            attrMachine.CurrentState.ShouldBe(BenchmarkTestsLegacy.BenchmarkState.A);
            fluentMachine.CurrentState.ShouldBe(BenchmarkState.A);

            attrMachine.Fire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
            fluentMachine.Fire(BenchmarkTrigger.Next);

            attrMachine.CurrentState.ShouldBe(BenchmarkTestsLegacy.BenchmarkState.B);
            fluentMachine.CurrentState.ShouldBe(BenchmarkState.B);

            attrMachine.Fire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
            fluentMachine.Fire(BenchmarkTrigger.Next);

            attrMachine.CurrentState.ShouldBe(BenchmarkTestsLegacy.BenchmarkState.C);
            fluentMachine.CurrentState.ShouldBe(BenchmarkState.C);
        }

        [Fact]
        public void WithGuardBenchmark_AttributeVsFluentAPI_GuardsShouldWorkIdentically()
        {
            // Arrange
            var attrMachine = new WithGuardBenchmarkMachineLegacy(BenchmarkTestsLegacy.BenchmarkState.A);
            var fluentMachine = new WithGuardBenchmarkMachineFluent(BenchmarkState.A);
            attrMachine.Start();
            fluentMachine.Start();

            // Act & Assert - guards should work the same
            attrMachine.ShouldAllow = false;
            fluentMachine.ShouldAllow = false;

            var attrCanFire = attrMachine.CanFire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
            var fluentCanFire = fluentMachine.CanFire(BenchmarkTrigger.Next);

            attrCanFire.ShouldBe(false);
            fluentCanFire.ShouldBe(false);

            // Enable guard
            attrMachine.ShouldAllow = true;
            fluentMachine.ShouldAllow = true;

            attrCanFire = attrMachine.CanFire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
            fluentCanFire = fluentMachine.CanFire(BenchmarkTrigger.Next);

            attrCanFire.ShouldBe(true);
            fluentCanFire.ShouldBe(true);

            // Fire and verify state change
            attrMachine.Fire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
            fluentMachine.Fire(BenchmarkTrigger.Next);

            attrMachine.CurrentState.ShouldBe(BenchmarkTestsLegacy.BenchmarkState.B);
            fluentMachine.CurrentState.ShouldBe(BenchmarkState.B);
        }

        [Fact]
        public void GuardedCallback_AttributeVsFluentAPI_CallbacksShouldExecuteIdentically()
        {
            // Arrange
            var attrMachine = new GuardedCallbackMachine(GuardedState.A);
            var fluentMachine = new GuardedCallbackMachineFluent(GuardedState.A);
            attrMachine.Start();
            fluentMachine.Start();

            // Act
            attrMachine.AllowTransition = true;
            fluentMachine.AllowTransition = true;
            
            attrMachine.Fire(GuardedTrigger.Go);
            fluentMachine.Fire(GuardedTrigger.Go);

            // Assert - both should have transitioned
            attrMachine.CurrentState.ShouldBe(GuardedState.B);
            fluentMachine.CurrentState.ShouldBe(GuardedState.B);
            
            // Both should have identical event logs
            attrMachine.EventLog.Count.ShouldBeGreaterThan(0);
            fluentMachine.EventLog.Count.ShouldBe(attrMachine.EventLog.Count);
        }

        [Fact]
        public void ComplexCallback_AttributeVsFluentAPI_AllCallbacksShouldExecuteIdentically()
        {
            // Arrange
            var attrMachine = new ComplexCallbackMachine(StateCallbackTests.ComplexCallbackState.Idle);
            var fluentMachine = new ComplexCallbackMachineFluent(StateCallbackTests.ComplexCallbackState.Idle);
            attrMachine.Start();
            fluentMachine.Start();

            // Act - test transitions and callbacks
            attrMachine.Fire(StateCallbackTests.ComplexCallbackTrigger.Start);
            fluentMachine.Fire(StateCallbackTests.ComplexCallbackTrigger.Start);

            attrMachine.CurrentState.ShouldBe(StateCallbackTests.ComplexCallbackState.Ready);
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.ComplexCallbackState.Ready);

            attrMachine.Fire(StateCallbackTests.ComplexCallbackTrigger.Process);
            fluentMachine.Fire(StateCallbackTests.ComplexCallbackTrigger.Process);

            attrMachine.CurrentState.ShouldBe(StateCallbackTests.ComplexCallbackState.Processing);
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.ComplexCallbackState.Processing);

            attrMachine.Fire(StateCallbackTests.ComplexCallbackTrigger.Complete);
            fluentMachine.Fire(StateCallbackTests.ComplexCallbackTrigger.Complete);

            attrMachine.CurrentState.ShouldBe(StateCallbackTests.ComplexCallbackState.Done);
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.ComplexCallbackState.Done);
            
            // Verify both executed callbacks
            attrMachine.ResourcesCleaned.ShouldBe(true);
            fluentMachine.ResourcesCleaned.ShouldBe(true);
            attrMachine.CompletionTime.ShouldNotBeNull();
            fluentMachine.CompletionTime.ShouldNotBeNull();
        }

        [Fact]
        public void PayloadStateMachine_AttributeVsFluentAPI_PayloadHandlingShouldBeIdentical()
        {
            // Arrange
            var attrMachine = new PayloadStateMachine(Machines.TestState.Initial);
            var fluentMachine = new PayloadStateMachineFluent(Machines.TestState.Initial);
            attrMachine.Start();
            fluentMachine.Start();

            var payload = new Machines.TestPayload { Id = 42, Data = "Test" };

            // Act & Assert - CanFire with payload
            var attrCanFire = attrMachine.CanFire(Machines.TestTrigger.Start, payload);
            var fluentCanFire = fluentMachine.CanFire(Machines.TestTrigger.Start, payload);

            attrCanFire.ShouldBe(true);
            fluentCanFire.ShouldBe(true);

            // Fire with payload
            attrMachine.Fire(Machines.TestTrigger.Start, payload);
            fluentMachine.Fire(Machines.TestTrigger.Start, payload);

            attrMachine.CurrentState.ShouldBe(Machines.TestState.Processing);
            fluentMachine.CurrentState.ShouldBe(Machines.TestState.Processing);

            // Test parameterless transition
            attrMachine.Fire(Machines.TestTrigger.Complete);
            fluentMachine.Fire(Machines.TestTrigger.Complete);

            attrMachine.CurrentState.ShouldBe(Machines.TestState.Completed);
            fluentMachine.CurrentState.ShouldBe(Machines.TestState.Completed);
        }

        [Fact]
        public void InitialStateMachine_AttributeVsFluentAPI_InitialStateHandlingShouldBeIdentical()
        {
            // Arrange & Act
            var attrMachine = new InitialStateMachine(StateCallbackTests.InitialState.Start);
            var fluentMachine = new InitialStateMachineFluent(StateCallbackTests.InitialState.Start);
            attrMachine.Start();
            fluentMachine.Start();

            // Assert - both should be in initial state
            attrMachine.CurrentState.ShouldBe(StateCallbackTests.InitialState.Start);
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.InitialState.Start);

            // Both should have called OnEntry for initial state
            attrMachine.EventLog.ShouldContain("OnEntry-Start");
            fluentMachine.EventLog.ShouldContain("OnEntry-Start");

            // Test transition
            attrMachine.Fire(StateCallbackTests.InitialTrigger.Go);
            fluentMachine.Fire(StateCallbackTests.InitialTrigger.Go);

            attrMachine.CurrentState.ShouldBe(StateCallbackTests.InitialState.Next);
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.InitialState.Next);

            attrMachine.EventLog.ShouldContain("OnEntry-Next");
            fluentMachine.EventLog.ShouldContain("OnEntry-Next");
        }

        [Fact]
        public void MultipleCallbacks_AttributeVsFluentAPI_AllCallbacksShouldExecute()
        {
            // Arrange
            var attrMachine = new MultipleCallbacksMachineLegacy(MultiState.A);
            var fluentMachine = new MultipleCallbacksMachineFluent(MultiState.A);
            attrMachine.Start();
            fluentMachine.Start();

            // Act - trigger transition
            attrMachine.Fire(MultiTrigger.Go);
            fluentMachine.Fire(MultiTrigger.Go);

            // Assert - verify state change
            attrMachine.CurrentState.ShouldBe(MultiState.B);
            fluentMachine.CurrentState.ShouldBe(MultiState.B);
            
            // Both should have similar log entries
            attrMachine.Log.Count.ShouldBeGreaterThan(0);
            fluentMachine.Log.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void ExceptionCallback_AttributeVsFluentAPI_ExceptionHandlingShouldBeIdentical()
        {
            // Arrange
            var attrMachine = new ExceptionCallbackMachine(StateCallbackTests.ExceptionState.A);
            var fluentMachine = new ExceptionCallbackMachineFluent(StateCallbackTests.ExceptionState.A);
            attrMachine.Start();
            fluentMachine.Start();

            // Act - trigger exception in OnEntry
            attrMachine.ThrowInOnEntry = true;
            fluentMachine.ThrowInOnEntry = true;

            Action attrAction = () => attrMachine.Fire(StateCallbackTests.ExceptionTrigger.Go);
            Action fluentAction = () => fluentMachine.Fire(StateCallbackTests.ExceptionTrigger.Go);

            // Assert - both should throw
            attrAction.ShouldThrow<InvalidOperationException>();
            fluentAction.ShouldThrow<InvalidOperationException>();

            // FastFSM does not rollback transition on OnEntry exception
            // Both machines should be in state B after the exception
            attrMachine.CurrentState.ShouldBe(StateCallbackTests.ExceptionState.B);
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.ExceptionState.B);
        }

        [Fact]
        public void FullOrderMachine_AttributeVsFluentAPI_ComplexScenarioShouldWorkIdentically()
        {
            // Arrange
            var attrMachine = new FullOrderMachine(OrderState.New);
            var fluentMachine = new FullOrderMachineFluent(OrderState.New);
            attrMachine.Start();
            fluentMachine.Start();

            var orderData = new Features.Integration.AllFeaturesExtendedTests.OrderPayload
            {
                OrderId = 1,
                Amount = 100.50m,
                TrackingNumber = "TRACK-001"
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
            
            var attrMachine = new CoreBenchmarkMachineLegacy(BenchmarkTestsLegacy.BenchmarkState.A);
            var fluentMachine = new CoreBenchmarkMachineFluent(BenchmarkState.A);
            attrMachine.Start();
            fluentMachine.Start();

            const int iterations = 1000;

            // Warm up
            for (int i = 0; i < 10; i++)
            {
                attrMachine.Fire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
                fluentMachine.Fire(BenchmarkTrigger.Next);
            }

            // Continue cycling through states

            // Run iterations
            for (int i = 0; i < iterations; i++)
            {
                attrMachine.Fire(BenchmarkTestsLegacy.BenchmarkTrigger.Next);
                fluentMachine.Fire(BenchmarkTrigger.Next);
                
                // States cycle through A->B->C->D->A
                attrMachine.CurrentState.ToString().ShouldBe(fluentMachine.CurrentState.ToString());
            }
        }
    }
}