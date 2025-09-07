using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Cancellation_Tests
{
    [Fact]
    public async Task OnEntry_OCE_AlwaysPropagates_EvenIfHandlerReturnsContinue()
    {
        var m = new AsyncOceOnEntryMachineFluent(CSState.A) { ThrowOceOnEntryB = true };
        await m.StartAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await m.FireAsync(CSTrigger.Go);
        });

        Assert.Equal(CSState.B, m.CurrentState);
    }
}

[StateMachine(typeof(CSState), typeof(CSTrigger), ContinueOnCapturedContext = false)]
public partial class AsyncOceOnEntryMachineFluent
{
    public bool ThrowOceOnEntryB { get; set; }

    private static void Configure() => FSM
        .OnException<CSState>(nameof(HandleAsync))
        .State(CSState.A)
            .On(CSTrigger.Go).GoTo(CSState.B)
        .State(CSState.B)
            .OnEntryAsync(nameof(OnEntryBAsync));

    private async ValueTask OnEntryBAsync(CancellationToken ct)
    {
        await Task.Yield();
        if (ThrowOceOnEntryB)
            throw new OperationCanceledException();
    }

    private ValueTask<ExceptionDirective> HandleAsync(ExceptionContext<CSState, CSTrigger> ctx, CancellationToken ct)
        => ValueTask.FromResult(ExceptionDirective.Continue);
}
