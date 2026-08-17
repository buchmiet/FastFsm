namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(TestInitialState), typeof(TestInitialTrigger))]
public partial class InitialOnEntryStateMachineActions
{
    [State(TestInitialState.Ready, OnEntry = nameof(OnReadyEntry))]
    private void ConfigureReady() { }

    private void OnReadyEntry() { }
}