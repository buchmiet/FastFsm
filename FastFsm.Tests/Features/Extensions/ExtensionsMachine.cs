using Abstractions.Attributes;
using FastFsm.Tests.Features.Extensions;

namespace FastFsm.Tests.Features.Extensions
{
    [StateMachine(typeof(ExtState), typeof(ExtTrigger), GenerateExtensibleVersion = true)]
    public partial class ExtensionsMachine
    {
        [State(ExtState.Idle, OnEntry = nameof(OnEnterIdle))]
        [State(ExtState.Working, OnExit = nameof(OnExitWorking))]
        private void ConfigureStates() { }

        [Transition(ExtState.Idle, ExtTrigger.Start, ExtState.Working,
            Guard = nameof(CanStart), Action = nameof(StartWork))]
        [Transition(ExtState.Working, ExtTrigger.Finish, ExtState.Complete)]
        [Transition(ExtState.Complete, ExtTrigger.Cancel, ExtState.Idle)]
        private void Configure() { }

        private bool CanStart() => true;
        private void StartWork() { }
        private void OnEnterIdle() { }
        private void OnExitWorking() { }
    }
}
