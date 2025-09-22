using Abstractions.Fluent;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(CaseSensitiveState), typeof(CaseSensitiveTrigger))]
public partial class CaseSensitiveMachineFluent
{
    private void Configure() => FSM
        .State(CaseSensitiveState.state)
        .On(CaseSensitiveTrigger.GO).GoTo(CaseSensitiveState.STATE)
        .State(CaseSensitiveState.STATE)
        .On(CaseSensitiveTrigger.go).GoTo(CaseSensitiveState.State)
        .State(CaseSensitiveState.State)
        .On(CaseSensitiveTrigger.Go).GoTo(CaseSensitiveState.state);
}