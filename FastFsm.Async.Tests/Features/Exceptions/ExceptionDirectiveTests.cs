using Abstractions.Attributes;
using Shouldly;
using StateMachine.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Async.Tests.Features.Exceptions;

#region Test Enums and Custom Exception

public enum ExceptionTestStates
{
    Idle,
    Running,
    Failed
}

public enum ExceptionTestTriggers
{
    Start,
    Stop,
    Fail
}

public class TransientDeviceException : Exception
{
    public TransientDeviceException() : base("Transient device error") { }
    public TransientDeviceException(string message) : base(message) { }
}

#endregion

#region Test 1: OnEntry Continue swallows exception

[StateMachine(typeof(ExceptionTestStates), typeof(ExceptionTestTriggers))]
[OnException(nameof(HandleException))]
public partial class OnEntryContinueMachine
{
    public bool OnEntryExecuted { get; private set; }
    public bool ActionExecuted { get; private set; }
    
    [State(ExceptionTestStates.Running, OnEntry = nameof(OnEnterRunning))]
    [Transition(ExceptionTestStates.Idle, ExceptionTestTriggers.Start, ExceptionTestStates.Running, 
        Action = nameof(DoStart))]
    private void Configure() { }
    
    private async Task OnEnterRunning()
    {
        OnEntryExecuted = true;
        await Task.Yield();
        throw new TransientDeviceException();
    }
    
    private async Task DoStart()
    {
        ActionExecuted = true;
        await Task.Yield();
    }
    
    private ExceptionDirective HandleException(ExceptionContext<ExceptionTestStates, ExceptionTestTriggers> ctx)
    {
        return ctx.Exception switch
        {
            TransientDeviceException => ExceptionDirective.Continue,
            _ => ExceptionDirective.Propagate
        };
    }
}

#endregion

#region Test 2: Action Propagate propagates exception

[StateMachine(typeof(ExceptionTestStates), typeof(ExceptionTestTriggers))]
[OnException(nameof(HandleException))]
public partial class ActionPropagateMachine
{
    public bool ActionStarted { get; private set; }
    
    [Transition(ExceptionTestStates.Idle, ExceptionTestTriggers.Start, ExceptionTestStates.Running, 
        Action = nameof(DoStart))]
    private void Configure() { }
    
    private async Task DoStart()
    {
        ActionStarted = true;
        await Task.Yield();
        throw new InvalidOperationException("Action failed");
    }
    
    private ExceptionDirective HandleException(ExceptionContext<ExceptionTestStates, ExceptionTestTriggers> ctx)
    {
        return ExceptionDirective.Propagate;
    }
}

#endregion

#region Test 3: Guard exception - directive ignored

[StateMachine(typeof(ExceptionTestStates), typeof(ExceptionTestTriggers))]
[OnException(nameof(HandleException))]
public partial class GuardExceptionMachine
{
    public bool GuardCalled { get; private set; }
    public bool HandlerCalled { get; private set; }
    
    [Transition(ExceptionTestStates.Idle, ExceptionTestTriggers.Start, ExceptionTestStates.Running, 
        Guard = nameof(CanStart))]
    private void Configure() { }
    
    private async ValueTask<bool> CanStart()
    {
        GuardCalled = true;
        await Task.Yield();
        throw new Exception("Guard failed");
    }
    
    private ExceptionDirective HandleException(ExceptionContext<ExceptionTestStates, ExceptionTestTriggers> ctx)
    {
        HandlerCalled = true;
        return ExceptionDirective.Continue; // Should be ignored for guards
    }
}

#endregion

#region Test 4: OperationCanceledException always propagates

[StateMachine(typeof(ExceptionTestStates), typeof(ExceptionTestTriggers))]
[OnException(nameof(HandleException))]
public partial class CancellationPropagationMachine
{
    public bool OnEntryStarted { get; private set; }
    
    [State(ExceptionTestStates.Running, OnEntry = nameof(OnEnterRunning))]
    [Transition(ExceptionTestStates.Idle, ExceptionTestTriggers.Start, ExceptionTestStates.Running)]
    private void Configure() { }
    
    private async Task OnEnterRunning(CancellationToken cancellationToken)
    {
        OnEntryStarted = true;
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw new OperationCanceledException();
    }
    
    private ValueTask<ExceptionDirective> HandleException(
        ExceptionContext<ExceptionTestStates, ExceptionTestTriggers> ctx,
        CancellationToken cancellationToken)
    {
        // This should never be called for OperationCanceledException
        return new ValueTask<ExceptionDirective>(ExceptionDirective.Continue);
    }
}

#endregion

#region Test 5: Async handler with CancellationToken

[StateMachine(typeof(ExceptionTestStates), typeof(ExceptionTestTriggers))]
[OnException(nameof(HandleExceptionAsync))]
public partial class AsyncHandlerMachine
{
    public bool HandlerExecuted { get; private set; }
    
    [Transition(ExceptionTestStates.Idle, ExceptionTestTriggers.Start, ExceptionTestStates.Running, 
        Action = nameof(DoStart))]
    private void Configure() { }
    
    private async Task DoStart(CancellationToken cancellationToken)
    {
        await Task.Yield();
        throw new TransientDeviceException();
    }
    
    private async ValueTask<ExceptionDirective> HandleExceptionAsync(
        ExceptionContext<ExceptionTestStates, ExceptionTestTriggers> ctx,
        CancellationToken cancellationToken)
    {
        HandlerExecuted = true;
        await Task.Delay(10, cancellationToken);
        return ctx.Exception is TransientDeviceException 
            ? ExceptionDirective.Continue 
            : ExceptionDirective.Propagate;
    }
}

#endregion

public class ExceptionDirectiveTests
{
    [Fact]
    public async Task OnEntry_Continue_Should_Swallow_Exception()
    {
        // Arrange
        var machine = new OnEntryContinueMachine(ExceptionTestStates.Idle);
        await machine.StartAsync();
        
        // Act
        await machine.FireAsync(ExceptionTestTriggers.Start);
        
        // Assert
        machine.CurrentState.ShouldBe(ExceptionTestStates.Running);
        machine.OnEntryExecuted.ShouldBeTrue();
        machine.ActionExecuted.ShouldBeTrue(); // Action should run after OnEntry exception is swallowed
    }
    
    [Fact]
    public async Task Action_Propagate_Should_Propagate_Exception()
    {
        // Arrange
        var machine = new ActionPropagateMachine(ExceptionTestStates.Idle);
        await machine.StartAsync();
        
        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await machine.FireAsync(ExceptionTestTriggers.Start));
        
        ex.Message.ShouldBe("Action failed");
        machine.CurrentState.ShouldBe(ExceptionTestStates.Running); // State already changed
        machine.ActionStarted.ShouldBeTrue();
    }
    
    [Fact]
    public async Task Guard_Exception_Directive_Should_Be_Ignored()
    {
        // Arrange
        var machine = new GuardExceptionMachine(ExceptionTestStates.Idle);
        await machine.StartAsync();
        
        // Act
        var result = await machine.TryFireAsync(ExceptionTestTriggers.Start);
        
        // Assert
        result.ShouldBeFalse(); // Guard exception is treated as false
        machine.CurrentState.ShouldBe(ExceptionTestStates.Idle); // No state change
        machine.GuardCalled.ShouldBeTrue();
        // Note: handler may or may not be called for guards - implementation specific
    }
    
    [Fact]
    public async Task OperationCanceledException_Should_Always_Propagate()
    {
        // Arrange
        var machine = new CancellationPropagationMachine(ExceptionTestStates.Idle);
        await machine.StartAsync();
        using var cts = new CancellationTokenSource();
        
        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await machine.FireAsync(ExceptionTestTriggers.Start, cts.Token));
        
        machine.CurrentState.ShouldBe(ExceptionTestStates.Running); // State already changed
        machine.OnEntryStarted.ShouldBeTrue();
    }
    
    [Fact]
    public async Task Async_Handler_With_CancellationToken_Should_Work()
    {
        // Arrange
        var machine = new AsyncHandlerMachine(ExceptionTestStates.Idle);
        await machine.StartAsync();
        using var cts = new CancellationTokenSource();
        
        // Act
        await machine.FireAsync(ExceptionTestTriggers.Start, cts.Token);
        
        // Assert
        machine.CurrentState.ShouldBe(ExceptionTestStates.Running);
        machine.HandlerExecuted.ShouldBeTrue();
    }
    
    [Fact]
    public async Task Exception_Context_Should_Have_Correct_Values()
    {
        // Arrange
        ExceptionContext<ExceptionTestStates, ExceptionTestTriggers>? capturedContext = null;
        var machine = new ExceptionContextCaptureMachine(
            ExceptionTestStates.Idle,
            ctx => { capturedContext = ctx; return ExceptionDirective.Continue; });
        await machine.StartAsync();
        
        // Act
        await machine.FireAsync(ExceptionTestTriggers.Start);
        
        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.Value.From.ShouldBe(ExceptionTestStates.Idle);
        capturedContext.Value.To.ShouldBe(ExceptionTestStates.Running);
        capturedContext.Value.Trigger.ShouldBe(ExceptionTestTriggers.Start);
        capturedContext.Value.Stage.ShouldBe(TransitionStage.Action);
        capturedContext.Value.StateAlreadyChanged.ShouldBeTrue();
        capturedContext.Value.Exception.ShouldBeOfType<TransientDeviceException>();
    }
}

#region Helper machine for context capture

[StateMachine(typeof(ExceptionTestStates), typeof(ExceptionTestTriggers))]
[OnException(nameof(HandleException))]
public partial class ExceptionContextCaptureMachine
{
    private readonly Func<ExceptionContext<ExceptionTestStates, ExceptionTestTriggers>, ExceptionDirective> _handler;
    
    public ExceptionContextCaptureMachine(
        ExceptionTestStates initialState, 
        Func<ExceptionContext<ExceptionTestStates, ExceptionTestTriggers>, ExceptionDirective> handler) 
        : this(initialState)
    {
        _handler = handler;
    }
    
    [Transition(ExceptionTestStates.Idle, ExceptionTestTriggers.Start, ExceptionTestStates.Running, 
        Action = nameof(DoStart))]
    private void Configure() { }
    
    private async Task DoStart()
    {
        await Task.Yield();
        throw new TransientDeviceException();
    }
    
    private ExceptionDirective HandleException(ExceptionContext<ExceptionTestStates, ExceptionTestTriggers> ctx)
    {
        return _handler(ctx);
    }
}

#endregion
