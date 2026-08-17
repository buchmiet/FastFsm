using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(CaseSensitiveState), typeof(CaseSensitiveTrigger))]
public partial class CaseSensitiveMachine
{
    private void Configure() => FSM
        .State(CaseSensitiveState.state)
        .On(CaseSensitiveTrigger.GO).GoTo(CaseSensitiveState.STATE)
        .State(CaseSensitiveState.STATE)
        .On(CaseSensitiveTrigger.go).GoTo(CaseSensitiveState.State)
        .State(CaseSensitiveState.State)
        .On(CaseSensitiveTrigger.Go).GoTo(CaseSensitiveState.state);
}