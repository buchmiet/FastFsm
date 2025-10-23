using System;
using System.Threading.Tasks;
using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;

namespace FastFsm.Tests.Exceptions;

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
