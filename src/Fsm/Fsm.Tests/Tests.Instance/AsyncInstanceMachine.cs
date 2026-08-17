using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using StateMachineAttribute = Abstractions.Attributes.StateMachineAttribute;

namespace Tests.Instance;

[StateMachine(typeof(State), typeof(Trigger))]
public partial class AsyncInstanceMachine
{
    public enum State { Idle, Busy }
    public enum Trigger { Activate, Deactivate }

    private readonly List<string> _events = new();

    public IReadOnlyList<string> Events => _events;

    private void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Activate)
                .ActionAsync(BeginAsync)
                .GoTo(State.Busy)
        .State(State.Busy)
            .On(Trigger.Deactivate)
                .Guard(AllowDeactivationAsync)
                .ActionAsync(CloseAsync)
                .GoTo(State.Idle);

    private ValueTask BeginAsync(CancellationToken ct)
    {
        _events.Add("begin");
        return ValueTask.CompletedTask;
    }

    private ValueTask<bool> AllowDeactivationAsync(CancellationToken ct)
        => ValueTask.FromResult(true);

    private ValueTask CloseAsync(CancellationToken ct)
    {
        _events.Add("close");
        return ValueTask.CompletedTask;
    }
}
