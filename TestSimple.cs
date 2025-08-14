using System;
using Abstractions.Attributes;
using StateMachine.Exceptions;

namespace StateMachine.Tests.BasicVariant;

public enum TestState { A, B }
public enum TestTrigger { Go }

[StateMachine(typeof(TestState), typeof(TestTrigger))]
[OnException(nameof(Handle))]
public partial class TestMachine
{
    [Transition(TestState.A, TestTrigger.Go, TestState.B, Action = nameof(DoWork))]
    private void Configure() { }

    private void DoWork() => throw new InvalidOperationException("test");

    private ExceptionDirective Handle(ExceptionContext<TestState, TestTrigger> ctx)
        => ExceptionDirective.Continue;
}

public class TestRunner
{
    public static void Main()
    {
        Console.WriteLine("Testing exception handling...");
        var machine = new TestMachine(TestState.A);
        machine.Start();
        
        Console.WriteLine($"Initial state: {machine.CurrentState}");
        var result = machine.Fire(TestTrigger.Go);
        Console.WriteLine($"Transition result: {result}");
        Console.WriteLine($"Final state: {machine.CurrentState}");
        
        Console.WriteLine("Expected: State should be B, result should be true");
    }
}