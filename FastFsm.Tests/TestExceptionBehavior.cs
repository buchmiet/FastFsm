using System;
using Xunit;
using FastFsm.Tests.Machines;
using FastFsm.Tests.Features.Core;
using Shouldly;

namespace FastFsm.Tests
{
    public class TestExceptionBehavior
    {
        [Fact]
        public void TestAttributeMachineExceptionBehavior()
        {
            // Test only the attribute machine
            var attrMachine = new ExceptionCallbackMachine(StateCallbackTests.ExceptionState.A);
            attrMachine.Start();
            
            attrMachine.CurrentState.ShouldBe(StateCallbackTests.ExceptionState.A);
            
            // Set to throw in OnEntry of state B
            attrMachine.ThrowInOnEntry = true;
            
            // This should throw
            Action action = () => attrMachine.Fire(StateCallbackTests.ExceptionTrigger.Go);
            action.ShouldThrow<InvalidOperationException>();
            
            // Check state after exception
            Console.WriteLine($"State after exception: {attrMachine.CurrentState}");
            
            // The question: does it stay in A or move to B?
            // Test expects A, but actual might be B
            attrMachine.CurrentState.ShouldBe(StateCallbackTests.ExceptionState.A);
        }
        
        [Fact]
        public void TestFluentMachineExceptionBehavior()
        {
            // Test only the fluent machine
            var fluentMachine = new ExceptionCallbackMachineFluentAPI(StateCallbackTests.ExceptionState.A);
            fluentMachine.Start();
            
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.ExceptionState.A);
            
            // Set to throw in OnEntry of state B
            fluentMachine.ThrowInOnEntry = true;
            
            // This should throw
            Action action = () => fluentMachine.Fire(StateCallbackTests.ExceptionTrigger.Go);
            action.ShouldThrow<InvalidOperationException>();
            
            // Check state after exception
            Console.WriteLine($"State after exception: {fluentMachine.CurrentState}");
            
            // The question: does it stay in A or move to B?
            fluentMachine.CurrentState.ShouldBe(StateCallbackTests.ExceptionState.A);
        }
    }
}