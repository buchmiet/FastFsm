using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class PriorityTransitionMachine
{
    // Multiple transitions with different priorities from same state
    [Transition(HsmState.Priority_Low, HsmTrigger.Execute, HsmState.Priority_Medium,
        Priority = 0,
        Action = (LowPriorityAction))]
    [Transition(HsmState.Priority_Low, HsmTrigger.Execute, HsmState.Priority_High,
        Priority = 1000,
        Guard = (IsHighPriorityNeeded),
        Action = (HighPriorityAction))]
    private void ConfigurePriorityTransitions() { }

    // Different priority on different states
    [Transition(HsmState.Priority_Medium, HsmTrigger.Execute, HsmState.Priority_High,
        Priority = 500,
        Action = (MediumPriorityAction))]
    [Transition(HsmState.Priority_High, HsmTrigger.Reset, HsmState.Priority_Low,
        Priority = 100)]
    private void ConfigureOtherPriorities() { }

    // Action and guard methods
    private void HighPriorityAction() { }
    private void MediumPriorityAction() { }
    private void LowPriorityAction() { }
    private bool IsHighPriorityNeeded() => true;
}
