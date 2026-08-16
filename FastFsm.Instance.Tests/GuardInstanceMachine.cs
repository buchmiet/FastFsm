using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Instance.Tests;

[StateMachine(typeof(State), typeof(Trigger))]
public partial class GuardInstanceMachine
{
    public enum State { Idle, Active }
    public enum Trigger { Start, Stop }

    private bool _allowStart = true;

    private void Configure() => FSM
        .State(State.Idle)
            .On(Trigger.Start)
                .Guard(CanStart)
                .GoTo(State.Active)
        .State(State.Active)
            .On(Trigger.Stop)
                .Guard(CanStop)
                .GoTo(State.Idle);

    private bool CanStart() => _allowStart;
    private bool CanStop() => true;

    public void SetAllowStart(bool value) => _allowStart = value;
}
