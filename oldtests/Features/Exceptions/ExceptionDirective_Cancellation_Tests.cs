using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using Xunit;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Cancellation_Tests
{
    [Fact]
    public async Task OnEntry_OCE_AlwaysPropagates_EvenIfHandlerReturnsContinue()
    {
        var m = new Machines.AsyncOceOnEntryMachine(CSState.A) { ThrowOceOnEntryB = true };
        await m.StartAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await m.FireAsync(CSTrigger.Go);
        });

        Assert.Equal(CSState.B, m.CurrentState);
    }
}
