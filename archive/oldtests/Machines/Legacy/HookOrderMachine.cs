using Abstractions.Attributes;
using FastFsm.Tests.Features.Extensions;

namespace FastFsm.Tests.Machines.Legacy;

[StateMachine(typeof(HookOrderState), typeof(HookOrderTrigger), GenerateExtensibleVersion = true)]
public partial class HookOrderMachine
{
    private bool Guard() => true;

    [Transition(HookOrderState.A, HookOrderTrigger.Next, HookOrderState.B,
        Guard = nameof(Guard))]
    private void Configure() { }
}