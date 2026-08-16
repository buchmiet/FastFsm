using Abstractions.Attributes;
using FastFsm.Tests.Features.Hsm.CompileTime;

namespace FastFsm.Tests.Machines.Legacy;

[StateMachine(typeof(NH_State), typeof(NH_Trigger), EnableHierarchy = true)]
public partial class HierarchyFlagNoHierarchyMachine
{
    // Flat states (no Parent/IsInitial/History)
    [State(NH_State.S1)] private void S_S1() { }
    [State(NH_State.S2)] private void S_S2() { }

    [Transition(NH_State.S1, NH_Trigger.Next, NH_State.S2)]
    private void T_Flat() { }
}