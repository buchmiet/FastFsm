using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(AafState), typeof(AafTrigger))]
public partial class AsyncActionFluentMachine
{
    public enum AafState { Disconnected, Connecting, Connected }
    public enum AafTrigger { Connect, ConnectedOk, Disconnect }

    private static void Configure() => FSM
        .State(AafState.Disconnected)
            .On(AafTrigger.Connect).GoTo(AafState.Connecting).Action(nameof(BeginConnectAsync))
        .State(AafState.Connecting)
            .On(AafTrigger.ConnectedOk).GoTo(AafState.Connected)
        .State(AafState.Connected)
            .On(AafTrigger.Disconnect).GoTo(AafState.Disconnected).Action(nameof(CloseAsync));

    private async ValueTask BeginConnectAsync()
    {
        await Task.Yield();
    }

    private async Task CloseAsync()
    {
        await Task.Yield();
    }
}

