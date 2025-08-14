using System;
using Abstractions.Attributes;
using StateMachine.Exceptions;

namespace TestNamespace;

public enum TestState { A, B }
public enum TestTrigger { Go }

[StateMachine(typeof(TestState), typeof(TestTrigger))]
[OnException(nameof(Handle))]
public partial class TestExceptionMachine
{
    [Transition(TestState.A, TestTrigger.Go, TestState.B, Action = nameof(DoWork))]
    private void Configure() { }

    private void DoWork() => throw new InvalidOperationException("test");

    private ExceptionDirective Handle(ExceptionContext<TestState, TestTrigger> ctx)
        => ExceptionDirective.Continue;
}