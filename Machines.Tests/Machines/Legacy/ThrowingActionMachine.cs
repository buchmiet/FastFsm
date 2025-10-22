namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(ThrowingActionMachine_TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class ThrowingActionMachine
{
    [Transition(ThrowingActionMachine_TestState.A, TestTrigger.Go, ThrowingActionMachine_TestState.B, Action = (ThrowingAction))]
    private void Configure() { }

    public void ThrowingAction() => throw new InvalidOperationException("boom");
}