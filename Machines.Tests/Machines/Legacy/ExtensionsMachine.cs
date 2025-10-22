namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(ExtState), typeof(ExtTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensionsMachine
{
    [State(ExtState.Idle, OnEntry = (OnEnterIdle))]
    [State(ExtState.Working, OnExit = (OnExitWorking))]
    private void ConfigureStates() { }

    [Transition(ExtState.Idle, ExtTrigger.Start, ExtState.Working,
        Guard = (CanStart), Action = (StartWork))]
    [Transition(ExtState.Working, ExtTrigger.Finish, ExtState.Complete)]
    [Transition(ExtState.Complete, ExtTrigger.Cancel, ExtState.Idle)]
    private void Configure() { }

    private bool CanStart() => true;
    private void StartWork() { }
    private void OnEnterIdle() { }
    private void OnExitWorking() { }
}