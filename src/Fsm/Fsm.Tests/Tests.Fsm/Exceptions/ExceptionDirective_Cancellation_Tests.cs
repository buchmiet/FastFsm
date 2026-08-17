using System;
using System.Threading.Tasks;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Exceptions;

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
