using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Cancellation_Tests_Fluent
{
    [Fact]
    public async Task OnEntry_OCE_AlwaysPropagates_EvenIfHandlerReturnsContinue_Fluent()
    {
        var m = new AsyncOceOnEntryMachine_Fluent(CSState.A) { ThrowOceOnEntryB = true };
        await m.StartAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await m.FireAsync(CSTrigger.Go);
        });

        Assert.Equal(CSState.B, m.CurrentState);
    }
}

[StateMachine(typeof(CSState), typeof(CSTrigger), ContinueOnCapturedContext = false)]
public partial class AsyncOceOnEntryMachine_Fluent
{
    public bool ThrowOceOnEntryB { get; set; }

    private static void Configure() => FSM
        .State<CSState>(CSState.A)
            .OnException(nameof(HandleAsync))
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