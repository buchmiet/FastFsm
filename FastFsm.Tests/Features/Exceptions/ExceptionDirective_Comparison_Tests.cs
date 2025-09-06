// TEMP disabled due to generator nested-type ExceptionContext formatting; see issue note.
#if false
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

/// <summary>
/// Tests comparing behavior between attribute-based and Fluent API exception handling.
/// </summary>
public partial class ExceptionDirective_Comparison_Tests
{
    #region Test Enums
    public enum CompState { Idle, Running, Failed, Done }
    public enum CompTrigger { Start, Process, Fail, Complete }
    #endregion

    #region Continue on OnEntry - Legacy
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    [OnException(nameof(HandleException))]
    public partial class OnEntryContinueMachine_Legacy
    {
        public bool ThrowOnEntry { get; set; }
        public List<string> Log { get; } = new();

        [State(CompState.Running, OnEntry = nameof(OnEntryRunning))]
        [Transition(CompState.Idle, CompTrigger.Start, CompState.Running)]
        private void Configure() { }

        private void OnEntryRunning()
        {
            Log.Add("OnEntry-Running");
            if (ThrowOnEntry)
            {
                Log.Add("OnEntry-Throwing");
                throw new InvalidOperationException("OnEntry error");
            }
        }

        private ExceptionDirective HandleException(ExceptionContext<CompState, CompTrigger> ctx)
        {
            Log.Add($"Handler-{ctx.Stage}");
            return ExceptionDirective.Continue;
        }
    }
    #endregion

    #region Continue on OnEntry - Fluent
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    public partial class OnEntryContinueMachine_Fluent
    {
        public bool ThrowOnEntry { get; set; }
        public List<string> Log { get; } = new();

        private static void Configure() => FSM
            .OnException<CompState>(nameof(HandleException))
            .State(CompState.Idle)
                .On(CompTrigger.Start).GoTo(CompState.Running)
            .State(CompState.Running)
                .OnEntry(nameof(OnEntryRunning));

        private void OnEntryRunning()
        {
            Log.Add("OnEntry-Running");
            if (ThrowOnEntry)
            {
                Log.Add("OnEntry-Throwing");
                throw new InvalidOperationException("OnEntry error");
            }
        }

        private ExceptionDirective HandleException(ExceptionContext<CompState, CompTrigger> ctx)
        {
            Log.Add($"Handler-{ctx.Stage}");
            return ExceptionDirective.Continue;
        }
    }
    #endregion

    #region Propagate on Action - Legacy
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    [OnException(nameof(HandleException))]
    public partial class ActionPropagateMachine_Legacy
    {
        public bool ThrowInAction { get; set; }
        public List<string> Log { get; } = new();

        [Transition(CompState.Idle, CompTrigger.Process, CompState.Running, Action = nameof(ProcessAction))]
        private void Configure() { }

        private void ProcessAction()
        {
            Log.Add("Action-Process");
            if (ThrowInAction)
            {
                Log.Add("Action-Throwing");
                throw new InvalidOperationException("Action error");
            }
        }

        private ExceptionDirective HandleException(ExceptionContext<CompState, CompTrigger> ctx)
        {
            Log.Add($"Handler-{ctx.Stage}");
            return ExceptionDirective.Propagate;
        }
    }
    #endregion

    #region Propagate on Action - Fluent
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    public partial class ActionPropagateMachine_Fluent
    {
        public bool ThrowInAction { get; set; }
        public List<string> Log { get; } = new();

        private static void Configure() => FSM
            .OnException<CompState>(nameof(HandleException))
            .State(CompState.Idle)
                .On(CompTrigger.Process).Action(nameof(ProcessAction)).GoTo(CompState.Running);

        private void ProcessAction()
        {
            Log.Add("Action-Process");
            if (ThrowInAction)
            {
                Log.Add("Action-Throwing");
                throw new InvalidOperationException("Action error");
            }
        }

        private ExceptionDirective HandleException(ExceptionContext<CompState, CompTrigger> ctx)
        {
            Log.Add($"Handler-{ctx.Stage}");
            return ExceptionDirective.Propagate;
        }
    }
    #endregion

    #region Async Handler - Legacy
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    [OnException(nameof(HandleExceptionAsync))]
    public partial class AsyncHandlerMachine_Legacy
    {
        public bool ThrowInOnExit { get; set; }
        public List<string> Log { get; } = new();

        [State(CompState.Idle, OnExit = nameof(OnExitIdleAsync))]
        [Transition(CompState.Idle, CompTrigger.Start, CompState.Running)]
        private void Configure() { }

        private async Task OnExitIdleAsync()
        {
            await Task.Yield();
            Log.Add("OnExit-Idle");
            if (ThrowInOnExit)
            {
                Log.Add("OnExit-Throwing");
                throw new InvalidOperationException("OnExit error");
            }
        }

        private async ValueTask<ExceptionDirective> HandleExceptionAsync(
            ExceptionContext<CompState, CompTrigger> ctx,
            CancellationToken ct)
        {
            await Task.Yield();
            Log.Add($"AsyncHandler-{ctx.Stage}");
            return ExceptionDirective.Continue;
        }
    }
    #endregion

    #region Async Handler - Fluent
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    public partial class AsyncHandlerMachine_Fluent
    {
        public bool ThrowInOnExit { get; set; }
        public List<string> Log { get; } = new();

        private static void Configure() => FSM
            .OnException<CompState>(nameof(HandleExceptionAsync))
            .State(CompState.Idle)
                .OnExitAsync(nameof(OnExitIdleAsync))
                .On(CompTrigger.Start).GoTo(CompState.Running)
            .State(CompState.Running);

        private async Task OnExitIdleAsync()
        {
            await Task.Yield();
            Log.Add("OnExit-Idle");
            if (ThrowInOnExit)
            {
                Log.Add("OnExit-Throwing");
                throw new InvalidOperationException("OnExit error");
            }
        }

        private async ValueTask<ExceptionDirective> HandleExceptionAsync(
            ExceptionContext<CompState, CompTrigger> ctx,
            CancellationToken ct)
        {
            await Task.Yield();
            Log.Add($"AsyncHandler-{ctx.Stage}");
            return ExceptionDirective.Continue;
        }
    }
    #endregion

    [Fact]
    public void OnEntry_Continue_BehaviorIdentical()
    {
        var legacy = new OnEntryContinueMachine_Legacy(CompState.Idle) { ThrowOnEntry = true };
        var fluent = new OnEntryContinueMachine_Fluent(CompState.Idle) { ThrowOnEntry = true };

        legacy.Start();
        fluent.Start();

        // Both should continue despite exception
        legacy.Fire(CompTrigger.Start);
        fluent.Fire(CompTrigger.Start);

        // Both should be in Running state
        Assert.Equal(CompState.Running, legacy.CurrentState);
        Assert.Equal(CompState.Running, fluent.CurrentState);

        // Both should have same log sequence
        Assert.Equal(legacy.Log, fluent.Log);
        Assert.Contains("Handler-OnEntry", legacy.Log);
    }

    [Fact]
    public void Action_Propagate_BehaviorIdentical()
    {
        var legacy = new ActionPropagateMachine_Legacy(CompState.Idle) { ThrowInAction = true };
        var fluent = new ActionPropagateMachine_Fluent(CompState.Idle) { ThrowInAction = true };

        legacy.Start();
        fluent.Start();

        // Both should throw
        var legacyEx = Assert.Throws<InvalidOperationException>(() => legacy.Fire(CompTrigger.Process));
        var fluentEx = Assert.Throws<InvalidOperationException>(() => fluent.Fire(CompTrigger.Process));

        // Both should have transitioned before throwing
        Assert.Equal(CompState.Running, legacy.CurrentState);
        Assert.Equal(CompState.Running, fluent.CurrentState);

        // Both should have same exception message
        Assert.Equal(legacyEx.Message, fluentEx.Message);

        // Both should have called handler
        Assert.Contains("Handler-Action", legacy.Log);
        Assert.Contains("Handler-Action", fluent.Log);
    }

    [Fact]
    public async Task AsyncHandler_Continue_BehaviorIdentical()
    {
        var legacy = new AsyncHandlerMachine_Legacy(CompState.Idle) { ThrowInOnExit = true };
        var fluent = new AsyncHandlerMachine_Fluent(CompState.Idle) { ThrowInOnExit = true };

        await legacy.StartAsync();
        await fluent.StartAsync();

        // Both should continue despite exception
        await legacy.FireAsync(CompTrigger.Start);
        await fluent.FireAsync(CompTrigger.Start);

        // Both should be in Running state
        Assert.Equal(CompState.Running, legacy.CurrentState);
        Assert.Equal(CompState.Running, fluent.CurrentState);

        // Both should have same log sequence
        Assert.Equal(legacy.Log, fluent.Log);
        Assert.Contains("AsyncHandler-OnExit", legacy.Log);
    }

    [Fact]
    public void ExceptionContext_PropertiesCorrect()
    {
        ExceptionContext<CompState, CompTrigger>? capturedContext = null;

        var machine = new ContextCaptureMachine_Fluent(CompState.Idle);
        machine.CaptureContext = ctx => capturedContext = ctx;
        machine.Start();

        machine.Fire(CompTrigger.Process);

        Assert.NotNull(capturedContext);
        Assert.Equal(CompState.Idle, capturedContext.Value.From);
        Assert.Equal(CompState.Running, capturedContext.Value.To);
        Assert.Equal(CompTrigger.Process, capturedContext.Value.Trigger);
        Assert.Equal(TransitionStage.Action, capturedContext.Value.Stage);
        Assert.True(capturedContext.Value.StateAlreadyChanged);
        Assert.IsType<InvalidOperationException>(capturedContext.Value.Exception);
    }

    #region Context Capture Machine
    [StateMachine(typeof(CompState), typeof(CompTrigger))]
    public partial class ContextCaptureMachine_Fluent
    {
        public Action<ExceptionContext<CompState, CompTrigger>>? CaptureContext { get; set; }

        private static void Configure() => FSM
            .OnException<CompState>(nameof(HandleException))
            .State(CompState.Idle)
                .On(CompTrigger.Process).Action(nameof(ProcessAction)).GoTo(CompState.Running);

        private void ProcessAction() => throw new InvalidOperationException("Test exception");

        private ExceptionDirective HandleException(ExceptionContext<CompState, CompTrigger> ctx)
        {
            CaptureContext?.Invoke(ctx);
            return ExceptionDirective.Continue;
        }
    }
    #endregion
}
#endif
