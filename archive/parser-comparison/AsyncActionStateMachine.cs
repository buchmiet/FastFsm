using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(AaState), typeof(AaTrigger))]
public partial class AsyncActionStateMachine
{
    public enum AaState { Disconnected, Connecting, Connected }
    public enum AaTrigger { Connect, ConnectedOk, Disconnect }

    [Transition(AaState.Disconnected, AaTrigger.Connect, AaState.Connecting, Action = nameof(BeginConnectAsync))]
    [Transition(AaState.Connecting, AaTrigger.ConnectedOk, AaState.Connected)]
    [Transition(AaState.Connected, AaTrigger.Disconnect, AaState.Disconnected, Action = nameof(CloseAsync))]
    private void Configure() { }

    private async ValueTask BeginConnectAsync()
    {
        await Task.Yield();
    }

    private async Task CloseAsync()
    {
        await Task.Yield();
    }
}

