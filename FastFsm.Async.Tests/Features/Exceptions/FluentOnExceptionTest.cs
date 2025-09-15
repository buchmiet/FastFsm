using System;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;
using Shouldly;
using Xunit.Abstractions;

namespace FastFsm.Async.Tests.Features.Exceptions;

public enum TestState { Idle, Working, Done, Error }
public enum TestTrigger { Start, Complete, Fail }

/// <summary>
/// Test machine using Fluent API with OnException handler
/// </summary>
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class FluentOnExceptionMachine
{
    public bool ExceptionHandled { get; private set; }
    public ExceptionDirective LastDirective { get; private set; }
    public Exception? LastException { get; private set; }
    
    private static void Configure() => FSM
        .OnException<TestState>(nameof(HandleException))
        .State(TestState.Idle)
            .On<TestTrigger>(TestTrigger.Start)
                .Action(nameof(ThrowingAction))
                .GoTo(TestState.Working)
        .State(TestState.Working)
            .On<TestTrigger>(TestTrigger.Complete)
                .GoTo(TestState.Done)
            .On<TestTrigger>(TestTrigger.Fail)
                .GoTo(TestState.Error);
    
    private async ValueTask ThrowingAction()
    {
        await Task.Yield();
        throw new InvalidOperationException("Test exception from action");
    }
    
    private ValueTask<ExceptionDirective> HandleException(ExceptionContext<TestState, TestTrigger> context)
    {
        ExceptionHandled = true;
        LastException = context.Exception;
        LastDirective = ExceptionDirective.Continue; // Continue to target state despite exception
        return new ValueTask<ExceptionDirective>(LastDirective);
    }
}

/// <summary>
/// Test for Fluent API OnException functionality
/// </summary>
public class FluentOnExceptionTests
{
    private readonly ITestOutputHelper _output;
    
    public FluentOnExceptionTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public async Task FluentApi_OnException_HandlesExceptionFromAction()
    {
        // Arrange
        var machine = new FluentOnExceptionMachine(TestState.Idle);
        await machine.StartAsync();

        // Act
        await machine.FireAsync(TestTrigger.Start);

        // Assert
        machine.ExceptionHandled.ShouldBeTrue("Exception handler should have been called");
        machine.LastException.ShouldBeOfType<InvalidOperationException>();
        machine.LastException!.Message.ShouldBe("Test exception from action");
        machine.LastDirective.ShouldBe(ExceptionDirective.Continue);
        machine.CurrentState.ShouldBe(TestState.Working, "Should transition to target state with Continue directive");
    }
}

/// <summary>
/// Test machine with Propagate directive
/// </summary>
[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class FluentPropagateMachine
{
    private static void Configure() => FSM
        .OnException<TestState>(nameof(PropagateHandler))
        .State(TestState.Idle)
            .On<TestTrigger>(TestTrigger.Start)
                .Action(nameof(ThrowingAction))
                .GoTo(TestState.Working);
    
    private async ValueTask ThrowingAction()
    {
        await Task.Yield();
        throw new InvalidOperationException("Test propagate");
    }
    
    private ExceptionDirective PropagateHandler(ExceptionContext<TestState, TestTrigger> context)
    {
        return ExceptionDirective.Propagate;
    }
}

public class FluentPropagateTests
{
    [Fact]
    public async Task FluentApi_OnException_PropagatesException()
    {
        // Arrange
        var machine = new FluentPropagateMachine(TestState.Idle);
        await machine.StartAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await machine.FireAsync(TestTrigger.Start));

        exception.Message.ShouldBe("Test propagate");
        machine.CurrentState.ShouldBe(TestState.Idle, "Should remain in original state on exception");
    }
}