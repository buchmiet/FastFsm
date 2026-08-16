using Abstractions.Attributes;
using Dsl;
using System.Threading;
using System.Threading.Tasks;

namespace ParserComparison.Tests;

[StateMachine(typeof(TestFluentAsyncMachine.AsyncState), typeof(TestFluentAsyncMachine.AsyncTrigger))]
public partial class TestFluentAsyncMachine
{
    public enum AsyncState { Disconnected, Connecting, Connected }
    public enum AsyncTrigger { Connect, Connected, Disconnect }

    public bool IsConnecting { get; private set; }
    public bool IsConnected { get; private set; }
    public int ConnectionAttempts { get; private set; }

    private static void Configure() => FSM
        .State(AsyncState.Disconnected)
            .OnEntryAsync(nameof(OnDisconnectedEntryAsync))
            .On(AsyncTrigger.Connect)
                .GuardAsync(nameof(CanConnectAsync))
                .ActionAsync(nameof(StartConnectionAsync))
                .GoTo(AsyncState.Connecting)
        .State(AsyncState.Connecting)
            .On(AsyncTrigger.Connected)
                .ActionAsync(nameof(OnConnectedAsync))
                .GoTo(AsyncState.Connected)
        .State(AsyncState.Connected)
            .OnExitAsync(nameof(OnConnectedExitAsync))
            .On(AsyncTrigger.Disconnect)
                .ActionAsync(nameof(DisconnectAsync))
                .GoTo(AsyncState.Disconnected);

    public async Task OnDisconnectedEntryAsync(CancellationToken ct)
    {
        IsConnected = false;
        await Task.Delay(10, ct);
    }

    public async ValueTask<bool> CanConnectAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
        return ConnectionAttempts < 3;
    }

    public async Task StartConnectionAsync(CancellationToken ct)
    {
        IsConnecting = true;
        ConnectionAttempts++;
        await Task.Delay(50, ct);
    }

    public async Task OnConnectedAsync(CancellationToken ct)
    {
        IsConnecting = false;
        IsConnected = true;
        await Task.Delay(10, ct);
    }

    public async ValueTask OnConnectedExitAsync()
    {
        await Task.Delay(10);
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        IsConnected = false;
        await Task.Delay(10, ct);
    }
}