using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Cancellation_Tests
{
    [Fact]
    public async Task OnEntry_OCE_AlwaysPropagates_EvenIfHandlerReturnsContinue()
    {
        var m = new AsyncOceOnEntryMachine(CSState.A) { ThrowOceOnEntryB = true };
        await m.StartAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await m.FireAsync(CSTrigger.Go);
        });

        Assert.Equal(CSState.B, m.CurrentState);
    }
}

public enum CSState { A, B }
public enum CSTrigger { Go }

[StateMachine(typeof(CSState), typeof(CSTrigger), ContinueOnCapturedContext = false)]
[OnException(nameof(HandleAsync))]
public partial class AsyncOceOnEntryMachine
{
    public bool ThrowOceOnEntryB { get; set; }

    [State(CSState.B, OnEntry = nameof(OnEntryBAsync))]
    [Transition(CSState.A, CSTrigger.Go, CSState.B)]
    private void Configure() { }

    private async ValueTask OnEntryBAsync(CancellationToken ct)
    {
        await Task.Yield();
        if (ThrowOceOnEntryB)
            throw new OperationCanceledException();
    }

    private ValueTask<ExceptionDirective> HandleAsync(ExceptionContext<CSState, CSTrigger> ctx, CancellationToken ct)
        => ValueTask.FromResult(ExceptionDirective.Continue);
}
