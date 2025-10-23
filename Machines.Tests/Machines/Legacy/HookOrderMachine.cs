using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(HookOrderState), typeof(HookOrderTrigger), GenerateExtensibleVersion = true)]
public partial class HookOrderMachine
{
    private bool Guard() => true;

    [Transition(HookOrderState.A, HookOrderTrigger.Next, HookOrderState.B,
        Guard = (Guard))]
    private void Configure() { }
}
