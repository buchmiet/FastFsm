using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

[StateMachine(typeof(AgfState), typeof(AgfTrigger))]
public partial class AsyncGuardFluentMachine
{
    public enum AgfState { Idle, Busy }
    public enum AgfTrigger { Start }

    private static void Configure() => FSM
        .State(AgfState.Idle)
            .On(AgfTrigger.Start).GoTo(AgfState.Busy).Guard(nameof(CanStartAsync));

    private async ValueTask<bool> CanStartAsync()
    {
        await Task.Yield();
        return true;
    }
}

