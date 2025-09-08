using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(CaseSensitiveState), typeof(CaseSensitiveTrigger))]
    public partial class CaseSensitiveMachine
    {
        [Transition(CaseSensitiveState.state, CaseSensitiveTrigger.GO, CaseSensitiveState.STATE)]
        [Transition(CaseSensitiveState.STATE, CaseSensitiveTrigger.go, CaseSensitiveState.State)]
        [Transition(CaseSensitiveState.State, CaseSensitiveTrigger.Go, CaseSensitiveState.state)]
        private void Configure() { }
    }
}
