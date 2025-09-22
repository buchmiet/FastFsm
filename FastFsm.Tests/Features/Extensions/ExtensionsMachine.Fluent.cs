using Abstractions.Fluent;

namespace FastFsm.Tests.Features.Extensions;

[StateMachine(typeof(ExtState), typeof(ExtTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensionsMachineFluent
{
    private void Configure() => FSM
        .State<ExtState>(ExtState.Idle)
        .OnEntry(nameof(OnEnterIdle))
        .On(ExtTrigger.Start).Guard(nameof(CanStart)).Action(nameof(StartWork)).GoTo(ExtState.Working)
        .State(ExtState.Working)
        .OnExit(nameof(OnExitWorking))
        .On(ExtTrigger.Finish).GoTo(ExtState.Complete)
        .State(ExtState.Complete)
        .On(ExtTrigger.Cancel).GoTo(ExtState.Idle);

    private bool CanStart() => true;
    private void StartWork() { }
    private void OnEnterIdle() { }
    private void OnExitWorking() { }
}