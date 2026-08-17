using Abstractions.Attributes;

namespace Tests.Logging;

[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensionsStateMachine
{
    public bool GuardResult { get; set; } = true;

    [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing,
        Guard = nameof(CanStart), Action = nameof(StartAction))]
    [State(TestState.Processing, OnEntry = nameof(OnProcessingEntry))]
    private void ConfigureStart() { }

    [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
    [Transition(TestState.Processing, TestTrigger.Fail, TestState.Failed)]
    private void ConfigureOthers() { }

    private bool CanStart() => GuardResult;
    private void StartAction() { }
    private void OnProcessingEntry() { }
}